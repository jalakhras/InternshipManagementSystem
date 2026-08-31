import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InternshipManagementSystemPermissions as P } from '../../core/permissions';
import { permissionSignal } from '../../core/permission.signal';

import { ResultService } from '../../core/api/result.service';
import { ItemAnalysisRow } from '../../core/api/result.models';
import { ExamService } from '../../core/api/exam.service';
import { ExamDto } from '../../core/api/assessment.models';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { PagerComponent } from '../../shared/ui/pager.component';
import { failureReason } from '../../core/failure';

/**
 * How each question has behaved across every sitting.
 *
 * This is what stops a bank rotting. A question everybody gets right measures
 * nothing; a question the strongest candidates get wrong more often than the
 * weakest is nearly always mis-keyed, and no amount of re-reading it shows that
 * as reliably as the number does.
 *
 * Worst first, because the point of the screen is the questions to fix, and an
 * alphabetical list is how they stay unfixed. Flags appear only where there is
 * enough data to mean them — flagging a question three people answered teaches
 * an author to ignore flags.
 *
 * The pages are cut here rather than asked for, because the endpoint answers
 * with the whole analysis and the ranking is the analysis: discrimination is
 * computed across every sitting of every question, so a server that returned
 * twenty-five rows would have had to rank all of them anyway. A bank of two
 * hundred questions was rendering two hundred rows with nothing to say how far
 * down the reader was — and the rows this screen exists for are all at the top,
 * which is precisely why nobody noticed the rest.
 */
@Component({
  selector: 'astro-item-analysis',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, DataStateComponent, PagerComponent],
  templateUrl: './item-analysis.component.html',
  styleUrl: './item-analysis.component.scss',
})
export class ItemAnalysisComponent {
  private readonly results = inject(ResultService);
  private readonly exams = inject(ExamService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly examOptions = signal<ExamDto[]>([]);
  /**
   * Whether to link a row to the question behind it.
   *
   * An observer may read this screen and holds nothing in the bank, so the link
   * would take them to a permission refusal. They still get the analysis — the
   * finding is theirs to read; only the repair is not.
   */
  readonly canEditQuestions = permissionSignal(P.Questions.Edit);

  readonly examId = signal('');
  readonly rows = signal<ItemAnalysisRow[]>([]);

  readonly page = signal(0);
  readonly pageSize = PAGE_SIZE;

  readonly visibleRows = computed(() =>
    this.rows().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize),
  );

  readonly hasExam = computed(() => !!this.examId());
  readonly isEmpty = computed(() => this.hasExam() && !this.loading() && this.rows().length === 0);

  constructor() {
    this.exams.getList({ maxResultCount: 100, skipCount: 0 }).subscribe({
      next: page => this.examOptions.set(page.items),
      error: err => this.error.set(this.reason(err)),
    });
  }

  setExam(value: string): void {
    this.examId.set(value);
    this.rows.set([]);
    this.page.set(0);

    if (value) {
      this.load();
    }
  }

  load(): void {
    const id = this.examId();

    if (!id) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.results.getItemAnalysis(id).subscribe({
      next: rows => {
        this.rows.set(rows);
        this.page.set(0);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    this.page.set(page);

    // The table is long enough that turning a page otherwise leaves the reader
    // looking at its middle, wondering what changed.
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /** Facility and discrimination are proportions; people read percentages. */
  percent(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  private reason(err: unknown): string {
    return failureReason(err, this.t);
  }
}

/** Matches the results list, which is the screen a reader arrives from. */
const PAGE_SIZE = 25;
