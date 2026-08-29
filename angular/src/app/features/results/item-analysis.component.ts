import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ResultService } from '../../core/api/result.service';
import { ItemAnalysisRow } from '../../core/api/result.models';
import { ExamService } from '../../core/api/exam.service';
import { ExamDto } from '../../core/api/assessment.models';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';

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
 */
@Component({
  selector: 'astro-item-analysis',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, DataStateComponent],
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
  readonly examId = signal('');
  readonly rows = signal<ItemAnalysisRow[]>([]);

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
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  /** Facility and discrimination are proportions; people read percentages. */
  percent(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
