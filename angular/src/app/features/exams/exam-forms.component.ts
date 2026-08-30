import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { StructureService } from '../../core/api/structure.service';
import {
  ExamFormDetailDto,
  ExamFormDto,
  ExamFormQuestionDto,
  ExamFormStatus,
} from '../../core/api/structure.models';
import { ExamService } from '../../core/api/exam.service';
import { QuestionService } from '../../core/api/question.service';
import { QuestionDto } from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { ModalDirective } from '../../shared/ui/modal.directive';
import { PagerComponent } from '../../shared/ui/pager.component';

/**
 * An exam's named papers.
 *
 * A form is a fixed set of questions in a fixed order, and it exists for one
 * reason: comparability. Two candidates who sat "Form 2" answered the same
 * questions, so their scores mean the same thing — where two random draws from
 * one bank do not. It is also how a retake is a genuinely different paper
 * rather than a redraw that repeats half the questions.
 *
 * All of this worked on the server and had no screen, so the delivery path that
 * serves a named form had nothing to serve.
 *
 * Generating from the blueprint is offered first, because starting from a filled
 * paper and removing two is work somebody will do; starting from an empty list
 * of four hundred questions is work they will not.
 *
 * The bank it draws from pages and searches on the server. It used to ask for
 * five hundred questions and render every one of them as a checkbox in a column
 * beside the paper, so a real bank arrived as a list nobody could read — and a
 * bank past five hundred had questions that could not be put on any paper at
 * all. The paper itself does not page: it is the thing being written, and its
 * order is its meaning.
 */
@Component({
  selector: 'astro-exam-forms',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    DataStateComponent,
    ModalDirective,
    PagerComponent,
  ],
  templateUrl: './exam-forms.component.html',
  styleUrl: './exam-forms.component.scss',
})
export class ExamFormsComponent {
  private readonly structure = inject(StructureService);
  private readonly exams = inject(ExamService);
  private readonly questions = inject(QuestionService);

  readonly t = inject(TranslateService).t;

  /** Bound from the route by withComponentInputBinding(). */
  readonly examId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly saving = signal(false);

  readonly examTitle = signal('');
  readonly forms = signal<ExamFormDto[]>([]);

