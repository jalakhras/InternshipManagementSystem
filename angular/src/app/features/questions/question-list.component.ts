import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { QuestionService } from '../../core/api/question.service';
import { ExamService } from '../../core/api/exam.service';
import { CatalogService } from '../../core/api/catalog.service';
import { CategoryDto } from '../../core/api/catalog.models';
import {
  ImportQuestionsResult,
  QuestionDifficulty,
  QuestionDto,
  QuestionTypeDescriptor,
} from '../../core/api/assessment.models';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/** Stands in for "the bank" in the loaded-once check, which is otherwise keyed by exam id. */
const BANK = '__bank__';

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
 *
 * The import panel lives here for the same reason the candidate one lives on
 * the roll: an author's questions are already in a spreadsheet, and the first
 * thing they will try is bringing that file in. It checks before it writes —
 * eighty rows, and the four that are wrong are named with their row number and
 * their column while the spreadsheet is still open. Reporting the damage
 * afterwards would be the same information at the moment it stops being useful.
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
  private readonly catalog = inject(CatalogService);

  readonly t = inject(TranslateService).t;

  /**
   * The exam whose questions these are, or absent on the bank screen.
   *
   * Absent is not a degraded case. A bank question belongs to a domain and a
   * level rather than to a paper, and until this screen existed there was no way
   * to see one — so the bank was written, tested, and invisible.
   */
  readonly examId = input<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<QuestionDto[]>([]);
  readonly totalCount = signal(0);
  readonly examTitle = signal('');

  readonly types = signal<QuestionTypeDescriptor[]>([]);

  readonly filter = signal('');
  readonly type = signal<string>('');
  readonly categoryId = signal<string>('');
  readonly levelId = signal<string>('');

  /** The catalogue, for the bank screen's two filters. */
  readonly categories = signal<CategoryDto[]>([]);

  readonly levels = computed(() => {
    const chosen = this.categories().find(c => c.id === this.categoryId());

    return chosen?.levels ?? [];
  });
  readonly difficulty = signal<QuestionDifficulty | null>(null);
  readonly page = signal(0);

  readonly pageSize = 20;

  readonly canCreate = permissionSignal(P.Questions.Create);
  readonly canEdit = permissionSignal(P.Questions.Edit);
  readonly canDelete = permissionSignal(P.Questions.Delete);

  readonly pendingDelete = signal<QuestionDto | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  // --- the import panel ---
  readonly importing = signal(false);
  readonly importFileName = signal('');
  readonly importChecking = signal(false);
  readonly importResult = signal<ImportQuestionsResult | null>(null);
  readonly importCommitted = signal(false);

  /**
   * Where an imported question is filed, on the bank screen.
   *
   * Kept apart from the list's own filters on purpose. Filtering the list is a
   * way of looking; this decides what gets written, and quietly reusing the
   * filter would mean clearing a filter changed where the next import lands.
   */
  readonly importCategoryId = signal<string>('');
  readonly importLevelId = signal<string>('');

  /**
   * The file itself, as base64, which is what a `byte[]` is over JSON.
   *
   * A signal rather than a field because reading the file is asynchronous, and
   * the check button has to stay disabled until it has finished. It did not, and
   * a quick click posted an empty file — which came back as "that file is
   * empty" about a file that plainly was not.
   */
  private readonly importContent = signal('');

  /** True on the bank screen: no owning exam, so the filters change and so does the heading. */
  readonly isBank = computed(() => !this.examId());

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);
  readonly isFiltered = computed(() =>
    !!this.filter() || !!this.type() || this.difficulty() !== null || !!this.categoryId(),
  );

  /** Where the new-question and edit links go, which differs between the two screens. */
  readonly newLink = computed(() => {
    const exam = this.examId();

    return exam ? ['/exams', exam, 'questions', 'new'] : ['/questions', 'new'];
  });

  editLink(questionId: string): unknown[] {
    const exam = this.examId();

    return exam ? ['/exams', exam, 'questions', questionId] : ['/questions', questionId];
  }
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  /** The domains an imported bank question can be filed under, on the bank screen. */
  readonly importLevels = computed(() => {
    const chosen = this.categories().find(c => c.id === this.importCategoryId());

    return chosen?.levels ?? [];
  });

  /**
   * Whether the file can be checked yet.
   *
   * A bank question needs a domain: without one it is written and then
   * invisible, because no exam draws it and no bank listing shows it.
   */
  readonly canCheckImport = computed(
    () => !!this.importContent() && (!this.isBank() || !!this.importCategoryId()),
  );

  /** Whether the checked file is worth committing. Nothing to write is not an error. */
  readonly canCommitImport = computed(() => {
    const result = this.importResult();

    return !!result && !this.importCommitted() && result.created > 0;
  });

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

      // The bank screen has no exam to wait for, so it loads once and stops. The
      // guard below would otherwise leave it permanently empty.
      if (!id) {
        if (this.loadedId !== BANK) {
          this.loadedId = BANK;

          this.catalog.getCategories().subscribe({
            next: categories => this.categories.set(categories),
          });

          this.load();
        }

        return;
      }

      if (id === this.loadedId) {
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
        bankOnly: this.isBank() ? true : undefined,
        categoryId: this.categoryId() || undefined,
        levelId: this.levelId() || undefined,
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

  setCategory(value: string): void {
    this.categoryId.set(value);

    // A level belongs to one ladder, so it cannot survive the domain changing.
    this.levelId.set('');
    this.applyFilter();
  }

  setLevel(value: string): void {
    this.levelId.set(value);
    this.applyFilter();
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
    return this.typeLabel(question.type);
  }

  /**
   * What this build calls a type.
   *
   * Named from the server's catalogue rather than from a table here, so the
   * preview and the picker cannot disagree about what an author is about to
   * create. Falls back to the identifier, which is at least true.
   */
  typeLabel(type: string): string {
    const descriptor = this.types().find(t => t.type === type);

    return descriptor ? this.t(descriptor.nameKey) : type;
  }

  // ------------------------------------------------------------------ import

  openImport(): void {
    this.importing.set(true);
    this.importFileName.set('');
    this.importContent.set('');
    this.importResult.set(null);
    this.importCommitted.set(false);
    this.actionError.set(null);

    // Seeded from what the screen is already showing, because an author
    // filtering the bank to one level and then importing means that level. It is
    // a starting point rather than a link: the pickers below still decide.
    this.importCategoryId.set(this.categoryId());
    this.importLevelId.set(this.levelId());
  }

  closeImport(): void {
    this.importing.set(false);

    if (this.importCommitted()) {
      this.load();
    }
  }

  /**
   * Takes the chosen file.
   *
   * Read here and held as base64 rather than posted as a form, so the bytes the
   * spreadsheet wrote — byte-order mark included — reach the server, which is
   * the one place that should be deciding what to do about them.
   */
  chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];

    this.importResult.set(null);
    this.importCommitted.set(false);
    this.actionError.set(null);

    this.importContent.set('');

    if (!file) {
      this.importFileName.set('');

      return;
    }

    this.importFileName.set(file.name);

    void file.arrayBuffer().then(buffer => {
      this.importContent.set(this.toBase64(new Uint8Array(buffer)));
    });
  }

  /** Reads the file and reports what would happen. Writes nothing. */
  checkImport(): void {
    this.send(true);
  }

  commitImport(): void {
    this.send(false);
  }

  /**
   * Saves the example spreadsheet.
   *
   * The blob is handed to a link this code creates and clicks, because that is
   * the only way to name a downloaded file from a fetched response.
   */
  downloadTemplate(): void {
    this.questions.importTemplate().subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = 'questions-template.csv';
        link.click();

        URL.revokeObjectURL(url);
      },
      error: err => this.actionError.set(this.reason(err)),
    });
  }

  private send(dryRun: boolean): void {
    this.importChecking.set(true);
    this.actionError.set(null);

    this.questions
      .import({
        content: this.importContent(),
        examId: this.examId(),
        categoryId: this.isBank() ? this.importCategoryId() || undefined : undefined,
        levelId: this.isBank() ? this.importLevelId() || undefined : undefined,
        dryRun,
      })
      .subscribe({
        next: result => {
          this.importResult.set(result);
          this.importCommitted.set(!dryRun);
          this.importChecking.set(false);
        },
        error: err => {
          this.importChecking.set(false);
          this.actionError.set(this.reason(err));
        },
      });
  }

  /**
   * Bytes as base64.
   *
   * In chunks because `String.fromCharCode(...bytes)` on a whole file spreads a
   * hundred thousand arguments across the call stack and throws — on a large
   * file only, which is exactly the one somebody imports on the day it matters.
   */
  private toBase64(bytes: Uint8Array): string {
    const chunk = 0x8000;
    let binary = '';

    for (let index = 0; index < bytes.length; index += chunk) {
      binary += String.fromCharCode(...bytes.subarray(index, index + chunk));
    }

    return btoa(binary);
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
