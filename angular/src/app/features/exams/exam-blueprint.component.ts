import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ExamService } from '../../core/api/exam.service';
import { QuestionService } from '../../core/api/question.service';
import { CatalogService } from '../../core/api/catalog.service';
import {
  BlueprintRuleDto,
  CreateUpdateBlueprintRuleDto,
  QuestionDifficulty,
  QuestionTypeDescriptor,
} from '../../core/api/assessment.models';
import { TopicDto } from '../../core/api/catalog.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * The shape of a drawn paper: how many questions of what kind.
 *
 * This is what makes two drawn papers comparable. Without a blueprint every
 * candidate gets a random handful of whatever is in the bank, and two scores sit
 * side by side meaning nothing. With one, "six grammar, four listening, two of
 * them hard" holds for everybody however the individual questions differ — which
 * is the entire argument for drawing rather than fixing a paper.
 *
 * The route to write one existed on the server and had no client at all, while
 * the papers screen offered "fill from the blueprint" as the recommended way to
 * build a form. There was nothing to fill from and no way to say so.
 *
 * Every rule shows how many bank questions actually match it. An author needs to
 * see "draw 8 from a pool of 5" here, not discover it when a candidate receives
 * a five-question paper.
 */
@Component({
  selector: 'astro-exam-blueprint',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './exam-blueprint.component.html',
  styleUrl: './exam-blueprint.component.scss',
})
export class ExamBlueprintComponent {
  private readonly exams = inject(ExamService);
  private readonly questions = inject(QuestionService);
  private readonly catalog = inject(CatalogService);

  readonly t = inject(TranslateService).t;

  readonly examId = input.required<string>();

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly examTitle = signal('');
  readonly rules = signal<Rule[]>([]);
  readonly topics = signal<TopicDto[]>([]);
  readonly types = signal<QuestionTypeDescriptor[]>([]);

  /** What the bank offers this exam in total, for the "is this even possible" line. */
  readonly bankSize = signal(0);

  readonly canEdit = permissionSignal(P.Exams.Edit);

  readonly total = computed(() => this.rules().reduce((sum, rule) => sum + (rule.questionCount || 0), 0));

  /**
   * Rules that ask for more than the bank holds.
   *
   * A blueprint that cannot be filled does not fail loudly — the builder
   * contributes what it can — so the paper simply comes out short, and nobody
   * finds out until a candidate has sat it.
   */
  readonly unfillable = computed(() =>
    this.rules().filter(rule => rule.availableCount >= 0 && rule.questionCount > rule.availableCount),
  );

  readonly difficulties = [
    { value: QuestionDifficulty.Easy, labelKey: '::Question:Difficulty:Easy' },
    { value: QuestionDifficulty.Medium, labelKey: '::Question:Difficulty:Medium' },
    { value: QuestionDifficulty.Hard, labelKey: '::Question:Difficulty:Hard' },
  ];

  private loadedId?: string;

  constructor() {
    this.questions.getTypes().subscribe({
      next: types => this.types.set(types),
      error: () => undefined,
    });

    // Read through an effect: withComponentInputBinding() sets a routed
    // component's inputs after it is constructed.
    effect(() => {
      const id = this.examId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;
      this.load(id);
    });
  }

  load(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.exams.get(id).subscribe({
      next: exam => {
        this.examTitle.set(exam.title);
        this.bankSize.set(exam.questionCount);

        // Topics from this exam's own domain. Offering the whole organisation's
        // is how a listening rule ends up drawing welding questions.
        this.catalog.getCategories().subscribe({
          next: categories =>
            this.topics.set(categories.find(c => c.id === exam.categoryId)?.topics ?? []),
        });
      },
      error: () => this.examTitle.set(''),
    });

    this.exams.getBlueprint(id).subscribe({
      next: rules => {
        this.rules.set(rules.map(toRule));
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  add(): void {
    this.rules.update(rules => [
      ...rules,
      {
        topicId: '',
        difficulty: null,
        questionType: '',
        questionCount: 5,
        displayOrder: rules.length,

        // Unknown until it is saved and counted server-side. Negative rather than
        // zero, so a fresh rule is not reported as unfillable before anyone has
        // had a chance to save it.
        availableCount: -1,
      },
    ]);

    this.saved.set(false);
  }

  remove(index: number): void {
    this.rules.update(rules => rules.filter((_, i) => i !== index));
    this.saved.set(false);
  }

  patch<K extends keyof Rule>(index: number, key: K, value: Rule[K]): void {
    this.rules.update(rules =>
      rules.map((rule, i) => (i === index ? { ...rule, [key]: value } : rule)),
    );

    this.saved.set(false);
  }

  save(): void {
    const id = this.examId();

    if (!id) {
      return;
    }

    const body: CreateUpdateBlueprintRuleDto[] = this.rules().map((rule, index) => ({
      topicId: rule.topicId || null,
      difficulty: rule.difficulty,
      questionType: rule.questionType || null,
      questionCount: rule.questionCount,
      displayOrder: index,
    }));

    this.saving.set(true);
    this.error.set(null);

    this.exams.setBlueprint(id, body).subscribe({
      next: rules => {
        // Replaced with what came back, because the server counts what each rule
        // can actually draw and that is the number the author needs.
        this.rules.set(rules.map(toRule));
        this.saving.set(false);
        this.saved.set(true);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.saving.set(false);
      },
    });
  }

  topicName(id: string | null): string {
    return this.topics().find(t => t.id === id)?.name ?? this.t('::Blueprint:AnyTopic');
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}

interface Rule {
  topicId: string;
  difficulty: QuestionDifficulty | null;
  questionType: string;
  questionCount: number;
  displayOrder: number;
  availableCount: number;
}

const toRule = (dto: BlueprintRuleDto): Rule => ({
  topicId: dto.topicId ?? '',
  difficulty: dto.difficulty ?? null,
  questionType: dto.questionType ?? '',
  questionCount: dto.questionCount,
  displayOrder: dto.displayOrder,
  availableCount: dto.availableCount,
});