  readonly canManage = permissionSignal(P.Exams.Edit);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.forms().length === 0);

  // --- creating one ---
  readonly draftOpen = signal(false);
  readonly draftName = signal('');
  readonly draftCode = signal('');

  // --- the paper being edited ---
  readonly opened = signal<ExamFormDetailDto | null>(null);
  readonly pool = signal<QuestionDto[]>([]);
  readonly chosen = signal<string[]>([]);
  readonly poolLoading = signal(false);

  readonly poolFilter = signal('');
  readonly poolPage = signal(0);
  readonly poolPageSize = POOL_PAGE_SIZE;
  readonly poolTotal = signal(0);

  /** Nothing matched the search, as distinct from a bank with nothing in it. */
  readonly poolEmpty = computed(
    () => !this.poolLoading() && this.poolTotal() === 0 && !!this.poolFilter().trim(),
  );

  /**
   * Every question this screen has seen, by id.
   *
   * The paper is held as ids and rendered by resolving them, and once the bank
   * pages they stop being resolvable from what is on screen: a question ticked
   * on page one would vanish from the paper on page two, and then from the
   * save. So the prompts accumulate — seeded from the form itself, which
   * carries them, and added to as pages arrive.
   */
  private readonly known = signal<Map<string, QuestionDto>>(new Map());

  readonly ExamFormStatus = ExamFormStatus;

  /** What the confirmation dialog is asking about, or null when it is closed. */
  readonly pending = signal<PendingAction | null>(null);

  private loadedId?: string;

  constructor() {
    // Read through an effect: withComponentInputBinding() sets a routed
    // component's inputs after it is constructed.
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

    this.structure.getForms(this.examId()).subscribe({
      next: forms => {
        this.forms.set(forms);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  newForm(): void {
    const next = this.forms().length + 1;

    // Named and coded for them. "Form 3" is what a coordinator calls it, and
    // making somebody invent a naming convention on the way in is how the third
    // one ends up called "test final FINAL".
    this.draftName.set(this.t('::Form:DefaultName', String(next)));
    this.draftCode.set(`F${next}`);
    this.draftOpen.set(true);
  }

  cancelDraft(): void {
    this.draftOpen.set(false);
  }

  saveDraft(): void {
    const name = this.draftName().trim();
    const code = this.draftCode().trim();

    if (!name || !code) {
      return;
    }

    this.saving.set(true);
    this.actionError.set(null);

    this.structure.createForm({ examId: this.examId(), name, code }).subscribe({
      next: form => {
        this.saving.set(false);
        this.draftOpen.set(false);
        this.load();
        this.open(form);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  open(form: ExamFormDto): void {
    this.actionError.set(null);
    this.poolFilter.set('');
    this.poolPage.set(0);

    this.structure.getForm(form.id).subscribe({
      next: detail => {
        this.opened.set(detail);
        this.chosen.set(detail.questions.map(q => q.questionId));

        // The paper's prompts arrive with the paper, so it reads correctly before
        // the first page of the bank does — and stays readable however far into
        // the bank somebody pages afterwards.
        this.remember(detail.questions.map(asPoolQuestion));
      },
      error: err => this.actionError.set(this.reason(err)),
    });

    this.loadPool(form.examId);
  }

  /**
   * One page of everything this exam can draw: its own questions and the bank
   * questions its domain and level make available. Listing only the owned ones
   * would tell an author their bank is empty when it is not.
   */
  loadPool(examId?: string): void {
    const id = examId ?? this.opened()?.examId ?? this.examId();

    this.poolLoading.set(true);

    this.questions
      .getList({
        examId: id,
        filter: this.poolFilter().trim() || undefined,
        skipCount: this.poolPage() * this.poolPageSize,
        maxResultCount: this.poolPageSize,
      })
      .subscribe({
        next: page => {
          this.pool.set(page.items);
          this.poolTotal.set(page.totalCount);
          this.remember(page.items);
          this.poolLoading.set(false);
        },
        error: err => {
          this.actionError.set(this.reason(err));
          this.poolLoading.set(false);
        },
      });
  }

  /** Searched on the server, so a bank of any size stays reachable. */
  applyPoolFilter(): void {
    this.poolPage.set(0);
    this.loadPool();
  }

  goToPoolPage(page: number): void {
    this.poolPage.set(page);
    this.loadPool();
  }

  /** The pool's copy wins: it is the whole question, where the paper's is four fields. */
  private remember(questions: QuestionDto[]): void {
    const next = new Map(this.known());

    for (const question of questions) {
      next.set(question.id, { ...next.get(question.id), ...question });
    }

    this.known.set(next);
  }

  close(): void {
    this.opened.set(null);
    this.chosen.set([]);
    this.pool.set([]);
    this.poolTotal.set(0);
    this.poolFilter.set('');
    this.poolPage.set(0);
  }

  isChosen(questionId: string): boolean {
    return this.chosen().includes(questionId);
  }

  /** Order is the order of choosing, which is the order the paper will ask them in. */
  toggle(questionId: string): void {
    const current = this.chosen();

    this.chosen.set(
      current.includes(questionId)
        ? current.filter(id => id !== questionId)
        : [...current, questionId],
    );
  }

  moveUp(index: number): void {
    if (index <= 0) {
      return;
    }

    const next = [...this.chosen()];
    [next[index - 1], next[index]] = [next[index], next[index - 1]];
    this.chosen.set(next);
  }

  moveDown(index: number): void {
    const next = [...this.chosen()];

    if (index >= next.length - 1) {
      return;
    }

    [next[index], next[index + 1]] = [next[index + 1], next[index]];
    this.chosen.set(next);
  }

  /** The chosen questions in their paper order, resolved back to prompts. */
  readonly chosenQuestions = computed(() => {
    const byId = this.known();

    return this.chosen()
      .map(id => byId.get(id))
      .filter((q): q is QuestionDto => !!q);
  });

  generate(): void {
    const form = this.opened();

    if (!form) {
      return;
    }

    this.saving.set(true);
    this.actionError.set(null);

    this.structure.generateForm(form.id).subscribe({
      next: detail => {
        this.opened.set(detail);
        this.chosen.set(detail.questions.map(q => q.questionId));

        // The blueprint draws from the whole bank, not from the page on screen,
        // so most of what it has just chosen has never been seen here.
        this.remember(detail.questions.map(asPoolQuestion));

        this.saving.set(false);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  saveQuestions(): void {
    const form = this.opened();

    if (!form) {
      return;
    }

    this.saving.set(true);
    this.actionError.set(null);

    this.structure.setFormQuestions(form.id, this.chosen()).subscribe({
      next: detail => {
        this.opened.set(detail);
        this.saving.set(false);
        this.load();
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  /**
   * All three are asked before they are done.
   *
   * Only Delete is destructive, but the other two are not small: publishing is
   * the moment a paper becomes something a real candidate can be sent, and
   * retiring takes a live one out of rotation mid-term. They were three
   * identical text links in one row, and Publish sat directly beside Delete.
   */
  askPublish(form: ExamFormDto): void {
    this.pending.set({ kind: 'publish', form });
  }

  askRetire(form: ExamFormDto): void {
    this.pending.set({ kind: 'retire', form });
  }

  askRemove(form: ExamFormDto): void {
    this.pending.set({ kind: 'delete', form });
  }

  cancelPending(): void {
    this.pending.set(null);
  }

  confirmPending(): void {
    const pending = this.pending();

    if (!pending) {
      return;
    }

    const request =
      pending.kind === 'publish'
        ? this.structure.publishForm(pending.form.id)
        : pending.kind === 'retire'
          ? this.structure.retireForm(pending.form.id)
          : this.structure.deleteForm(pending.form.id);

    this.pending.set(null);
    this.act(request);
  }

  /** The title, the confirm button and the destructiveness, in one place. */
  pendingTitleKey(kind: PendingAction['kind']): string {
    switch (kind) {
      case 'publish':
        return '::Form:Publish';
      case 'retire':
        return '::Form:Retire';
      default:
        return '::ConfirmDelete';
    }
  }

  pendingConfirmKey(kind: PendingAction['kind']): string {
    switch (kind) {
      case 'publish':
        return '::Exam:Publish:Confirm';
      case 'retire':
        return '::Form:Retire';
      default:
        return '::Delete';
    }
  }

  statusKey(status: ExamFormStatus): string {
    switch (status) {
      case ExamFormStatus.Published:
        return '::Form:Status:Published';
      case ExamFormStatus.Retired:
        return '::Form:Status:Retired';
      default:
        return '::Form:Status:Draft';
    }
  }

  private act(request: Observable<unknown>): void {
    this.saving.set(true);
    this.actionError.set(null);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.close();
        this.load();
      },
      error: (err: unknown) => {
        this.actionError.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

interface PendingAction {
  readonly kind: 'publish' | 'retire' | 'delete';
  readonly form: ExamFormDto;
}

/**
 * Small, because each row is a whole question prompt in a pane half the width of
 * the screen.
 */
const POOL_PAGE_SIZE = 15;

/**
 * A paper's question as the pool renders one.
 *
 * The form carries the four fields the paper needs to be read; the rest of a
 * QuestionDto belongs to the bank and is filled in if and when that page of it
 * is fetched.
 */
function asPoolQuestion(question: ExamFormQuestionDto): QuestionDto {
  return {
    id: question.questionId,
    text: question.text,
    type: question.type,
    difficulty: question.difficulty,
  } as QuestionDto;
}
