import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Observable } from 'rxjs';

import { ExamService } from '../../core/api/exam.service';
import { ExamDto, ExamStatus } from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { TranslateService } from '../../core/translate.service';
import { permissionSignal } from '../../core/permission.signal';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

import { StatusChipComponent, StatusTone } from '../../shared/ui/status-chip.component';
import { ModalDirective } from '../../shared/ui/modal.directive';
import { failureReason } from '../../core/failure';

/**
 * The exam list.
 *
 * A table rather than cards: this is a working list an author returns to daily,
 * scanning for one exam among many. Cards look better in a screenshot and are
 * slower to scan.
 */
@Component({
  selector: 'astro-exam-list',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, PageHeaderComponent, StatusChipComponent, ModalDirective],
  templateUrl: './exam-list.component.html',
  styleUrl: './exam-list.component.scss',
})
export class ExamListComponent {
  private readonly exams = inject(ExamService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<ExamDto[]>([]);
  readonly totalCount = signal(0);

  readonly filter = signal('');
  readonly status = signal<ExamStatus | null>(null);
  readonly page = signal(0);

  readonly pageSize = 20;

  readonly canCreate = permissionSignal(P.Exams.Create);

  // Writing questions is its own permission: a coordinator who may create an
  // exam is not necessarily the person who writes its questions.
  readonly canAddQuestions = permissionSignal(P.Questions.Create);
  readonly canEdit = permissionSignal(P.Exams.Edit);
  readonly canDelete = permissionSignal(P.Exams.Delete);
  readonly canPublish = permissionSignal(P.Exams.Publish);

  /**
   * The exam awaiting confirmation, or null. Holding the row rather than its id
   * lets the prompt name what is about to be deleted — "delete this?" with no
   * subject is how the wrong thing gets deleted.
   */
  readonly pendingDelete = signal<ExamDto | null>(null);

  /** The row being published, archived or deleted, so it can say so and refuse a second click. */
  readonly busyId = signal<string | null>(null);

  readonly actionError = signal<string | null>(null);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);

  /** True when a filter is set, so the empty state can say "no matches" rather than "none yet". */
  readonly isFiltered = computed(() => !!this.filter() || this.status() !== null);

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  /**
   * Publishing is refused on a draft that would not survive it, so the action is
   * offered only where it can succeed. The server checks again regardless: this
   * is courtesy, not enforcement.
   */
  canPublishRow(exam: ExamDto): boolean {
    return this.canPublish() && exam.status === ExamStatus.Draft;
  }

  canArchiveRow(exam: ExamDto): boolean {
    return this.canPublish() && exam.status === ExamStatus.Published;
  }

  publish(exam: ExamDto): void {
    this.run(exam.id, this.exams.publish(exam.id));
  }

  archive(exam: ExamDto): void {
    this.run(exam.id, this.exams.archive(exam.id));
  }

  confirmDelete(): void {
    const exam = this.pendingDelete();

    if (!exam) {
      return;
    }

    this.pendingDelete.set(null);
    this.run(exam.id, this.exams.delete(exam.id));
  }

  /**
   * Runs a row action, then reloads rather than patching the row in place.
   * <p>
   * Publishing can fail on the server for a reason this screen cannot know — an
   * unsatisfiable blueprint, a bank that shrank — and a row updated optimistically
   * would show a state the database does not have.
   * </p>
   */
  private run(id: string, action: Observable<unknown>): void {
    this.busyId.set(id);
    this.actionError.set(null);

    action.subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: err => {
        this.busyId.set(null);
        this.actionError.set(failureReason(err, this.t));
      },
    });
  }

  readonly statusOptions = [
    { value: null, labelKey: '::Exam:Status:All' },
    { value: ExamStatus.Draft, labelKey: '::Exam:Status:Draft' },
    { value: ExamStatus.Published, labelKey: '::Exam:Status:Published' },
    { value: ExamStatus.Archived, labelKey: '::Exam:Status:Archived' },
  ];

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.exams
      .getList({
        filter: this.filter() || undefined,
        status: this.status() ?? undefined,
        skipCount: this.page() * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .subscribe({
        next: result => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: err => {
          // The real reason, not a generic apology: it is the only thing that
          // tells the reader whether retrying will help.
          this.error.set(failureReason(err, this.t));
          this.loading.set(false);
        },
      });
  }

  applyFilter(): void {
    this.page.set(0);
    this.load();
  }

  setStatus(value: ExamStatus | null): void {
    this.status.set(value);
    this.applyFilter();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  statusTone(status: ExamStatus): StatusTone {
    switch (status) {
      case ExamStatus.Published:
        return 'pass';
      case ExamStatus.Draft:
        return 'pending';
      default:
        return 'neutral';
    }
  }

  statusLabel(status: ExamStatus): string {
    switch (status) {
      case ExamStatus.Published:
        return this.t('::Exam:Status:Published');
      case ExamStatus.Draft:
        return this.t('::Exam:Status:Draft');
      default:
        return this.t('::Exam:Status:Archived');
    }
  }

  statusIcon(status: ExamStatus): string {
    switch (status) {
      case ExamStatus.Published:
        return 'bi-broadcast';
      case ExamStatus.Draft:
        return 'bi-pencil';
      default:
        return 'bi-archive';
    }
  }

  /**
   * How many questions each taker gets, against how many the bank holds.
   *
   * Shown because the difference is the whole anti-leak mechanism: 25 drawn from
   * 120 means two candidates sit different papers. Equal numbers mean everyone
   * sits the same one, which is worth seeing at a glance.
   */
  formSummary(exam: ExamDto): string {
    if (!exam.questionsPerForm || exam.questionsPerForm >= exam.questionCount) {
      return `${exam.questionCount}`;
    }

    return `${exam.questionsPerForm} / ${exam.questionCount}`;
  }

  drawsFromPool(exam: ExamDto): boolean {
    return !!exam.questionsPerForm && exam.questionsPerForm < exam.questionCount;
  }
}
