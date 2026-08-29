import { Component, inject, input } from '@angular/core';
import { TranslateService } from '../../core/translate.service';

/**
 * The three states any remote list can be in, in one place.
 *
 * Every screen that fetches something has to answer: what while it loads, what
 * when it fails, what when there is genuinely nothing. Left to each screen, one
 * of the three gets forgotten — almost always the empty state, which then renders
 * as a blank area the reader interprets as a broken page.
 *
 * The empty state carries an action rather than an apology: someone looking at an
 * empty list wants to know what to do next, not to be told there is nothing.
 *
 * This owns the stylesheet as well as the markup. Twelve screens had grown a
 * private copy of the block below, four of them had drifted, and one had lost
 * `.spinner` altogether — which rendered a dialog's loading state as an empty
 * box. A spinner is also never shown bare here: `role="status"` with nothing
 * inside it announces nothing at all, so the word always comes with it.
 */
@Component({
  selector: 'astro-data-state',
  standalone: true,
  imports: [],
  template: `
    @if (loading()) {
      <div class="state" role="status" aria-live="polite">
        <span class="spinner" aria-hidden="true"></span>
        <p class="state__text">{{ t('::Loading') }}</p>
      </div>
    } @else if (error()) {
      <div class="state state--error" role="alert">
        <i class="bi bi-exclamation-triangle" aria-hidden="true"></i>
        <p class="state__title">{{ t('::CouldNotLoad') }}</p>
        <!-- The actual reason, not a generic apology: it is the only thing that
             tells the reader whether retrying will help. -->
        <p class="state__text">{{ error() }}</p>
        <ng-content select="[slot=retry]" />
      </div>
    } @else if (empty()) {
      <div class="state">
        <i class="bi {{ emptyIcon() }}" aria-hidden="true"></i>
        @if (emptyTitle()) {
          <p class="state__title">{{ emptyTitle() }}</p>
        }
        @if (emptyDescription()) {
          <p class="state__text">{{ emptyDescription() }}</p>
        }
        <ng-content select="[slot=empty-action]" />
      </div>
    }
  `,
  styles: `
    :host { display: block; }

    .state {
      display: grid;
      justify-items: center;
      gap: var(--astro-space-2);
      padding-block: var(--astro-space-7);
      text-align: center;
      color: var(--text-muted);

      i { font-size: 1.75rem; }
    }

    // Semantic rather than the raw hue: --astro-fail-600 does not re-map, and
    // an error icon at 2.7:1 in dark mode is one nobody sees.
    .state--error i { color: var(--status-fail-mark); }

    .state__title {
      margin: 0;
      font-weight: var(--astro-weight-semibold);
      color: var(--text-primary);
    }

    .state__text {
      margin: 0;
      max-inline-size: 44ch;
    }

    .spinner {
      inline-size: 1.5rem;
      block-size: 1.5rem;
      border: 2px solid var(--border-subtle);
      border-block-start-color: var(--accent);
      border-radius: 50%;
      animation: astro-spin .7s linear infinite;
    }

    @keyframes astro-spin { to { transform: rotate(360deg); } }

    @media (prefers-reduced-motion: reduce) {
      .spinner { animation-duration: 2s; }
    }
  `,
})
export class DataStateComponent {
  readonly t = inject(TranslateService).t;

  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly empty = input(false);

  readonly emptyTitle = input('');
  readonly emptyDescription = input<string>();
  readonly emptyIcon = input('bi-inbox');
}
