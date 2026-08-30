import { Component, computed, inject, input, output } from '@angular/core';

import { TranslateService } from '../../core/translate.service';

/**
 * Where you are in a list, and how to move.
 *
 * Six screens had grown a private copy of the same three elements, and every one
 * of them was hidden behind `@if (totalPages() > 1)`. That guard is why the
 * owner reported "لا يوجد pagination" from a tenant of nine people: on a small
 * tenant nothing paged, so nothing said how much there was, and a list that
 * shows twenty rows out of a hundred and forty-eight looks exactly like a list
 * of twenty. The count sentence therefore renders whenever there is anything at
 * all; only the arrows wait for a second page.
 *
 * The sentence, not the arrows, is the point. "٢١–٤٠ من ١٤٨" answers both
 * questions a reader has — where am I, and how much is there — where "‹ ›"
 * answers neither, and "٢ / ٨" only the first.
 *
 * `page` is zero-based, because every caller's `skipCount` is.
 */
@Component({
  selector: 'astro-pager',
  standalone: true,
  template: `
    @if (totalCount() > 0) {
      <nav class="pager" [attr.aria-label]="label() || t('::Pagination')">
        @if (totalPages() > 1) {
          <button
            type="button"
            class="btn btn-sm btn-outline-secondary"
            [disabled]="page() === 0"
            (click)="go(page() - 1)">
            <!-- chevron-LEFT under astro-flip, which mirrors it in Arabic: back
                 points left in English and right in Arabic. The five inline
                 pagers this replaces name the opposite icon and flip that, so
                 their Previous points forwards in both directions — worth
                 correcting here rather than copying. -->
            <i class="bi bi-chevron-left astro-flip" aria-hidden="true"></i>
            {{ t('::Previous') }}
          </button>
        }

        <!-- Announced rather than merely redrawn: pressing Next moves nothing a
             screen-reader user can feel unless the new position is spoken. -->
        <span class="pager__where" aria-live="polite">
          @if (totalPages() > 1) {
            <span class="pager__count astro-numeric">{{ page() + 1 }} / {{ totalPages() }}</span>
          }
          <!-- Not .astro-numeric: this is a sentence with Arabic in it, and that
               class hands the whole run to a monospace family with no Arabic
               glyphs and reverses it. Tabular figures without the direction
               switch are what a sentence wants. -->
          <span class="pager__range">{{ rangeText() }}</span>
        </span>

        @if (totalPages() > 1) {
          <button
            type="button"
            class="btn btn-sm btn-outline-secondary"
            [disabled]="page() + 1 >= totalPages()"
            (click)="go(page() + 1)">
            {{ t('::Next') }}
            <i class="bi bi-chevron-right astro-flip" aria-hidden="true"></i>
          </button>
        }
      </nav>
    }
  `,
  styles: `
    :host { display: block; }

    .pager {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-wrap: wrap;
      gap: var(--astro-space-3);
      margin-block-start: var(--astro-space-4);
    }

    .pager__where {
      display: grid;
      justify-items: center;
      gap: 2px;
    }

    .pager__count {
      color: var(--text-secondary);
      font-size: var(--astro-text-sm);
    }

    .pager__range {
      color: var(--text-muted);
      font-size: var(--astro-text-xs);
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class PagerComponent {
  readonly t = inject(TranslateService).t;

  /** Zero-based, matching the `skipCount` the caller computes from it. */
  readonly page = input.required<number>();

  readonly pageSize = input.required<number>();

  /** Everything the filter matches, not what this page happens to hold. */
  readonly totalCount = input.required<number>();

  /**
   * The navigation's accessible name.
   *
   * Defaulted rather than required: a page with one list wants "تصفّح الصفحات",
   * and only a page with two of them needs to say which is which.
   */
  readonly label = input<string>('');

  readonly pageChange = output<number>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeText = computed(() => {
    const first = this.page() * this.pageSize() + 1;
    const last = Math.min(this.totalCount(), (this.page() + 1) * this.pageSize());

    return this.t('::Pager:Range', String(first), String(last), String(this.totalCount()));
  });

  go(page: number): void {
    if (page < 0 || page >= this.totalPages() || page === this.page()) {
      return;
    }

    this.pageChange.emit(page);
  }
}
