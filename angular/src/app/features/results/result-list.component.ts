import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ResultService } from '../../core/api/result.service';
import { ResultRow, ResultSummary } from '../../core/api/result.models';
import { ExamService } from '../../core/api/exam.service';
import { CandidateService } from '../../core/api/candidate.service';
import { ExamDto } from '../../core/api/assessment.models';
import { CandidateGroupDto } from '../../core/api/candidate.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { PagerComponent } from '../../shared/ui/pager.component';

/**
 * Who sat the exam, and how it went.
 *
 * The screen the product was missing. Every permission for it existed and
 * nothing implemented them, so a centre could send an exam to forty students,
 * have every paper marked automatically, and then have no way to see a single
 * score — the review queue lists only sittings that still need a person, which
 * an all-multiple-choice paper never does.
 *
 * The summary strip is above the table on purpose. A coordinator's first
 * question is "how did the group do", not "how did row seventeen do", and the
 * figures are computed over the whole filtered set rather than the visible page
 * so they do not change when somebody turns one.
 */
@Component({
  selector: 'astro-result-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    DatePipe,
    PageHeaderComponent,
    DataStateComponent,
    PagerComponent,
  ],
  templateUrl: './result-list.component.html',
  styleUrl: './result-list.component.scss',
})
export class ResultListComponent {
  private readonly results = inject(ResultService);
  private readonly exams = inject(ExamService);
  private readonly candidates = inject(CandidateService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly items = signal<ResultRow[]>([]);
  readonly totalCount = signal(0);
  readonly summary = signal<ResultSummary | null>(null);

  readonly examOptions = signal<ExamDto[]>([]);
  readonly groups = signal<CandidateGroupDto[]>([]);

  readonly filter = signal('');
  readonly examId = signal('');
  readonly groupId = signal('');
  readonly passedOnly = signal(false);
  readonly awaitingMarking = signal(false);
  readonly page = signal(0);

  readonly pageSize = 25;

  readonly canExport = permissionSignal(P.Results.Export);
  readonly canSeeItemAnalysis = permissionSignal(P.Results.ViewItemAnalysis);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);
  readonly isFiltered = computed(
    () => !!this.filter() || !!this.examId() || !!this.groupId() || this.passedOnly() || this.awaitingMarking(),
  );
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  readonly exporting = signal(false);

  /**
   * Saves the filtered rows as a file.
   *
   * The blob is handed to a link this code creates and clicks, because that is
   * the only way to name a downloaded file from a fetched response.
   */
  exportCsv(): void {
    this.exporting.set(true);

    this.results.exportCsv(this.request()).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = `results-${new Date().toISOString().slice(0, 10)}.csv`;
        link.click();

        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.exporting.set(false);
      },
    });
  }

  constructor() {
    // The two pickers. Neither is worth failing the screen over: results still
    // read fine unfiltered.
    this.exams.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: page => this.examOptions.set(page.items),
    });

    this.candidates.getGroups().subscribe({
      next: groups => this.groups.set(groups),
    });

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.results.getList(this.request()).subscribe({
      next: page => {
        this.items.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });

    // Separately, because it describes the cohort rather than the page and must
    // not be recomputed from what happens to be visible.
    this.results.getSummary(this.request()).subscribe({
      next: summary => this.summary.set(summary),
      error: () => this.summary.set(null),
    });
  }

  applyFilter(): void {
    this.page.set(0);
    this.load();
  }

  setExam(value: string): void {
    this.examId.set(value);
    this.applyFilter();
  }

  setGroup(value: string): void {
    this.groupId.set(value);
    this.applyFilter();
  }

  togglePassed(): void {
    this.passedOnly.update(v => !v);

    // The two toggles contradict each other: a sitting waiting to be marked has
    // not passed anything yet. Turning one on turns the other off rather than
    // returning an empty table nobody can explain.
    if (this.passedOnly()) {
      this.awaitingMarking.set(false);
    }

    this.applyFilter();
  }

  toggleAwaiting(): void {
    this.awaitingMarking.update(v => !v);

    if (this.awaitingMarking()) {
      this.passedOnly.set(false);
    }

    this.applyFilter();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Pass, fail, or neither yet. Three states, because two would be a lie. */
  outcomeKey(row: ResultRow): string {
    if (!row.isGraded) {
      return '::Results:Pending';
    }

    return row.isPassed ? '::Results:Pass' : '::Results:Fail';
  }

  private request() {
    return {
      examId: this.examId() || undefined,
      candidateGroupId: this.groupId() || undefined,
      filter: this.filter() || undefined,
      passedOnly: this.passedOnly() || undefined,
      awaitingMarking: this.awaitingMarking() || undefined,
      skipCount: this.page() * this.pageSize,
      maxResultCount: this.pageSize,
    };
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
