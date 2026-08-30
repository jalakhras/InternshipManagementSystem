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
import { Observable, shareReplay } from 'rxjs';

import { MediaService } from '../../core/media.service';
import { TranslateService } from '../../core/translate.service';
import { TakeService } from './take.service';
import { AttemptState, IntegritySignalType, SaveAnswerResult, TakerQuestion } from './take.models';
import {
  ANSWER_INPUTS,
  AnswerAttachment,
  AnswerInput,
  FALLBACK_ANSWER_INPUT,
} from './answers/answer-input';

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

  private readonly media = inject(MediaService);

  readonly t = inject(TranslateService).t;

  /**
   * A media URL from the server, made absolute.
   *
   * The paper arrives with the grant already in the address — a candidate has no
   * account and nothing in this page could attach a header to an `<audio src>`
   * anyway — so all it needs is the API's origin in front of it. Without that it
   * resolves against the application and the clip never plays.
   */
  src(url: string | null | undefined): string | null {
    return this.media.absolute(url);
  }

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

  /**
   * One entry per question: its number, whether it has an answer, and whether it
   * is the one on screen.
   * <p>
   * The server has sent this map on every exchange since the paper was built —
   * the field's own comment says it is "so the map can show what is left without
   * fetching the paper" — and nothing ever drew it. So the submit dialog could
   * tell a candidate that two questions had no answer and offer them no way to
   * reach either one, with the clock running.
   * </p>
   */
  readonly map = computed(() => {
    const state = this.state();

    if (!state) {
      return [];
    }

    return state.answered.map((answered, index) => ({
      position: index + 1,
      answered,
      current: index + 1 === this.position(),
    }));
  });

  /**
   * Whether the map is a way to move or only a picture of progress.
   * <p>
   * When an exam refuses back navigation, jumping is not offered at all —
   * deliberately, and not only because going back is barred. Jumping *forward*
   * would strand every question it skipped, since there would be no way to
   * return to them: a candidate would gain a control that quietly cost them
   * marks. So the map still shows what is left, and moving stays with Next.
   * </p>
   */
  readonly canJump = computed(() => this.state()?.allowBackNavigation ?? false);

  jumpTo(position: number): void {
    if (this.canJump() && position !== this.position()) {
      this.goTo(position);
    }
  }

  /** From the submit dialog: the first question still without an answer. */
  goToFirstUnanswered(): void {
    const first = this.state()?.answered.findIndex(a => !a) ?? -1;

    if (first < 0) {
      return;
    }

    this.confirmingSubmit.set(false);
    this.goTo(first + 1);
  }

  readonly canGoBack = computed(() => (this.state()?.allowBackNavigation ?? false) && this.position() > 1);
  readonly isLast = computed(() => this.position() >= (this.state()?.totalQuestions ?? 1));

  private answerRef?: ComponentRef<AnswerInput>;
  private pendingResponse: string | null = null;
  private pendingAttachment: AnswerAttachment | null = null;
  private saveTimer?: ReturnType<typeof setTimeout>;
  private blockedTimer?: ReturnType<typeof setTimeout>;

  /** The key of the line explaining what was just refused, while it is showing. */
  readonly blocked = signal<string | null>(null);

  /**
   * The save currently on the wire, so submitting can wait for it.
   * <p>
   * Saving is fire-and-forget everywhere else, which is right: a candidate
   * moving between questions should never wait on the network. Submitting is
   * the one place it is wrong. Answering the last question and pressing Finish
   * straight away sent the submit alongside the save, and the two raced — the
   * submit sometimes arrived first, and the exam was scored without the answer
   * the candidate had just given. Silent, intermittent, and it costs them the
   * mark.
   * </p>
   */
  private inFlightSave: Observable<SaveAnswerResult> | null = null;
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

      // The two types whose answer is a file. They have already stored it by the
      // time this fires; what arrives is the name to hang on the answer.
      ref.instance.attachment?.subscribe((file: AnswerAttachment) => this.onAttached(file));

      this.answerRef = ref;
    });
  }

  /**
   * A stored file is the answer.
   * <p>
   * Sent at once rather than debounced: the upload has already happened, so
   * there is nothing to coalesce, and the candidate should see it recorded
   * immediately after waiting for it.
   * </p>
   */
  onAttached(file: AnswerAttachment): void {
    this.pendingAttachment = file;

    this.markAnswered();
    this.flush();
  }

  onAnswered(response: string): void {
    this.pendingResponse = response;

    // Counted as answered the moment they answer it, not when the server says so.
    //
    // The submit confirmation reads this count, and saving is debounced by most
    // of a second — so answering the last question and pressing Finish straight
    // away produced "you have 1 unanswered" about a question the candidate was
    // looking at and had just filled in. Alarming, wrong, and at the worst
    // possible moment.
    //
    // From the candidate's point of view they have answered it. If the save
    // fails they are told separately, and the response is kept and retried.
    if (response.trim().length > 0) {
      this.markAnswered();
    }

    // Debounced, so typing does not become one request per character, and
    // flushed on leaving regardless.
    clearTimeout(this.saveTimer);
    this.saveTimer = setTimeout(() => this.flush(), 800);
  }

  /** Sends whatever is pending. Safe to call when there is nothing. */
  private flush(): void {
    const question = this.question();
    const response = this.pendingResponse;
    const attachment = this.pendingAttachment;

    clearTimeout(this.saveTimer);

    if (!question || (response === null && attachment === null)) {
      return;
    }

    this.pendingResponse = null;
    this.pendingAttachment = null;
    this.saving.set(true);

    // Shared, so submitting can wait on the same request rather than issuing a
    // second one — and so a subscriber arriving after it has landed still gets
    // the answer instead of hanging.
    const request = this.take
      .saveAnswer({
        questionId: question.id,
        response: response ?? undefined,
        answerBlobName: attachment?.blobName,
        answerFileName: attachment?.fileName,
        timeSpentSeconds: Math.round((Date.now() - this.enteredAt) / 1000),
        wasPasted: this.wasPasted,
        keystrokeCount: this.keystrokes,
        backspaceCount: this.backspaces,
      })
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));

    this.inFlightSave = request;

    const settled = () => {
      if (this.inFlightSave === request) {
        this.inFlightSave = null;
      }
    };

    request.subscribe({
        next: result => {
          settled();
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
          settled();
          this.saving.set(false);

          // Kept, so the next save carries it. Losing an answer because one
          // request failed is the worst thing this screen could do.
          this.pendingResponse = response;
          this.pendingAttachment = attachment;
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
    // Whatever is in the box goes first, so the confirmation counts what they
    // have actually done rather than what the server has heard so far.
    this.flush();
    this.confirmingSubmit.set(true);
  }

  submit(automatic = false): void {
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.confirmingSubmit.set(false);

    // Time ran out, or the server has already said the attempt is over. It ends
    // either way, so waiting on a save would only delay the result screen.
    if (automatic) {
      this.send();
      return;
    }

    this.flush();

    const pending = this.inFlightSave;

    if (!pending) {
      this.send();
      return;
    }

    // The last answer has to reach the server before the submit does. It is the
    // whole reason this waits: the two used to race, and the submit sometimes
    // won.
    pending.subscribe({
      next: () => this.send(),
      error: err => {
        // Deliberately not submitted. Finalising is irreversible, and doing it
        // while an answer is known to be missing turns a failed request into a
        // lost mark. The response is still held, so pressing submit again
        // retries the save first.
        this.submitting.set(false);
        this.error.set(this.reason(err));
      },
    });
  }

  private send(): void {
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
    // A reload mid-sitting, reported once.
    //
    // Worth a marker seeing: the clock is the server's, so reloading buys no
    // time, and somebody doing it repeatedly is usually either fighting a bad
    // connection or looking for a way to start the paper again. Which of those
    // it was is exactly the judgement a person should make and software should
    // not — so it is recorded and nothing else happens.
    //
    // Read from the Navigation Timing API rather than tracked in storage: the
    // browser already knows how this page was entered, and a flag we set
    // ourselves would survive into a different attempt on a shared machine and
    // accuse the wrong person.
    const entry = performance.getEntriesByType('navigation')[0] as
      PerformanceNavigationTiming | undefined;

    if (entry?.type === 'reload') {
      this.take.reportSignal(IntegritySignalType.PageReloaded);
    }

    const onVisibility = () => {
      if (document.visibilityState === 'hidden') {
        this.take.reportSignal(IntegritySignalType.WindowBlur, this.question()?.id);
      }
    };

    // Pasting into the paper is refused, not merely noted.
    //
    // Still recorded: an attempt that was stopped is more worth a marker's eye
    // than one that succeeded, because it says what somebody tried to do. The
    // save-time threshold no longer applies — nothing arrives to measure — so
    // the attempt is reported from here.
    //
    // And it says so. A paste that silently does nothing reads as a broken text
    // box, and somebody under time pressure will try it three more times before
    // deciding the exam is broken. One line costs nothing and prevents that.
    //
    // This is a deterrent and not a control, and it is worth being clear about
    // that: a second device, a screenshot, or the browser's own tools all defeat
    // it. What it does stop is the easy path — an answer prepared elsewhere and
    // dropped in.
    const onPaste = (event: ClipboardEvent) => {
      event.preventDefault();

      this.wasPasted = true;
      this.take.reportSignal(IntegritySignalType.Paste, this.question()?.id);

      this.blocked.set('::Take:Blocked:Paste');
      this.clearBlockedSoon();
    };

    // Copying the question out is the other half. A paper that can be selected
    // and copied is a paper that can be sent to somebody else while the clock
    // runs, and the whole point of drawing a different paper per candidate is
    // that the questions do not travel.
    const onCopy = (event: ClipboardEvent) => {
      event.preventDefault();

      this.blocked.set('::Take:Blocked:Copy');
      this.clearBlockedSoon();
    };

    const onContextMenu = (event: MouseEvent) => event.preventDefault();

    const onKey = (event: KeyboardEvent) => {
      this.keystrokes++;

      if (event.key === 'Backspace') {
        this.backspaces++;
      }
    };

    document.addEventListener('visibilitychange', onVisibility);
    document.addEventListener('paste', onPaste);
    document.addEventListener('copy', onCopy);
    document.addEventListener('cut', onCopy);
    document.addEventListener('contextmenu', onContextMenu);
    document.addEventListener('keydown', onKey);

    this.destroyRef.onDestroy(() => {
      document.removeEventListener('visibilitychange', onVisibility);
      document.removeEventListener('paste', onPaste);
      document.removeEventListener('copy', onCopy);
      document.removeEventListener('cut', onCopy);
      document.removeEventListener('contextmenu', onContextMenu);
      document.removeEventListener('keydown', onKey);

      clearTimeout(this.blockedTimer);
    });
  }

  /**
   * Takes the notice away on its own.
   * <p>
   * It is an explanation, not an error: nothing is wrong and there is nothing to
   * dismiss. Leaving it on screen would make the candidate wonder whether it
   * still applies.
   * </p>
   */
  private clearBlockedSoon(): void {
    clearTimeout(this.blockedTimer);
    this.blockedTimer = setTimeout(() => this.blocked.set(null), 4000);
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
