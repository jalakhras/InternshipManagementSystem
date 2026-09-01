import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { ResultService } from '../../core/api/result.service';
import { ResultDetail } from '../../core/api/result.models';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { DataStateComponent } from '../../shared/ui/data-state.component';
import { failureReason } from '../../core/failure';

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
  imports: [RouterLink, DatePipe, PageHeaderComponent, DataStateComponent],
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

  /**
   * Whether this paper ended in a way that changes how its score should be read.
   *
   * A candidate who pressed submit needs no explaining, and a line saying so on
   * every result is noise on the screen a coordinator reads most. The three that
   * are worth saying are the three where the paper stopped before the person
   * was finished with it: time ran out here or on the server, or somebody ended
   * the sitting.
   *
   * None of it was shown at all. The reason crossed the wire and no template
   * rendered it, so a paper cut short read exactly like one that was completed —
   * and the note the coordinator was asked to write, under a label promising it
   * would be recorded, was written into a column nothing read back.
   */
  readonly endedUnusually = computed(() => {
    const reason = this.detail()?.summary.endReason;

    return reason === 'TimedOutInBrowser'
        || reason === 'TimedOutOnServer'
        || reason === 'EndedByAdministrator';
  });

  /**
   * Whether this paper had parts at all.
   *
   * Unlike the topic block there is no "no sections yet" note for the empty
   * case: most exams are one undivided paper, and a heading that appears on
   * every result to say a feature was not used is noise on the screen a
   * coordinator reads most.
   */
  readonly hasSections = computed(() => (this.detail()?.bySection.length ?? 0) > 0);

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
    return failureReason(err, this.t);
  }
}
