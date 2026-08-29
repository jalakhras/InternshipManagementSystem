import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { ResultService } from '../../core/api/result.service';
import { ResultDetail } from '../../core/api/result.models';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * One person's sitting, question by question.
 *
 * The topic breakdown sits above the answers because it is the part somebody
 * acts on: "weak on listening" tells a training centre what to teach next, where
 * a list of twenty right-and-wrong marks tells them to read twenty rows. When
 * the questions carry no topic the section says so and points at the catalogue,
 * rather than rendering an empty box that reads as a bug.
 */
@Component({
  selector: 'astro-result-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  templateUrl: './result-detail.component.html',
  styleUrl: './result-detail.component.scss',
})
export class ResultDetailComponent {
  private readonly results = inject(ResultService);

  readonly t = inject(TranslateService).t;

  /** Bound from the route by withComponentInputBinding(). */
  readonly attemptId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detail = signal<ResultDetail | null>(null);

  readonly hasTopics = computed(() => (this.detail()?.byTopic.length ?? 0) > 0);

  private loadedId?: string;

  constructor() {
    // Read through an effect, not the constructor: withComponentInputBinding()
    // sets a routed component's inputs after it is built, so reading the id
    // straight away gets undefined and the screen loads nothing.
    effect(() => {
      const id = this.attemptId();

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

    this.results.get(id).subscribe({
      next: detail => {
        this.detail.set(detail);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  reload(): void {
    const id = this.attemptId();

    if (id) {
      this.load(id);
    }
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
