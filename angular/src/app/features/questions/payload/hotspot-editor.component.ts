import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { TranslateService } from '../../../core/translate.service';
import { MediaFieldComponent } from '../../../shared/ui/media-field.component';
import { HotspotPayload, HotspotRegion, newId, readPayload, writePayload } from './payload.models';

/**
 * Click the picture: an image, and the areas on it that count as right.
 *
 * The area is drawn by dragging across the image. That is the only sane design
 * — the alternative is four percentage fields per region, which asks an author
 * to work out where 34.2% of a chart is, and gets it wrong.
 *
 * The regions are stored as fractions of the image rather than pixels, so the
 * same question is answerable on a phone and on a desk monitor without the
 * target moving.
 *
 * This is the type the owner's trading exam needed and Google Forms could not
 * give it: "mark the support level on this chart" is not a multiple-choice
 * question, and turning it into one changes what is being measured.
 */
@Component({
  selector: 'astro-hotspot-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MediaFieldComponent],
  template: `
    @if (!imageBlobName()) {
      <p class="lede">{{ t('::Question:Hotspot:ChooseImage') }}</p>
      <astro-media-field mediaType="image" (changed)="setImage($event)" />
    } @else {
      <p class="lede">{{ t('::Question:Hotspot:Lede') }}</p>

      <div
        #canvas
        class="canvas"
        (pointerdown)="startDraw($event)"
        (pointermove)="draw($event)"
        (pointerup)="endDraw()"
        (pointerleave)="endDraw()">
        <img class="canvas__image" [src]="imageUrl()" [alt]="t('::Question:Hotspot:Image')" draggable="false" />

        @for (region of regions(); track region.id; let i = $index) {
          <span
            class="region"
            [style.inset-inline-start.%]="region.x * 100"
            [style.inset-block-start.%]="region.y * 100"
            [style.inline-size.%]="region.width * 100"
            [style.block-size.%]="region.height * 100">
            <button
              type="button"
              class="region__remove"
              [attr.aria-label]="t('::Question:Hotspot:RemoveRegion')"
              (pointerdown)="$event.stopPropagation()"
              (click)="removeRegion(region.id)">
              <i class="bi bi-x-lg" aria-hidden="true"></i>
            </button>
          </span>
        }

        @if (pending(); as box) {
          <span
            class="region region--pending"
            [style.inset-inline-start.%]="box.x * 100"
            [style.inset-block-start.%]="box.y * 100"
            [style.inline-size.%]="box.width * 100"
            [style.block-size.%]="box.height * 100"></span>
        }
      </div>

      <div class="actions">
        <span class="count">{{ t('::Question:Hotspot:Count', regions().length.toString()) }}</span>
        <button type="button" class="btn btn-sm btn-outline-secondary" (click)="changeImage()">
          {{ t('::Question:Hotspot:ChangeImage') }}
        </button>
      </div>
    }

    @for (warning of warnings(); track warning) {
      <p class="warning" role="status">
        <i class="bi bi-exclamation-triangle" aria-hidden="true"></i>
        {{ t('::' + warning) }}
      </p>
    }
  `,
  styles: `
    :host { display: block; }

    .lede { margin-block: 0 var(--astro-space-3); color: var(--astro-ink-3); font-size: .875rem; }

    .canvas {
      position: relative;
      display: inline-block;
      max-inline-size: 100%;
      border: 1px solid var(--astro-line);
      border-radius: var(--astro-radius-md);
      overflow: hidden;
      cursor: crosshair;
      touch-action: none;
      user-select: none;
    }

    .canvas__image { display: block; max-inline-size: 100%; block-size: auto; }

    .region {
      position: absolute;
      border: 2px solid var(--astro-pass-fg);
      background: color-mix(in srgb, var(--astro-pass-fg) 18%, transparent);
      border-radius: 2px;
    }

    .region--pending {
      border-style: dashed;
      border-color: var(--astro-brand-600);
      background: color-mix(in srgb, var(--astro-brand-600) 14%, transparent);
    }

    .region__remove {
      position: absolute;
      inset-block-start: -.5rem;
      inset-inline-end: -.5rem;
      display: grid;
      place-items: center;
      inline-size: 1.25rem;
      block-size: 1.25rem;
      padding: 0;
      border: 0;
      border-radius: 50%;
      background: var(--astro-fail-fg);
      color: #fff;
      font-size: .625rem;
      cursor: pointer;
    }

    .actions {
      display: flex;
      align-items: center;
      gap: var(--astro-space-3);
      margin-block-start: var(--astro-space-3);
    }

    .count { font-size: .875rem; color: var(--astro-ink-3); }

    .warning {
      display: flex;
      gap: var(--astro-space-2);
      margin-block-start: var(--astro-space-3);
      color: var(--astro-warn-fg);
      font-size: .875rem;
    }
  `,
})
export class HotspotEditorComponent {
  readonly t = inject(TranslateService).t;

