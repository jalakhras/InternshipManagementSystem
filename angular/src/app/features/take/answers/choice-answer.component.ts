import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { MediaService } from '../../../core/media.service';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * Choosing one option, or several.
 *
 * The options arrive already shuffled and stripped: the server sends an id, a
 * text and sometimes a picture, and nothing that says which is right. The order
 * they arrive in is this candidate's order, fixed for the attempt, so reloading
 * the page does not rearrange the answers under them mid-thought.
 *
 * Whole-row targets rather than the native control alone. A radio button is
 * sixteen pixels and this is sat on phones under time pressure, which is exactly
 * the situation where a near miss costs somebody a mark they had earned.
 */
@Component({
  selector: 'astro-choice-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <fieldset class="choices">
      <legend class="astro-visually-hidden">{{ t('::Take:ChooseAnswer') }}</legend>

      @for (option of question().options; track option.id) {
        <label class="choice" [class.choice--picked]="isPicked(option.id)">
          <input
            class="choice__control"
            [type]="isSingle() ? 'radio' : 'checkbox'"
            [name]="'q-' + question().id"
            [checked]="isPicked(option.id)"
            (change)="pick(option.id)" />

          <span class="choice__mark" aria-hidden="true"></span>

          <span class="choice__body">
            @if (option.mediaUrl) {
              <img class="choice__image" [src]="src(option.mediaUrl)" [alt]="option.text" />
            }
            <span class="choice__text">{{ option.text }}</span>
          </span>
        </label>
      }
    </fieldset>
  `,
  styles: `
    :host { display: block; }

    .choices {
      display: grid;
      gap: var(--astro-space-2);
      margin: 0;
      padding: 0;
      border: 0;
    }

    .choice {
      display: flex;
      align-items: flex-start;
      gap: var(--astro-space-3);

      /* Comfortably past the forty-four pixel floor: this is answered on a phone,
         standing up, against a clock. */
      min-block-size: 3.25rem;
      padding: var(--astro-space-3);
      border: 1px solid var(--border-subtle);
      border-radius: var(--astro-radius-md);
      background: var(--surface-raised);
      cursor: pointer;

      &:hover { border-color: var(--astro-brand-400); }

      &--picked {
        border-color: var(--astro-brand-600);
        background: var(--astro-brand-50);
      }
    }

    .choice__control {
      position: absolute;
      inline-size: 1px;
      block-size: 1px;
      opacity: 0;
    }

    .choice__mark {
      flex: 0 0 auto;
      inline-size: 1.25rem;
      block-size: 1.25rem;
      margin-block-start: .125rem;
      border: 2px solid var(--border-strong);
      border-radius: 50%;
      background: var(--surface-raised);
    }

    .choice--picked .choice__mark {
      border-color: var(--astro-brand-600);
      background: radial-gradient(var(--astro-brand-600) 45%, transparent 47%);
    }

    /* A square mark where several answers are allowed, because the shape is how
       people know how many they may pick without reading an instruction. */
    :host(.multi) .choice__mark { border-radius: var(--astro-radius-sm); }

    .choice__body { display: grid; gap: var(--astro-space-2); }
    .choice__text { line-height: var(--astro-leading-body); }

    .choice__image {
      max-inline-size: 100%;
      max-block-size: 12rem;
      border-radius: var(--astro-radius-sm);
    }

    .choice__control:focus-visible + .choice__mark {
      outline: 2px solid var(--astro-brand-600);
      outline-offset: 2px;
    }
  `,
  host: { '[class.multi]': '!isSingle()' },
})
export class ChoiceAnswerComponent {
  readonly t = inject(TranslateService).t;

  /** The API's origin in front of a grant the paper already carries. */
  src(url: string | null | undefined): string | null {
    return this.media.absolute(url);
  }

  private readonly media = inject(MediaService);

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly picked = signal<string[]>([]);

  readonly isSingle = computed(() => this.question().type !== 'multi-select');

  private seeded = false;

  constructor() {
    effect(() => {
      const saved = this.response();

      // Seeded once from what was stored, then owned here. Writing the value back
      // on every change would fight the candidate's own clicks.
      if (this.seeded) {
        return;
      }

      this.seeded = true;
      this.picked.set(this.parse(saved));
    });
  }

  isPicked(id: string): boolean {
    return this.picked().includes(id);
  }

  pick(id: string): void {
    if (this.isSingle()) {
      this.picked.set([id]);
    } else {
      this.picked.update(list => (list.includes(id) ? list.filter(x => x !== id) : [...list, id]));
    }

    this.responseChange.emit(JSON.stringify(this.picked()));
  }

  /**
   * Reads a stored response.
   * <p>
   * Tolerant of both shapes the server accepts — an array, or a bare string for a
   * single choice — because a stored answer can outlive a change in how it was
   * written, and a resumed attempt losing its answers is the worst thing this
   * screen could do.
   * </p>
   */
  private parse(saved: string | undefined): string[] {
    if (!saved) {
      return [];
    }

    try {
      const parsed: unknown = JSON.parse(saved);

      if (Array.isArray(parsed)) {
        return parsed.filter((x): x is string => typeof x === 'string');
      }

      return typeof parsed === 'string' ? [parsed] : [];
    } catch {
      return [saved];
    }
  }
}
