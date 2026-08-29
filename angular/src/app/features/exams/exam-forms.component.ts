import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { StructureService } from '../../core/api/structure.service';
import {
  ExamFormDetailDto,
  ExamFormDto,
  ExamFormStatus,
} from '../../core/api/structure.models';
import { ExamService } from '../../core/api/exam.service';
import { QuestionService } from '../../core/api/question.service';
import { QuestionDto } from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

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
 */
@Component({
  selector: 'astro-exam-forms',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
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

  readonly ExamFormStatus = ExamFormStatus;

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
    this.poolLoading.set(true);

    this.structure.getForm(form.id).subscribe({
      next: detail => {
        this.opened.set(detail);
        this.chosen.set(detail.questions.map(q => q.questionId));
      },
      error: err => this.actionError.set(this.reason(err)),
    });

    // Everything this exam can draw: its own questions and the bank questions
    // its domain and level make available. Listing only the owned ones would
    // tell an author their bank is empty when it is not.
    this.questions.getList({ examId: form.examId, skipCount: 0, maxResultCount: 500 }).subscribe({
      next: page => {
        this.pool.set(page.items);
        this.poolLoading.set(false);
      },
      error: err => {
        this.actionError.set(this.reason(err));
        this.poolLoading.set(false);
      },
    });
  }

  close(): void {
    this.opened.set(null);
    this.chosen.set([]);
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
    const byId = new Map(this.pool().map(q => [q.id, q]));

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

  publish(form: ExamFormDto): void {
    this.act(this.structure.publishForm(form.id));
  }

  retire(form: ExamFormDto): void {
    this.act(this.structure.retireForm(form.id));
  }

  remove(form: ExamFormDto): void {
    this.act(this.structure.deleteForm(form.id));
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
