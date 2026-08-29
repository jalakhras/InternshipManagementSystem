import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ExamService } from '../../core/api/exam.service';
import { ExamDto, ExamStatus } from '../../core/api/assessment.models';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * Which exam do you want to send?
 *
 * Sending always happens in the context of one exam — "assign", with nothing
 * named, is a question rather than an action — and that reasoning was used to
 * justify having no `/assignments` screen at all. But the sidebar linked there
 * and so did the dashboard's fourth card, and both fell through to the
 * dashboard. Two of the most prominent links in the product did nothing.
 *
 * So the answer is to ask the question rather than to remove the link. Only
 * published exams appear: a draft cannot be sent, and offering one is an error
 * somebody has to hit before they learn it.
 */
@Component({
  selector: 'astro-assignment-picker',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  templateUrl: './assignment-picker.component.html',
  styleUrl: './assignment-picker.component.scss',
})
export class AssignmentPickerComponent {
  private readonly exams = inject(ExamService);

  readonly t = inject(TranslateService).t;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<ExamDto[]>([]);

  readonly isEmpty = computed(() => !this.loading() && !this.error() && this.items().length === 0);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.exams.getList({ status: ExamStatus.Published, skipCount: 0, maxResultCount: 100 }).subscribe({
      next: page => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
