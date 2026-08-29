import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * One box per blank.
 *
 * This did not exist, and a fill-in-the-blank question fell through to the plain
 * text box — which emits one string. The grader reads a value per blank, could
 * not parse a bare string, and returned *wrong*. So a candidate who typed the
 * right answer scored zero, and because it never asked for a person nobody was
 * ever going to notice.
 *
 * The blank ids come from the server, which sends which blanks exist and never
 * what goes in them.
 */
@Component({
  selector: 'astro-blanks-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <ol class="blanks">
      @for (id of blankIds(); track id; let i = $index) {
        <li class="blank">
          <label class="blank__label" [attr.for]="'blank-' + id">
            {{ t('::Take:Blank', (i + 1).toString()) }}
          </label>
          <input
            class="form-control blank__input"
            [id]="'blank-' + id"
            [ngModel]="value()[id] ?? ''"
            [name]="'blank-' + id"
            (ngModelChange)="set(id, $event)" />
        </li>
      }
    </ol>

    @if (blankIds().length === 0) {
      <!-- A question whose payload names no blanks. Rendering nothing would
           strand somebody on a question they can read and cannot answer, with a
           clock running. -->
      <textarea
        class="form-control fallback"
        rows="4"
        [ngModel]="fallback()"
        (ngModelChange)="setFallback($event)"
        [attr.aria-label]="t('::Take:YourAnswer')"></textarea>
    }
  `,
  styles: `
    :host { display: block; }

    .blanks {
      list-style: none;
      margin: 0;
      padding: 0;
      display: grid;
      gap: var(--astro-space-3);
    }

    .blank {
      display: grid;
      gap: 0.25rem;
    }

    .blank__label {
      font-size: var(--astro-text-sm);
      color: var(--text-secondary);
    }

    .blank__input {
      font-size: 1rem;
      min-block-size: var(--astro-touch-min);
    }

    .fallback { font-size: 1rem; }
  `,
})
export class BlanksAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly value = signal<Record<string, string>>({});
  readonly fallback = signal('');

  /** Which blanks this question has, in the order the author wrote them. */
  readonly blankIds = computed<string[]>(() => {
    const ids = this.question().display?.['blankIds'];

    return Array.isArray(ids) ? (ids as string[]) : [];
  });

  private seeded = false;

  constructor() {
    effect(() => {
      const saved = this.response();

      // Seeded once. Re-reading on every emission would overwrite what somebody
      // is typing with what the server last heard.
      if (this.seeded || saved === undefined) {
        return;
      }

      this.seeded = true;

      try {
        const parsed = JSON.parse(saved) as unknown;

        if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
          this.value.set(parsed as Record<string, string>);
          return;
        }
      } catch {
        // An older answer saved as a bare string, from before this component
        // existed. Kept rather than discarded: it is what the candidate wrote.
      }

      this.fallback.set(saved);
    });
  }

  set(id: string, typed: string): void {
    const next = { ...this.value(), [id]: typed };

    this.value.set(next);
    this.responseChange.emit(JSON.stringify(next));
  }

  setFallback(typed: string): void {
    this.fallback.set(typed);
    this.responseChange.emit(typed);
  }
}
