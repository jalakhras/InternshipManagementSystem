import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { TranslateService } from '../../../core/translate.service';
import { TakeService } from '../take.service';
import { TakerQuestion } from '../take.models';
import { AnswerAttachment } from './answer-input';

/**
 * The answer is spoken.
 *
 * This is the whole of a speaking assessment, and the type shipped with no way
 * to record — a candidate asked to describe a chart aloud was given a textarea,
 * which is a different exam. The marker's screen already played an attached
 * recording; nothing ever produced one.
 *
 * Recorded in the browser and uploaded when they stop, so the waiting happens
 * once, on their terms, rather than at submit when there is no time left to
 * recover from a failure.
 *
 * Kept to one recording that can be replaced. Somebody who stumbles has to be
 * able to go again — a single irreversible take is a cruelty in a timed exam —
 * but keeping several would ask the candidate to choose, and choosing is not
 * what is being assessed.
 */
@Component({
  selector: 'astro-audio-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="audio">
      @if (!supported) {
        <!-- An old browser, or an insecure origin: getUserMedia is unavailable
             outside HTTPS. Saying which beats a dead button. -->
        <p class="audio__error" role="alert">{{ t('::Take:Audio:Unsupported') }}</p>
      } @else {
        <button
          type="button"
          class="audio__button"
          [class.audio__button--recording]="recording()"
          [disabled]="busy()"
          (click)="toggle()">
          <!-- A square and a circle, not colour alone: which state this is has to
               read without relying on red. -->
          <svg class="audio__icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
            @if (recording()) {
              <rect x="7" y="7" width="10" height="10" rx="1.5" fill="currentColor" />
            } @else {
              <circle cx="12" cy="12" r="6" fill="currentColor" />
            }
          </svg>

          <span>
            {{ recording() ? t('::Take:Audio:Stop') : stored() ? t('::Take:Audio:Again') : t('::Take:Audio:Start') }}
          </span>
        </button>

        <p class="audio__state" role="status">
          @if (recording()) {
            {{ t('::Take:Audio:Recording', elapsed().toString()) }}
          } @else if (busy()) {
            {{ t('::Take:Audio:Working') }}
          } @else if (stored()) {
            {{ t('::Take:Audio:Stored') }}
          } @else {
            {{ t('::Take:Audio:Hint') }}
          }
        </p>

        <!-- Hearing it back is the point. A candidate who cannot tell whether
             the microphone worked will record it again and again. -->
        @if (playback(); as src) {
          <audio class="audio__player" [src]="src" controls></audio>
        }
      }

      @if (error(); as message) {
        <p class="audio__error" role="alert">{{ message }}</p>
      }
    </div>
  `,
  styles: `
    :host { display: block; }

    .audio__button {
      display: inline-flex;
      align-items: center;
      gap: var(--astro-space-2);
      min-block-size: 3rem;
      padding-inline: var(--astro-space-5);
      border: 1px solid var(--accent);
      border-radius: var(--astro-radius-full);
      background: var(--surface-raised);
      color: var(--accent);
      font-weight: var(--astro-weight-medium);
      cursor: pointer;

      &--recording {
        border-color: var(--status-fail-text);
        color: var(--status-fail-text);
      }

      &:disabled { opacity: .6; cursor: default; }
    }

    .audio__icon { inline-size: 1.25rem; block-size: 1.25rem; }

    .audio__state {
      margin: var(--astro-space-2) 0 0;
      color: var(--text-secondary);
      font-size: .9375rem;
    }

    .audio__player {
      display: block;
      inline-size: min(24rem, 100%);
      margin-block-start: var(--astro-space-3);
    }

    .audio__error {
      margin: var(--astro-space-2) 0 0;
      color: var(--status-fail-text);
      font-size: .9375rem;
    }
  `,
})
export class AudioAnswerComponent {
  private readonly take = inject(TakeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();
  readonly attachment = output<AnswerAttachment>();

  readonly recording = signal(false);
  readonly busy = signal(false);
  readonly stored = signal(false);
  readonly elapsed = signal(0);
  readonly playback = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly supported =
    typeof navigator !== 'undefined' &&
    !!navigator.mediaDevices?.getUserMedia &&
    typeof MediaRecorder !== 'undefined';

  private recorder?: MediaRecorder;
  private chunks: Blob[] = [];
  private ticker?: ReturnType<typeof setInterval>;

  constructor() {
    // A recording still running when the question changes or the exam ends must
    // release the microphone. A browser that keeps showing the recording
    // indicator after an exam is over is its own kind of alarming.
    this.destroyRef.onDestroy(() => this.release());
  }

  toggle(): void {
    if (this.recording()) {
      this.recorder?.stop();
      return;
    }

    this.start();
  }

  private start(): void {
    this.error.set(null);

    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then(stream => {
        const recorder = new MediaRecorder(stream);

        this.recorder = recorder;
        this.chunks = [];

        recorder.ondataavailable = event => {
          if (event.data.size > 0) {
            this.chunks.push(event.data);
          }
        };

        recorder.onstop = () => {
          this.release();
          this.send(new Blob(this.chunks, { type: recorder.mimeType || 'audio/webm' }));
        };

        recorder.start();

        this.recording.set(true);
        this.elapsed.set(0);
        this.ticker = setInterval(() => this.elapsed.update(s => s + 1), 1000);
      })
      .catch(() => {
        // Refused, or no microphone. Not an error to apologise for — say what to
        // do about it, since the candidate can fix one of those.
        this.error.set(this.t('::Take:Audio:NoPermission'));
      });
  }

  private send(blob: Blob): void {
    // Heard back locally straight away, before the upload finishes: the question
    // "did that work" should not wait on the network.
    this.playback.set(URL.createObjectURL(blob));

    const extension = blob.type.includes('mp4') ? 'mp4' : 'webm';
    const file = new File([blob], `answer.${extension}`, { type: blob.type });

    this.busy.set(true);

    this.take.uploadAnswerFile(file).subscribe({
      next: result => {
        this.busy.set(false);
        this.stored.set(true);
        this.attachment.emit({
          blobName: result.blobName,
          fileName: result.originalFileName,
        });
      },
      error: err => {
        this.busy.set(false);
        this.error.set(this.reason(err));
      },
    });
  }

  private release(): void {
    clearInterval(this.ticker);

    this.recording.set(false);
    this.recorder?.stream.getTracks().forEach(track => track.stop());
    this.recorder = undefined;
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
