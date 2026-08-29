import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { TranslateService } from '../../core/translate.service';

/**
 * Attaches an image, a sound or a video to a question.
 *
 * The reason this exists in the shape it does: the exam that prompted it was a
 * trading course written in Google Forms, and half its questions showed a chart.
 * When it was exported the charts did not survive — the questions came out
 * asking about a candle nobody could see. A question's media is not decoration
 * on that kind of exam, it is the question.
 *
 * <h4>No skill required</h4>
 * Choose a file, or drop one. No URL to paste, no path to type, no format to
 * know. Whoever writes the questions at a language centre is a teacher, and the
 * product has to be usable by them without help.
 */
@Component({
  selector: 'astro-media-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    @if (blobName()) {
      <div class="preview">
        @if (isImage()) {
          <img class="preview__image" [src]="url()" [alt]="t('::Media:Attached')" />
        } @else if (isAudio()) {
          <audio class="preview__player" [src]="url()" controls></audio>
        } @else if (isVideo()) {
          <video class="preview__player" [src]="url()" controls></video>
        }

        <div class="preview__bar">
          <span class="preview__name">{{ fileName() || t('::Media:Attached') }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" (click)="clear()">
            {{ t('::Media:Remove') }}
          </button>
        </div>
      </div>
    } @else {
      <!-- One control, and it accepts a drop as well as a click, because half the
           people who reach for this will already be dragging the file. -->
      <label
        class="drop"
        [class.drop--over]="dragging()"
        (dragover)="onDragOver($event)"
        (dragleave)="dragging.set(false)"
        (drop)="onDrop($event)">
        <input
          type="file"
          class="drop__input"
          [accept]="accept()"
          (change)="onPick($event)" />

        <i class="bi bi-paperclip" aria-hidden="true"></i>
        <span class="drop__label">{{ t('::Media:Choose') }}</span>
        <small class="drop__hint">{{ t('::Media:Hint') }}</small>
      </label>
    }

    @if (uploading()) {
      <p class="status" role="status">{{ t('::Media:Uploading') }}</p>
    }

    @if (error(); as message) {
      <p class="status status--error" role="alert">{{ message }}</p>
    }
  `,
  styles: `
    :host { display: block; }

    .drop {
      display: grid;
      justify-items: center;
      gap: var(--astro-space-1);
      padding-block: var(--astro-space-5);
      border: 1px dashed var(--astro-line);
      border-radius: var(--astro-radius-md);
      background: var(--astro-surface-2);
      color: var(--astro-ink-3);
      cursor: pointer;
      text-align: center;

      &:hover, &--over {
        border-color: var(--astro-brand-600);
        color: var(--astro-ink-2);
      }

      i { font-size: 1.5rem; }
    }

    .drop__input { position: absolute; inline-size: 1px; block-size: 1px; opacity: 0; }
    .drop__label { font-weight: 600; color: var(--astro-ink-1); }
    .drop__hint { font-size: .8125rem; }

    .preview {
      border: 1px solid var(--astro-line);
      border-radius: var(--astro-radius-md);
      overflow: hidden;
    }

    .preview__image {
      display: block;
      inline-size: 100%;
      max-block-size: 22rem;
      object-fit: contain;
      background: var(--astro-surface-2);
    }

    .preview__player { inline-size: 100%; display: block; }

    .preview__bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--astro-space-2);
      padding: var(--astro-space-2);
      border-block-start: 1px solid var(--astro-line);
    }

    .preview__name {
      font-size: .875rem;
      color: var(--astro-ink-2);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .status { margin-block: var(--astro-space-2) 0; font-size: .875rem; color: var(--astro-ink-3); }
    .status--error { color: var(--astro-fail-fg); }
  `,
})
export class MediaFieldComponent {
  private readonly rest = inject(RestService);

  readonly t = inject(TranslateService).t;

  readonly blobName = input<string | undefined>();
  readonly mediaType = input<string | undefined>();

  readonly changed = output<{ blobName?: string; mediaType?: string }>();

  readonly uploading = signal(false);
  readonly dragging = signal(false);
  readonly error = signal<string | null>(null);
  readonly fileName = signal<string | null>(null);

  isImage(): boolean {
    return this.mediaType() === 'image';
  }

  isAudio(): boolean {
    return this.mediaType() === 'audio';
  }

  isVideo(): boolean {
    return this.mediaType() === 'video';
  }

  url(): string {
    return `/api/assessment/media/${this.blobName()}`;
  }

  accept(): string {
    return 'image/*,audio/*,video/*,.pdf';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);

    const file = event.dataTransfer?.files?.[0];

    if (file) {
      this.upload(file);
    }
  }

  onPick(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];

    if (file) {
      this.upload(file);
    }
  }

  clear(): void {
    this.fileName.set(null);
    this.changed.emit({ blobName: undefined, mediaType: undefined });
  }

  private upload(file: File): void {
    this.uploading.set(true);
    this.error.set(null);

    const body = new FormData();
    body.append('file', file);

    this.rest
      .request<FormData, { blobName: string; mediaType: string; originalFileName: string }>(
        { method: 'POST', url: '/api/assessment/media', body },
        { apiName: 'Default' },
      )
      .subscribe({
        next: result => {
          this.uploading.set(false);
          this.fileName.set(result.originalFileName);
          this.changed.emit({ blobName: result.blobName, mediaType: result.mediaType });
        },
        error: err => {
          this.uploading.set(false);

          // The reason, because "upload failed" leaves an author guessing between
          // a file too large, a format we do not hold, and a network that dropped.
          this.error.set(err?.error?.error?.message ?? err?.message ?? this.t('::UnknownError'));
        },
      });
  }
}
