import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { QuestionService } from '../../core/api/question.service';
import { ExamService } from '../../core/api/exam.service';
import {
  QuestionDifficulty,
  QuestionDto,
  QuestionTypeDescriptor,
} from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * An exam's questions.
 *
 * The screen an author lives in once the first few questions exist, and the one
 * the product was missing: the builder could write a question and nothing could
 * show you what you had written.
 *
 * Two things it does that a plain list would not. It shows a question's own
 * questions and the shared bank's together, marking which is which — because
 * that is what the exam will actually draw, and an author who cannot see the
 * bank half will write it twice. And it surfaces the item statistics beside each
 * row, so a question that has stopped measuring anything is visible where
 * somebody can act on it rather than in a report nobody opens.
 */
@Component({
  selector: 'astro-question-list',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './question-list.component.html',
  styleUrl: './question-list.component.scss',
})
export class QuestionListComponent {
  private readonly questions = inject(QuestionService);
  private readonly exams = inject(ExamService);

  readonly t = inject(TranslateService).t;

  readonly examId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<QuestionDto[]>([]);
  readonly totalCount = signal(0);
  readonly examTitle = signal('');

  readonly types = signal<QuestionTypeDescriptor[]>([]);

  readonly filter = signal('');
  readonly type = signal<string>('');
  readonly difficulty = signal<QuestionDifficulty | null>(null);
  readonly page = signal(0);

  readonly pageSize = 20;

  readonly canCreate = permissionSignal(P.Questions.Create);
  readonly canEdit = permissionSignal(P.Questions.Edit);
  readonly canDelete = permissionSignal(P.Questions.Delete);

  readonly pendingDelete = signal<QuestionDto | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);
  readonly isFiltered = computed(() => !!this.filter() || !!this.type() || this.difficulty() !== null);
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  readonly difficulties = [
    { value: QuestionDifficulty.Easy, labelKey: '::Question:Difficulty:Easy' },
    { value: QuestionDifficulty.Medium, labelKey: '::Question:Difficulty:Medium' },
    { value: QuestionDifficulty.Hard, labelKey: '::Question:Difficulty:Hard' },
  ];

  private loadedId?: string;

  constructor() {
    this.questions.getTypes().subscribe({
      next: types => this.types.set(types),
      error: () => {
        // A missing catalogue costs the type filter, not the list. Failing the
        // whole screen for it would hide the questions over a dropdown.
      },
    });

    // Read through an effect, not in the constructor: withComponentInputBinding()
    // sets a routed component's inputs after it is constructed.
    effect(() => {
      const id = this.examId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;

      this.exams.get(id).subscribe({
        next: exam => this.examTitle.set(exam.title),
        error: () => this.examTitle.set(''),
      });

      this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.questions
      .getList({
        examId: this.examId(),
        filter: this.filter() || undefined,
        type: this.type() || undefined,
        difficulty: this.difficulty() ?? undefined,
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
          this.error.set(this.reason(err));
          this.loading.set(false);
        },
      });
  }

  applyFilter(): void {
    this.page.set(0);
    this.load();
  }

  setType(value: string): void {
    this.type.set(value);
    this.applyFilter();
  }

  setDifficulty(value: QuestionDifficulty | null): void {
    this.difficulty.set(this.difficulty() === value ? null : value);
    this.applyFilter();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  /** Whether this row came from the shared bank rather than from this exam. */
  isFromBank(question: QuestionDto): boolean {
    return !question.examId;
  }

  typeName(question: QuestionDto): string {
    const descriptor = this.types().find(t => t.type === question.type);

    return descriptor ? this.t(descriptor.nameKey) : question.type;
  }

  difficultyKey(question: QuestionDto): string {
    return this.difficulties.find(d => d.value === question.difficulty)?.labelKey
      ?? '::Question:Difficulty:Medium';
  }

  /**
   * Whether a question has stopped measuring anything.
   * <p>
   * Nearly everyone right, or nearly everyone wrong. The second is the one worth
   * catching: a question almost nobody answers correctly is usually a question
   * whose key is wrong, and it reads as a hard question until somebody looks.
   * Only shown once enough people have answered for the number to mean anything.
   * </p>
   */
  itemHealth(question: QuestionDto): 'unmeasured' | 'healthy' | 'tooEasy' | 'tooHard' {
    if (question.timesAnswered < 20 || question.difficultyIndex == null) {
      return 'unmeasured';
    }

    if (question.difficultyIndex >= 0.95) {
      return 'tooEasy';
    }

    return question.difficultyIndex <= 0.15 ? 'tooHard' : 'healthy';
  }

  confirmDelete(): void {
    const question = this.pendingDelete();

    if (!question) {
      return;
    }

    this.pendingDelete.set(null);
    this.run(question.id, this.questions.delete(question.id));
  }

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
        this.actionError.set(this.reason(err));
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
