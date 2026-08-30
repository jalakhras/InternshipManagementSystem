import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { TranslateService } from '../../../core/translate.service';
import { TakeService } from '../take.service';
import { TakerQuestion } from '../take.models';
import { AnswerAttachment } from './answer-input';

/**
 * The answer is a file the candidate hands in.
 *
 * A scanned worksheet, a spreadsheet, a design. The type existed, the marker's
 * screen already showed an attached file, and the candidate was given a
 * textarea — so the one thing the question asked for was the one thing they
 * could not do.
 *
 * Uploaded when chosen rather than held until submit. Somebody who picks a file
 * and then loses their connection at the end of an exam should not discover that
 * their answer never left the machine, and the upload is the slow part: doing it
 * early puts the waiting somewhere it costs nothing.
 */
@Component({
  selector: 'astro-upload-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="upload">
      <label class="upload__button">
        <input
          type="file"
          class="upload__field"
          [accept]="accept"
          [disabled]="busy()"
          (change)="choose($event)" />
        <span>{{ busy() ? t('::Take:Upload:Working') : t('::Take:Upload:Choose') }}</span>
      </label>

      <!--
        What was accepted, said plainly. An upload with no confirmation is
        indistinguishable from one that failed, and this is a candidate who
        cannot try again afterwards.
      -->
      <p class="upload__state" role="status">
        @if (stored(); as name) {
          {{ t('::Take:Upload:Stored', name) }}
        } @else if (busy()) {
          {{ t('::Take:Upload:Working') }}
        } @else {
          {{ t('::Take:Upload:Hint') }}
        }
      </p>

      @if (error(); as message) {
        <p class="upload__error" role="alert">{{ message }}</p>
      }
    </div>
  `,
  styles: `
    :host { display: block; }

    .upload__button {
      display: inline-flex;
      align-items: center;
      justify-content: center;

      /* Comfortably past the minimum target size: this is the only control on
         the question and it is used once, under time pressure. */
      min-block-size: 3rem;
      padding-inline: var(--astro-space-5);
      border: 1px solid var(--accent);
      border-radius: var(--astro-radius-md);
      background: var(--surface-raised);
      color: var(--accent);
      font-weight: var(--astro-weight-medium);
      cursor: pointer;

      &:focus-within {
        outline: 2px solid var(--accent);
        outline-offset: 2px;
      }
    }

    /* Visually hidden rather than display:none — a hidden input is not
       focusable, and the keyboard would never reach the control. */
    .upload__field {
      position: absolute;
      inline-size: 1px;
      block-size: 1px;
      opacity: 0;
    }

    .upload__state {
      margin: var(--astro-space-2) 0 0;
      color: var(--text-secondary);
      font-size: .9375rem;
    }

    .upload__error {
      margin: var(--astro-space-2) 0 0;
      color: var(--status-fail-text);
      font-size: .9375rem;
    }
  `,
})
export class UploadAnswerComponent {
  private readonly take = inject(TakeService);

  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();
  readonly attachment = output<AnswerAttachment>();

  readonly busy = signal(false);
  readonly stored = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  /** What the server will accept, so the picker does not offer what it refuses. */
  readonly accept = '.pdf,.doc,.docx,.txt,.png,.jpg,.jpeg';

  choose(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.take.uploadAnswerFile(file).subscribe({
      next: result => {
        this.busy.set(false);
        this.stored.set(result.originalFileName);
        this.attachment.emit({
          blobName: result.blobName,
          fileName: result.originalFileName,
        });
      },
      error: err => {
        this.busy.set(false);

        // The server's own sentence — it names the size limit and the kinds it
        // takes. A generic failure here would leave somebody re-picking the same
        // refused file until the clock ran out.
        this.error.set(this.reason(err));

        // So the same file can be chosen again after fixing it; a file input
        // fires nothing when the same name is re-selected.
        input.value = '';
      },
    });
  }

  private reason(err: unknown): string {
    const problem = err as { error?: { error?: { message?: string } }; message?: string };

    return problem?.error?.error?.message ?? problem?.message ?? this.t('::UnknownError');
  }
}
