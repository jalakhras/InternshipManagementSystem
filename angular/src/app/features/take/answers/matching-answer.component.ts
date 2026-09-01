import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * Matching each term with its pair.
 *
 * A dropdown per row, not lines drawn between two columns. Drawing connections
 * is a mouse-only interaction that cannot be done with a keyboard at all, and
 * on a phone it is impossible — while a select is native, reachable, and
 * announced properly without any work.
 *
 * The right-hand column arrives shuffled independently of the left, so the rows
 * lining up says nothing about which pairs with which.
 */
@Component({
  selector: 'astro-matching-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    @if (allOrNothing()) {
      <p class="scoring-rule">{{ t('::Take:Scoring:all') }}</p>
    }

    <div class="pairs">
      @for (item of left(); track item.id) {
        <div class="pair">
          <span class="pair__term">{{ item.text }}</span>

          <select
            class="form-select pair__choice"
            [ngModel]="chosen()[item.id] ?? ''"
            (ngModelChange)="choose(item.id, $event)"
            [attr.aria-label]="t('::Take:MatchFor') + ': ' + item.text">
            <option value="">{{ t('::Take:ChooseMatch') }}</option>
            @for (option of right(); track option.id) {
              <option [value]="option.id">{{ option.text }}</option>
            }
          </select>
        </div>
      }
    </div>
  `,
  styles: `
    .scoring-rule {
      margin: 0 0 var(--space-3);
      padding-inline-start: var(--space-3);
      border-inline-start: 3px solid var(--astro-border);
      color: var(--astro-fg-muted);
      font-size: 0.875rem;
      line-height: 1.6;
    }
    :host { display: block; }

    .pairs { display: grid; gap: var(--astro-space-3); }

    .pair {
      display: grid;
      grid-template-columns: 1fr 1fr;
      align-items: center;
      gap: var(--astro-space-3);
      padding: var(--astro-space-2) var(--astro-space-3);
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-md);
      background: var(--surface-raised);
    }

    @media (max-width: 40rem) {
      /* Stacked rather than squeezed: two columns at 412px leaves neither the
         term nor its options readable. */
      .pair { grid-template-columns: 1fr; }
    }

    .pair__term { line-height: var(--astro-leading-body); }
    .pair__choice { min-block-size: 2.75rem; }
  `,
})
export class MatchingAnswerComponent {
  readonly t = inject(TranslateService).t;

  /**
   * True when this question pays nothing for a partly right answer.
   *
   * Said only in that case. Being paid for the parts you placed correctly is
   * what a person assumes, so saying it adds nothing; being paid nothing for
   * four of five is not, and somebody who knew would have spent their last
   * minutes differently.
   */
  readonly allOrNothing = computed(() => this.question().display?.['scoring'] === 'all');

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly left = signal<{ id: string; text: string }[]>([]);
  readonly right = signal<{ id: string; text: string }[]>([]);
  readonly chosen = signal<Record<string, string>>({});

  private seeded = false;

  constructor() {
    effect(() => {
      const question = this.question();
      const saved = this.response();

      if (this.seeded) {
        return;
      }

      this.seeded = true;

      this.left.set((question.display['left'] as { id: string; text: string }[] | undefined) ?? []);
      this.right.set((question.display['right'] as { id: string; text: string }[] | undefined) ?? []);
      this.chosen.set(this.parse(saved));
    });
  }

  choose(leftId: string, rightId: string): void {
    this.chosen.update(current => {
      const next = { ...current };

      if (rightId) {
        next[leftId] = rightId;
      } else {
        delete next[leftId];
      }

      return next;
    });

    this.responseChange.emit(JSON.stringify(this.chosen()));
  }

  private parse(saved: string | undefined): Record<string, string> {
    if (!saved) {
      return {};
    }

    try {
      const parsed: unknown = JSON.parse(saved);

      return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
        ? (parsed as Record<string, string>)
        : {};
    } catch {
      return {};
    }
  }
}