  readonly payload = input<string>('');
  readonly payloadChange = output<string>();

  readonly imageBlobName = signal('');
  readonly regions = signal<HotspotRegion[]>([]);
  readonly pending = signal<{ x: number; y: number; width: number; height: number } | null>(null);

  private readonly canvas = viewChild<ElementRef<HTMLElement>>('canvas');

  private origin: { x: number; y: number } | null = null;

  readonly warnings = computed<string[]>(() => {
    const found: string[] = [];

    if (!this.imageBlobName()) {
      found.push('IMS:Question:HotspotNeedsImage');
    } else if (this.regions().length === 0) {
      found.push('IMS:Question:NoCorrectRegion');
    }

    return found;
  });

  constructor() {
    effect(() => {
      const parsed = readPayload<HotspotPayload>(this.payload(), { imageBlobName: '', regions: [] });

      this.imageBlobName.set(parsed.imageBlobName ?? '');
      this.regions.set(parsed.regions ?? []);
    });
  }

  imageUrl(): string {
    return `/api/assessment/media/${this.imageBlobName()}`;
  }

  setImage(media: { blobName?: string }): void {
    this.imageBlobName.set(media.blobName ?? '');

    // Regions are fractions of a particular picture. Keeping them across a change
    // of image would leave targets floating over whatever the new one shows.
    this.regions.set([]);
    this.emit();
  }

  changeImage(): void {
    this.setImage({ blobName: undefined });
  }

  startDraw(event: PointerEvent): void {
    const point = this.pointFrom(event);

    if (!point) {
      return;
    }

    this.origin = point;
    this.pending.set({ ...point, width: 0, height: 0 });
  }

  draw(event: PointerEvent): void {
    if (!this.origin) {
      return;
    }

    const point = this.pointFrom(event);

    if (!point) {
      return;
    }

    // Drawn from either corner: an author dragging up and to the left means the
    // same rectangle as one dragging down and to the right.
    this.pending.set({
      x: Math.min(this.origin.x, point.x),
      y: Math.min(this.origin.y, point.y),
      width: Math.abs(point.x - this.origin.x),
      height: Math.abs(point.y - this.origin.y),
    });
  }

  endDraw(): void {
    const box = this.pending();

    this.origin = null;
    this.pending.set(null);

    // A click rather than a drag. Ignored, because a region a candidate cannot
    // hit is a question nobody can pass.
    if (!box || box.width < 0.02 || box.height < 0.02) {
      return;
    }

    this.regions.update(list => [
      ...list,
      { id: newId('r'), ...this.round(box), isCorrect: true },
    ]);

    this.emit();
  }

  removeRegion(id: string): void {
    this.regions.update(list => list.filter(r => r.id !== id));
    this.emit();
  }

  /** Where the pointer is, as a fraction of the image rather than in pixels. */
  private pointFrom(event: PointerEvent): { x: number; y: number } | null {
    const element = this.canvas()?.nativeElement;

    if (!element) {
      return null;
    }

    const box = element.getBoundingClientRect();

    return {
      x: Math.min(Math.max((event.clientX - box.left) / box.width, 0), 1),
      y: Math.min(Math.max((event.clientY - box.top) / box.height, 0), 1),
    };
  }

  private round(box: { x: number; y: number; width: number; height: number }) {
    const to3 = (value: number) => Math.round(value * 1000) / 1000;

    return { x: to3(box.x), y: to3(box.y), width: to3(box.width), height: to3(box.height) };
  }

  private emit(): void {
    this.payloadChange.emit(
      writePayload({ imageBlobName: this.imageBlobName(), regions: this.regions() }),
    );
  }
}
