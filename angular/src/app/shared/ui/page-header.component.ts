import { Component, input } from '@angular/core';

/**
 * The heading block every list and detail screen opens with.
 *
 * Exists so the title, the supporting line and the primary action keep the same
 * relationship on every screen. Left to each screen they drift within a week,
 * and the drift is the thing people read as "unfinished".
 */
@Component({
  selector: 'astro-page-header',
  standalone: true,
  template: `
    <header class="head">
      <div class="head__text">
        <h1 class="head__title">{{ title() }}</h1>
        @if (description()) {
          <p class="head__description">{{ description() }}</p>
        }
      </div>

      <!-- Actions sit at the far end of the reading direction, wherever that is. -->
      <div class="head__actions">
        <ng-content select="[slot=actions]" />
      </div>
    </header>
  `,
  styles: `
    .head {
      display: flex;
      align-items: flex-start;
      gap: var(--astro-space-4);
      flex-wrap: wrap;
      margin-block-end: var(--astro-space-5);
    }

    .head__text { flex: 1; min-inline-size: 16rem; }

    .head__title {
      margin: 0;
      font-size: var(--astro-text-2xl);
    }

    .head__description {
      margin: var(--astro-space-1) 0 0;
      color: var(--text-secondary);
      max-inline-size: 60ch;
    }

    .head__actions {
      display: flex;
      gap: var(--astro-space-2);
      align-items: center;
    }
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
}
