import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/**
 * A rating between two labelled ends.
 *
 * Buttons rather than a slider. A slider asks somebody to aim, reports a value
 * they did not choose deliberately, and is close to unusable with a keyboard or
 * a screen reader. Discrete points are what the question actually means.
 */
@Component({
  selector: 'astro-scale-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="scale" role="radiogroup" [attr.aria-label]="t('::Take:YourAnswer')">
      @if (minLabel()) {
        <span class="scale__label">{{ minLabel() }}</span>
      }

      <div class="scale__points">
        @for (point of points(); track point) {
          <button
            type="button"
            role="radio"
            class="scale__point astro-numeric"
            [class.scale__point--picked]="picked() === point"
            [attr.aria-checked]="picked() === point"
            (click)="pick(point)">
            {{ point }}
          </button>
        }
      </div>

      @if (maxLabel()) {
        <span class="scale__label">{{ maxLabel() }}</span>
      }
    </div>
  `,
  styles: `
    :host { display: block; }

    .scale {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--astro-space-3);
    }

    .scale__label {
      color: var(--text-secondary);
      font-size: .9375rem;
    }

    .scale__points { display: flex; flex-wrap: wrap; gap: var(--astro-space-2); }

    .scale__point {
      inline-size: 3rem;
      block-size: 3rem;
      border: 1px solid var(--border-subtle);
      border-radius: 50%;
      background: var(--surface-raised);
      color: var(--text-secondary);
      font-size: 1rem;
      cursor: pointer;

      &:hover { border-color: var(--astro-brand-400); }

      &--picked {
        border-color: var(--astro-brand-600);
        background: var(--astro-brand-600);
        color: var(--text-on-accent);
      }
    }
  `,
})
export class ScaleAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  readonly picked = signal<number | null>(null);

  readonly points = computed<number[]>(() => {
    const display = this.question().display;
    const from = Number(display['min'] ?? 1);
    const to = Number(display['max'] ?? 5);

    // Guarded for the same reason the editor caps it: a scale of one to nine
    // hundred is a typo, and rendering it would hang the page a candidate is
    // being timed on.
    if (!Number.isFinite(from) || !Number.isFinite(to) || to <= from || to - from > 20) {
      return [];
    }

    return Array.from({ length: to - from + 1 }, (_, i) => from + i);
  });

  private seeded = false;

  constructor() {
    effect(() => {
      const saved = this.response();

      if (this.seeded) {
        return;
      }

      this.seeded = true;

      const parsed = Number(saved?.replace(/^"|"$/g, ''));
      this.picked.set(Number.isFinite(parsed) && saved ? parsed : null);
    });
  }

  minLabel(): string {
    return (this.question().display['minLabel'] as string | undefined) ?? '';
  }

  maxLabel(): string {
    return (this.question().display['maxLabel'] as string | undefined) ?? '';
  }

  pick(point: number): void {
    this.picked.set(point);
    this.responseChange.emit(String(point));
  }
}
