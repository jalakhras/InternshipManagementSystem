import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AttemptAdminService } from '../../core/api/attempt-admin.service';
import { ResultRow } from '../../core/api/result.models';
import { ExamService } from '../../core/api/exam.service';
import { ExamDto } from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { ModalDirective } from '../../shared/ui/modal.directive';
import { PagerComponent } from '../../shared/ui/pager.component';

/**
 * Who is sitting an exam right now.
 *
 * Three permissions described this screen and nothing implemented them, so
 * `Attempts.View`, `.ForceSubmit` and `.Delete` could be granted and enforced
 * nothing at all. What they describe is a real moment: somebody is in the room,
 * their browser has stopped responding, and the coordinator can see neither
 * that the attempt is live nor any way to end it.
 *
 * It refreshes on a timer, because the thing it shows changes without anybody
 * touching the page — and a monitoring screen that needs a manual reload is a
 * screen that shows the past.
 *
 * It asked for a hundred sittings and said nothing about the hundred and first,
 * which is the shape of failure a monitoring screen can least afford: an exam
 * day at a centre of two hundred showed half the room and looked complete. It
 * pages now, and the page survives the timer — a list that jumped back to the
 * first page every ten seconds would be unreadable at exactly the moment it is
 * needed.
 */
@Component({
  selector: 'astro-attempt-monitor',
  standalone: true,
  imports: [FormsModule, DatePipe, PageHeaderComponent, ModalDirective, PagerComponent],
  templateUrl: './attempt-monitor.component.html',
  styleUrl: './attempt-monitor.component.scss',
})
export class AttemptMonitorComponent {
  private readonly attempts = inject(AttemptAdminService);
  private readonly exams = inject(ExamService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly busy = signal(false);

  readonly items = signal<ResultRow[]>([]);
  readonly totalCount = signal(0);
  readonly examOptions = signal<ExamDto[]>([]);

  readonly examId = signal('');
  readonly filter = signal('');
  readonly includeExpired = signal(false);

  readonly page = signal(0);
  readonly pageSize = PAGE_SIZE;

  readonly canEnd = permissionSignal(P.Attempts.ForceSubmit);
  readonly canDelete = permissionSignal(P.Attempts.Delete);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);

  // --- the two confirmations ---
  readonly ending = signal<ResultRow | null>(null);
  readonly endReason = signal('');
  readonly discarding = signal<ResultRow | null>(null);

  constructor() {
    this.exams.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: page => this.examOptions.set(page.items),
    });

    this.load();

    // Ten seconds: fast enough that the list matches the room, slow enough that
    // it is not a load test against the coordinator's own server.
    const timer = setInterval(() => this.load({ quietly: true }), 10_000);

    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  /**
   * Reloads the list.
   *
   * `quietly` skips the spinner, because the timer refresh must not make the
   * table flash every ten seconds while somebody is reading it.
   */
  load(options: { quietly?: boolean } = {}): void {
    if (!options.quietly) {
      this.loading.set(true);
    }

    this.error.set(null);

    this.attempts
      .getRunning({
        examId: this.examId() || undefined,
        filter: this.filter() || undefined,
        includeExpired: this.includeExpired() || undefined,
        skipCount: this.page() * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .subscribe({
        next: page => {
          this.items.set(page.items);
          this.totalCount.set(page.totalCount);

          // Sittings end while somebody is watching them, so the page they are
          // on can stop existing between two refreshes. Stepping back is the
          // only reading of that which does not show an empty table on a screen
          // whose whole job is to say who is still in the room.
          const lastPage = Math.max(0, Math.ceil(page.totalCount / this.pageSize) - 1);

          if (this.page() > lastPage) {
            this.page.set(lastPage);
            this.load({ quietly: true });
            return;
          }

          this.loading.set(false);
        },
        error: err => {
          this.error.set(this.reason(err));
          this.loading.set(false);
        },
      });
  }

  applyFilter(): void {
    // A narrower list is a different list, and page four of the old one means
    // nothing in it.
    this.page.set(0);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  toggleExpired(): void {
    this.includeExpired.update(v => !v);
    this.applyFilter();
  }

  askEnd(row: ResultRow): void {
    this.ending.set(row);
    this.endReason.set('');
    this.actionError.set(null);
  }

  cancelEnd(): void {
    this.ending.set(null);
  }

  confirmEnd(): void {
    const row = this.ending();

    if (!row) {
      return;
    }

    this.busy.set(true);

    this.attempts.forceSubmit(row.attemptId, this.endReason()).subscribe({
      next: () => {
        this.busy.set(false);
        this.ending.set(null);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.busy.set(false);
        this.ending.set(null);
      },
    });
  }

  askDiscard(row: ResultRow): void {
    this.discarding.set(row);
    this.actionError.set(null);
  }

  cancelDiscard(): void {
    this.discarding.set(null);
  }

  confirmDiscard(): void {
    const row = this.discarding();

    if (!row) {
      return;
    }

    this.busy.set(true);

    this.attempts.delete(row.attemptId).subscribe({
      next: () => {
        this.busy.set(false);
        this.discarding.set(null);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.busy.set(false);
        this.discarding.set(null);
      },
    });
  }

  /**
   * Minutes left on a sitting, from its own clock.
   *
   * Computed from the row rather than counted down here: the server owns the
   * deadline, and a browser clock that disagrees would show a candidate more
   * time than they have.
   */
  minutesLeft(row: ResultRow): number {
    const total = row.durationInMinutes;
    const elapsed = Math.floor((Date.now() - new Date(row.startedAt).getTime()) / 60_000);

    return Math.max(0, total - elapsed);
  }

  isExpired(row: ResultRow): boolean {
    return this.minutesLeft(row) <= 0;
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

/**
 * Small on purpose. This is read over somebody's shoulder during an exam, and a
 * page that fits on the screen is worth more here than one that holds everybody.
 */
const PAGE_SIZE = 25;
