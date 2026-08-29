import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissionService } from '@abp/ng.core';

import { ExamService } from '../../core/api/exam.service';
import {
  CreateUpdateExamDto,
  ExamDto,
  ExamMode,
  ExamStatus,
  PublishCheckDto,
} from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusChipComponent } from '../../shared/ui/status-chip.component';

/**
 * Creating and editing an exam.
 *
 * Grouped by the decision being made rather than by the shape of the DTO: what it
 * is, how long people get, and how the paper is assembled. The last group is where
 * the anti-leak behaviour lives, so it is stated in sentences rather than left as
 * four unexplained switches.
 */
@Component({
  selector: 'astro-exam-form',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, StatusChipComponent],
  templateUrl: './exam-form.component.html',
  styleUrl: './exam-form.component.scss',
})
export class ExamFormComponent {
  private readonly exams = inject(ExamService);
  private readonly router = inject(Router);
  private readonly permission = inject(PermissionService);

  readonly t = inject(TranslateService).t;

  /** Bound from the route. Absent means this is a new exam. */
  readonly id = input<string>();

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly exam = signal<ExamDto | null>(null);
  readonly publishCheck = signal<PublishCheckDto | null>(null);
  readonly showPublishPanel = signal(false);

  readonly form = signal<CreateUpdateExamDto>({
    title: '',
    description: '',
    mode: ExamMode.Assessment,
    timeLimitInMinutes: 60,
    passingPercentage: 60,
    shuffleQuestions: true,
    shuffleOptions: true,
    oneQuestionAtATime: true,
    allowBackNavigation: true,
    collectIntegritySignals: true,
    isScheduled: false,
  });

  readonly isNew = computed(() => !this.id());
  readonly canEdit = this.permission.getGrantedPolicy(P.Exams.Edit);
  readonly canPublish = this.permission.getGrantedPolicy(P.Exams.Publish);

  readonly isPublished = computed(() => this.exam()?.status === ExamStatus.Published);

  readonly ExamMode = ExamMode;

  constructor() {
    const id = this.id();

    if (id) {
      this.loadExam(id);
    }
  }

  private loadExam(id: string): void {
    this.loading.set(true);

    this.exams.get(id).subscribe({
      next: exam => {
        this.exam.set(exam);
        this.form.set({
          title: exam.title,
          description: exam.description,
          categoryId: exam.categoryId,
          levelId: exam.levelId,
          mode: exam.mode,
          timeLimitInMinutes: exam.timeLimitInMinutes,
          passingPercentage: exam.passingPercentage,
          questionsPerForm: exam.questionsPerForm,
          shuffleQuestions: exam.shuffleQuestions,
          shuffleOptions: exam.shuffleOptions,
          oneQuestionAtATime: exam.oneQuestionAtATime,
          allowBackNavigation: exam.allowBackNavigation,
          collectIntegritySignals: exam.collectIntegritySignals,
          isScheduled: exam.isScheduled,
          scheduledStartTime: exam.scheduledStartTime,
          scheduledEndTime: exam.scheduledEndTime,
        });
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  patch<K extends keyof CreateUpdateExamDto>(key: K, value: CreateUpdateExamDto[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    const id = this.id();
    const request = id
      ? this.exams.update(id, this.form())
      : this.exams.create(this.form());

    request.subscribe({
      next: exam => {
        this.saving.set(false);
        this.exam.set(exam);

        if (!id) {
          // A new exam has no questions yet, so the next thing anyone does is add
          // them. Land on the saved exam rather than back on the list.
          this.router.navigate(['/exams', exam.id]);
        }
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * Asks what publishing would do before offering it.
   *
   * The whole list at once, rather than discovering the problems one refused click
   * at a time — publishing is the gate between a draft and something a real person
   * sits, and someone led through three refusals stops reading the fourth.
   */
  openPublishPanel(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    this.showPublishPanel.set(true);
    this.publishCheck.set(null);

    this.exams.checkPublish(id).subscribe({
      next: check => this.publishCheck.set(check),
      error: err => this.error.set(this.reason(err)),
    });
  }

  publish(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    this.saving.set(true);

    this.exams.publish(id).subscribe({
      next: exam => {
        this.exam.set(exam);
        this.showPublishPanel.set(false);
        this.saving.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * The server's own message when there is one.
   *
   * Error codes are localised server-side, so the message that comes back is
   * already a sentence in the reader's language — passing it through beats
   * inventing a generic one here.
   */
  private reason(err: unknown): string {
    const e = err as { error?: { error?: { message?: string } }; message?: string };
    return e?.error?.error?.message ?? e?.message ?? this.t('::UnknownError');
  }
}
