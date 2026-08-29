import { Component, inject } from '@angular/core';
import { TranslateService } from '../../core/translate.service';
import { ActivatedRoute } from '@angular/router';

/**
 * Stands in for a screen that arrives in phase 3b.
 *
 * Present so the navigation and route tree can be exercised — including the RTL
 * pass and the permission filtering — before any feature screen exists. It says
 * plainly that the screen is not built rather than showing an empty table, which
 * would read as a failure.
 */
@Component({
  selector: 'astro-placeholder',
  standalone: true,
  imports: [],
  template: `
    <div class="pending">
      <i class="bi bi-cone-striped" aria-hidden="true"></i>
      <h1>{{ t(titleKey) }}</h1>
      <p>{{ t('::ScreenNotBuiltYet') }}</p>
    </div>
  `,
  styles: `
    .pending {
      display: grid;
      justify-items: center;
      gap: var(--astro-space-2);
      padding-block: var(--astro-space-8);
      text-align: center;
      color: var(--text-secondary);

      i { font-size: 2rem; color: var(--text-muted); }
      h1 { color: var(--text-primary); }
      p { max-inline-size: 32ch; }
    }
  `,
})
export class PlaceholderComponent {
  private readonly route = inject(ActivatedRoute);

  readonly t = inject(TranslateService).t;

  readonly titleKey = this.route.snapshot.data['titleKey'] ?? '::Loading';
}
