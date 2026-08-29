import {
  ChangeDetectionStrategy,
  Component,
  ComponentRef,
  DestroyRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';

import { TranslateService } from '../../core/translate.service';
import { TakeService } from './take.service';
import { AttemptState, TakerQuestion } from './take.models';
import { ANSWER_INPUTS, AnswerInput, FALLBACK_ANSWER_INPUT } from './answers/answer-input';

/**
 * Sitting the exam.
 *
 * The highest-risk screen in the product: it is used once, under time pressure,
 * by somebody who cannot come back and try again, and every defect in it costs a
 * real person a real mark.
 *
 * <h4>The server owns the clock</h4>
 * The countdown here is a display. Every save returns the authoritative
 * remaining seconds and this screen takes them, so a candidate whose laptop
 * clock is wrong — or who sets it back deliberately — gains nothing. When the
 * server says the time has gone, the screen submits rather than arguing.
 *
 * <h4>One question in the browser</h4>
 * Questions are fetched one at a time. The whole paper is never in memory, so
 * developer tools show the question in front of them and nothing else. That is
 * also why moving between questions costs a request, which is the right trade.
 *
 * <h4>Nothing is lost</h4>
 * An answer is saved when it changes and again when leaving the question, and
 * the last save wins. Somebody whose connection drops mid-exam should lose the
 * sentence they were typing, not the hour behind it.
 */
@Component({
  selector: 'astro-take-sitting',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './take-sitting.component.html',
  styleUrl: './take-sitting.component.scss',
})
export class TakeSittingComponent {
  private readonly take = inject(TakeService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly t = inject(TranslateService).t;

  readonly token = input.required<string>();

  readonly state = signal<AttemptState | null>(null);
  readonly question = signal<TakerQuestion | null>(null);
  readonly position = signal(1);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly savedAt = signal<Date | null>(null);
  readonly saving = signal(false);
  readonly submitting = signal(false);
  readonly confirmingSubmit = signal(false);

  /** Counted down locally between saves; corrected by the server on every one. */
  readonly secondsRemaining = signal(0);

  readonly answerHost = viewChild('answerHost', { read: ViewContainerRef });

  readonly clock = computed(() => {
    const total = Math.max(this.secondsRemaining(), 0);
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const seconds = total % 60;

    const pad = (n: number) => n.toString().padStart(2, '0');

    return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${pad(minutes)}:${pad(seconds)}`;
  });

  /** Five minutes, then one. Warned twice rather than once, and never only at the end. */
  readonly clockTone = computed<'calm' | 'warn' | 'urgent'>(() => {
    const left = this.secondsRemaining();

    if (left <= 60) {
      return 'urgent';
    }

    return left <= 300 ? 'warn' : 'calm';
  });

  readonly unanswered = computed(() => {
    const state = this.state();

    return state ? state.answered.filter(a => !a).length : 0;
  });

  readonly canGoBack = computed(() => (this.state()?.allowBackNavigation ?? false) && this.position() > 1);
  readonly isLast = computed(() => this.position() >= (this.state()?.totalQuestions ?? 1));

  private answerRef?: ComponentRef<AnswerInput>;
  private pendingResponse: string | null = null;
  private saveTimer?: ReturnType<typeof setTimeout>;
  private tickTimer?: ReturnType<typeof setInterval>;
  private enteredAt = Date.now();
  private keystrokes = 0;
  private backspaces = 0;
  private wasPasted = false;

  constructor() {
    if (!this.take.hasSession()) {
      // Reached without opening a link — a reload, or a bookmark. Back to the
      // entry, which will mint a session or say why it cannot.
      queueMicrotask(() => this.router.navigate(['/exam', this.token()]));
    }

    // Local ticking so the clock moves between exchanges. It only ever counts
    // down; the server's number is what raises it back.
    this.tickTimer = setInterval(() => {
      this.secondsRemaining.update(left => (left > 0 ? left - 1 : 0));

      if (this.secondsRemaining() === 0 && !this.submitting()) {
        this.submit(true);
      }
    }, 1000);

    this.destroyRef.onDestroy(() => {
      clearInterval(this.tickTimer);
      clearTimeout(this.saveTimer);
    });

    this.watchIntegrity();

    effect(() => {
      const host = this.answerHost();
      const question = this.question();

      if (!host || !question) {
        return;
      }

      this.mountAnswer(host, question);
    });

    this.refreshState();
  }

  // ------------------------------------------------------------------ loading

  private refreshState(): void {
    this.take.getState().subscribe({
      next: state => {
        this.state.set(state);
        this.secondsRemaining.set(state.secondsRemaining);

        if (state.isSubmitted) {
          this.router.navigate(['/exam', this.token(), 'result']);
          return;
        }

        // Resume where they left off: the first question with no answer, or the
        // first if everything is answered.
        const next = state.answered.findIndex(a => !a);
        this.goTo(next >= 0 ? next + 1 : 1);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  goTo(position: number): void {
    // Whatever is in the box goes before the question changes. Moving on must
    // never be the thing that loses an answer.
    this.flush();

    this.loading.set(true);
    this.error.set(null);
    this.position.set(position);

    this.take.getQuestion(position).subscribe({
      next: question => {
        this.question.set(question);
        this.resetObservations();
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  next(): void {
    if (!this.isLast()) {
      this.goTo(this.position() + 1);
    }
  }

  previous(): void {
    if (this.canGoBack()) {
      this.goTo(this.position() - 1);
    }
  }

  // ------------------------------------------------------------------ answers

  private mountAnswer(host: ViewContainerRef, question: TakerQuestion): void {
    host.clear();
    this.answerRef = undefined;

    // A type this build does not know still has to be answerable: the server
    // accepts them and sends them to a person to mark. Falling back to a text box
    // is worse than a purpose-built control and far better than nothing.
    const load = ANSWER_INPUTS[question.type] ?? FALLBACK_ANSWER_INPUT;

    void load().then(componentType => {
      const ref = host.createComponent(componentType);

      ref.setInput('question', question);
      ref.setInput('response', question.savedResponse);

      ref.instance.responseChange.subscribe((response: string) => this.onAnswered(response));

      this.answerRef = ref;
    });
  }

  onAnswered(response: string): void {
    this.pendingResponse = response;

    // Debounced, so typing does not become one request per character, and
    // flushed on leaving regardless.
    clearTimeout(this.saveTimer);
    this.saveTimer = setTimeout(() => this.flush(), 800);
  }

  /** Sends whatever is pending. Safe to call when there is nothing. */
  private flush(): void {
    const question = this.question();
    const response = this.pendingResponse;

    clearTimeout(this.saveTimer);

    if (!question || response === null) {
      return;
    }

    this.pendingResponse = null;
    this.saving.set(true);

    this.take
      .saveAnswer({
        questionId: question.id,
        response,
        timeSpentSeconds: Math.round((Date.now() - this.enteredAt) / 1000),
        wasPasted: this.wasPasted,
        keystrokeCount: this.keystrokes,
        backspaceCount: this.backspaces,
      })
      .subscribe({
        next: result => {
          this.saving.set(false);
          this.savedAt.set(new Date(result.savedAt));

          // The authoritative clock. A browser whose time is wrong, or set back
          // on purpose, gains nothing by it.
          this.secondsRemaining.set(result.secondsRemaining);

          this.markAnswered();

          if (result.isExpired) {
            this.submit(true);
          }
        },
        error: err => {
          this.saving.set(false);

          // Kept, so the next save carries it. Losing an answer because one
          // request failed is the worst thing this screen could do.
          this.pendingResponse = response;
          this.error.set(this.reason(err));
        },
      });
  }

  private markAnswered(): void {
    this.state.update(state => {
      if (!state) {
        return state;
      }

      const answered = [...state.answered];
      answered[this.position() - 1] = true;

      return {
        ...state,
        answered,
        answeredCount: answered.filter(Boolean).length,
      };
    });
  }

  // --------------------------------------------------------------- submitting

  askToSubmit(): void {
    this.flush();
    this.confirmingSubmit.set(true);
  }

  submit(automatic = false): void {
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.confirmingSubmit.set(false);

    if (!automatic) {
      this.flush();
    }

    this.take.submit().subscribe({
      next: () => this.router.navigate(['/exam', this.token(), 'result']),
      error: err => {
        this.submitting.set(false);
        this.error.set(this.reason(err));
      },
    });
  }

  // ---------------------------------------------------------------- integrity

  /**
   * Notes how an answer arrived, and when the window stopped being looked at.
   * <p>
   * Observations, not accusations. Leaving the tab is not cheating — a phone
   * rings, a notification steals focus — and the record exists so a person can
   * weigh it beside everything else, never so the software can decide.
   * </p>
   */
  private watchIntegrity(): void {
    const onVisibility = () => {
      if (document.visibilityState === 'hidden') {
        this.take.reportSignal('window-blur');
      }
    };

    const onPaste = () => {
      this.wasPasted = true;
      this.take.reportSignal('paste');
    };

    const onKey = (event: KeyboardEvent) => {
      this.keystrokes++;

      if (event.key === 'Backspace') {
        this.backspaces++;
      }
    };

    document.addEventListener('visibilitychange', onVisibility);
    document.addEventListener('paste', onPaste);
    document.addEventListener('keydown', onKey);

    this.destroyRef.onDestroy(() => {
      document.removeEventListener('visibilitychange', onVisibility);
      document.removeEventListener('paste', onPaste);
      document.removeEventListener('keydown', onKey);
    });
  }

  private resetObservations(): void {
    this.enteredAt = Date.now();
    this.keystrokes = 0;
    this.backspaces = 0;
    this.wasPasted = false;
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
