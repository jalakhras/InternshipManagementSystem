import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';

import { ReviewService, ReviewQueueItem } from '../../core/api/review.service';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { failureReason } from '../../core/failure';

/**
 * Attempts waiting on a person.
 *
 * Ordered oldest first and nothing else. A queue that lets a reviewer choose
 * what to mark next is a queue where the hard ones are never marked — and the
 * candidate whose answer is hardest to read is the one who waits longest.
 *
 * Two numbers per row, because they answer different questions. How many
 * answers are pending tells a reviewer how long this will take; the provisional
 * score tells them whether it matters — an attempt already past the pass mark
 * on its automatic marks alone is a different piece of work from one sitting on
 * the line.
 */
@Component({
  selector: 'astro-review-queue',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  templateUrl: './review-queue.component.html',
  styleUrl: './review-queue.component.scss',
})
export class ReviewQueueComponent {
  private readonly review = inject(ReviewService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<ReviewQueueItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(0);

  /**
   * Whether to list sittings already marked instead of those waiting.
   * <p>
   * The queue's job is what is waiting, so that stays the default. But an
   * attempt left it the moment its last answer was marked and never came back —
   * so a marker who typed 7 where they meant 17 had no route to that sitting at
   * all. A mark is a person's judgement, and people revise judgements; making
   * the revision impossible does not make the first mark more correct, only
   * permanent.
   * </p>
   */
  readonly finished = signal(false);

  showFinished(value: boolean): void {
    this.finished.set(value);
    this.page.set(0);
    this.load();
  }

  readonly pageSize = 20;

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.review
      .getQueue({
        skipCount: this.page() * this.pageSize,
        maxResultCount: this.pageSize,
        finished: this.finished(),
      })
      .subscribe({
        next: result => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: err => {
          this.error.set(failureReason(err, this.t));
          this.loading.set(false);
        },
      });
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /**
   * How long this attempt has been waiting, in whole days.
   * <p>
   * Shown because a queue without it hides its own backlog: twelve rows look the
   * same whether the oldest arrived this morning or three weeks ago, and only one
   * of those is a problem.
   * </p>
   */
  waitingDays(item: ReviewQueueItem): number {
    const submitted = new Date(item.submittedAt).getTime();

    return Math.max(Math.floor((Date.now() - submitted) / 86_400_000), 0);
  }

  isStale(item: ReviewQueueItem): boolean {
    return this.waitingDays(item) >= 3;
  }
}
