import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ReviewService, ReviewAnswer, IntegrityReport } from '../../core/api/review.service';
import { MediaService } from '../../core/media.service';
import { TranslateService } from '../../core/translate.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

/**
 * Marking one attempt.
 *
 * <h4>The rubric adds up for you</h4>
 * A reviewer awards per criterion and the total is computed. Asking somebody to
 * add four numbers under time pressure and type the sum is asking for
 * arithmetic mistakes in people's results — and the mistake is invisible,
 * because a wrong total looks exactly like a considered one.
 *
 * <h4>The comment is feedback, not a note</h4>
 * It reaches the candidate. That is said on the field rather than assumed,
 * because a reviewer who thinks they are writing an internal note writes a
 * different sentence.
 *
 * <h4>Signals are shown, never scored</h4>
 * Behavioural observations sit beside the answer, described plainly, with no
 * suggestion attached. Leaving a tab is not cheating — a phone rings, a
 * notification steals focus — and the software's job is to report what happened
 * so a person can weigh it against everything else they know.
 */
@Component({
  selector: 'astro-review-attempt',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './review-attempt.component.html',
  styleUrl: './review-attempt.component.scss',
})
export class ReviewAttemptComponent {
  private readonly review = inject(ReviewService);

  readonly t = inject(TranslateService).t;


  /**

   * An uploaded answer, fetched with the marker's token.

   *

   * The server sends a path relative to the API, and a plain link from this

   * application would resolve against the wrong origin and carry no token.

   */

  fileUrl(url: string | null | undefined): string | null {
    const blob = url?.split('/api/assessment/media/')[1];

    return blob ? this.media.objectUrl(blob)() : null;
  }

  /**
   * Whether the attachment could not be fetched.
   *
   * <p>
   * A null URL means two different things — not here yet, and never coming —
   * and the screen used to draw the same inert paperclip for both. A marker
   * clicked it, nothing happened, and there was nothing anywhere to tell them
   * why. The commonest cause was a permission: reading media was guarded by a
   * question permission the Marker role does not hold.
   * </p>
   */
  fileFailed(url: string | null | undefined): boolean {
    const blob = url?.split('/api/assessment/media/')[1];

    return blob ? this.media.failed(blob)() : false;
  }


  private readonly media = inject(MediaService);

  readonly attemptId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly answers = signal<ReviewAnswer[]>([]);
  readonly integrity = signal<IntegrityReport | null>(null);

  /** Per answer: the criterion marks a reviewer has entered so far. */
  readonly rubricMarks = signal<Record<string, Record<string, number>>>({});

  /** Per answer: the total, when there is no rubric to add up. */
  readonly directMarks = signal<Record<string, number>>({});

  readonly comments = signal<Record<string, string>>({});

  readonly savingId = signal<string | null>(null);
  readonly savedId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly pending = computed(() => this.answers().filter(a => a.awardedScore == null).length);
  readonly isDone = computed(() => !this.loading() && this.answers().length > 0 && this.pending() === 0);

  private loadedId?: string;

  constructor() {
    effect(() => {
      const id = this.attemptId();

      if (!id || id === this.loadedId) {
        return;
      }

      this.loadedId = id;
      this.load(id);
    });
  }

  private load(attemptId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.review.getAnswers(attemptId).subscribe({
      next: answers => {
        this.answers.set(answers);
        this.seed(answers);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });

    this.review.getIntegrity(attemptId).subscribe({
      next: report => this.integrity.set(report),
      error: () => {
        // Advisory. A missing report must not stop somebody marking.
      },
    });
  }

  /** Brings back whatever was already awarded, so a reopened attempt shows its marks. */
  private seed(answers: ReviewAnswer[]): void {
    const direct: Record<string, number> = {};
    const comments: Record<string, string> = {};

    for (const answer of answers) {
      if (answer.awardedScore != null) {
        direct[answer.answerId] = answer.awardedScore;
      }

      if (answer.reviewComment) {
        comments[answer.answerId] = answer.reviewComment;
      }
    }

    this.directMarks.set(direct);
    this.comments.set(comments);
  }

  // ------------------------------------------------------------------ marking

  criterionMark(answer: ReviewAnswer, criterionId: string): number {
    return this.rubricMarks()[answer.answerId]?.[criterionId] ?? 0;
  }

  setCriterionMark(answer: ReviewAnswer, criterionId: string, value: number | string): void {
    const mark = Math.max(Number(value) || 0, 0);

    this.rubricMarks.update(all => ({
      ...all,
      [answer.answerId]: { ...all[answer.answerId], [criterionId]: mark },
    }));
  }

  /**
   * What this answer is currently worth.
   * <p>
   * Summed from the criteria when there is a rubric, and capped at the question's
   * marks — a reviewer who types eight into a five-mark criterion has made a slip,
   * and a result should not carry it.
   * </p>
   */
  total(answer: ReviewAnswer): number {
    if (answer.rubric.length === 0) {
      return Math.min(this.directMarks()[answer.answerId] ?? 0, answer.maxScore);
    }

    const marks = this.rubricMarks()[answer.answerId] ?? {};

    const sum = answer.rubric.reduce(
      (running, criterion) => running + Math.min(marks[criterion.id] ?? 0, criterion.maxScore),
      0,
    );

    return Math.round(Math.min(sum, answer.maxScore) * 100) / 100;
  }

  setDirectMark(answer: ReviewAnswer, value: number | string): void {
    this.directMarks.update(all => ({ ...all, [answer.answerId]: Number(value) || 0 }));
  }

  comment(answer: ReviewAnswer): string {
    return this.comments()[answer.answerId] ?? '';
  }

  setComment(answer: ReviewAnswer, value: string): void {
    this.comments.update(all => ({ ...all, [answer.answerId]: value }));
  }

  save(answer: ReviewAnswer): void {
    this.savingId.set(answer.answerId);
    this.actionError.set(null);

    this.review
      .grade({
        answerId: answer.answerId,
        awardedScore: this.total(answer),
        rubricScores: answer.rubric.length > 0 ? this.rubricMarks()[answer.answerId] : undefined,
        comment: this.comments()[answer.answerId] || undefined,
      })
      .subscribe({
        next: () => {
          this.savingId.set(null);
          this.savedId.set(answer.answerId);

          // Reflected locally rather than reloading: a reload would scroll a
          // reviewer back to the top of a long attempt they were working down.
          this.answers.update(list =>
            list.map(a =>
              a.answerId === answer.answerId
                ? { ...a, awardedScore: this.total(answer), reviewedAt: new Date().toISOString() }
                : a,
            ),
          );

          setTimeout(() => this.savedId.set(null), 2000);
        },
        error: err => {
          this.savingId.set(null);
          this.actionError.set(this.reason(err));
        },
      });
  }

  /**
   * How the answer arrived, in a sentence.
   * <p>
   * Descriptive on purpose. "Pasted" is a fact; "probably copied" is a verdict,
   * and the software is not the one to reach it.
   * </p>
   */
  arrivalNotes(answer: ReviewAnswer): string[] {
    const notes: string[] = [];

    if (answer.wasPasted) {
      notes.push(this.t('::Review:Arrival:Pasted'));
    }

    if (answer.timeSpentSeconds != null) {
      notes.push(this.t('::Review:Arrival:Time', Math.round(answer.timeSpentSeconds / 60).toString()));
    }

    if (answer.keystrokeCount > 0) {
      notes.push(this.t('::Review:Arrival:Keystrokes', answer.keystrokeCount.toString()));
    }

    return notes;
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
