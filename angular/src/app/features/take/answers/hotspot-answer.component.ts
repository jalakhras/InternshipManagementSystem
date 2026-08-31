import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { MediaService } from '../../../core/media.service';
import { TranslateService } from '../../../core/translate.service';
import { TakerQuestion } from '../take.models';

/** Where on the image the candidate pointed, in percentages so it scales. */
interface Point {
  x: number;
  y: number;
}

/**
 * Point at the place on the image that answers the question.
 *
 * The author has a region editor and draws the accepted areas; the candidate is
 * sent the image and nothing else — the server's projection has no field for the
 * regions, so the answer cannot be read off the screen. Until now the candidate
 * was sent a plain text box instead, which is not an answer to "click the
 * support level" by any reading.
 *
 * The response is `{ x, y }` as percentages of the image, which is what the
 * grader already expects and what makes the answer independent of the size the
 * image happened to render at — a phone and a desktop produce the same answer
 * for the same place.
 *
 * **Keyboard, not only pointer.** WCAG 2.2 requires a single-pointer interaction
 * to have a non-pointer path, and this is the whole question rather than a
 * convenience: a candidate who cannot use a mouse could not answer at all. The
 * image is focusable, the arrow keys move the mark by one percent and by five
 * with Shift, and Enter places it in the middle to start from. That is also the
 * only way to answer precisely, which is worth having for everybody.
 */
@Component({
  selector: 'astro-hotspot-answer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (imageUrl(); as url) {
      <div class="hotspot">
        <!--
          The whole image is the target, so there is no small hit area to miss —
          the touch-target rules are about controls, and here the control is the
          picture. tabindex makes it reachable; role and aria-label say what it
          is, because an <img> alone announces as a graphic and not as something
          you answer with.
        -->
        <div
          class="hotspot__frame"
          tabindex="0"
          role="application"
          [attr.aria-label]="t('::Take:Hotspot:Instruction')"
          (click)="place($event)"
          (keydown)="onKey($event)">
          <img class="hotspot__image" [src]="url" alt="" draggable="false" />

          @if (point(); as p) {
            <!--
              A ring with a crosshair through it, not a coloured dot: an answer
              must not be conveyed by colour alone, and this has to read on a
              photograph of any colour. aria-hidden because the position is
              announced as text below, where a screen reader can actually use it.
            -->
            <!--
              Physical left and top, not the logical pair, and this is the one
              place in the product where that is right.

              The percentages are measured from the left and top of the image
              itself, and an image does not mirror when the page direction does —
              a diagram of a heart has the aorta where it has it, in Arabic and
              in English alike. The logical property measures from the right in
              an RTL page, so the marker landed mirrored across the picture: a
              candidate pointing at the correct place watched the ring appear
              somewhere else, while their answer was recorded where they had
              actually pointed.
            -->
            <svg
              class="hotspot__mark"
              [style.left.%]="p.x"
              [style.top.%]="p.y"
              viewBox="0 0 32 32"
              aria-hidden="true"
              focusable="false">
              <circle cx="16" cy="16" r="11" class="hotspot__ring" />
              <line x1="16" y1="2" x2="16" y2="10" class="hotspot__cross" />
              <line x1="16" y1="22" x2="16" y2="30" class="hotspot__cross" />
              <line x1="2" y1="16" x2="10" y2="16" class="hotspot__cross" />
              <line x1="22" y1="16" x2="30" y2="16" class="hotspot__cross" />
            </svg>
          }
        </div>

        <!--
          Said in words as well as shown. It is the non-visual answer to "where
          did I put it", the confirmation that the tap registered, and the only
          feedback somebody adjusting with the arrow keys gets.
        -->
        <p class="hotspot__where" role="status">
          @if (point(); as p) {
            {{ t('::Take:Hotspot:Placed', p.x.toString(), p.y.toString()) }}
          } @else {
            {{ t('::Take:Hotspot:Instruction') }}
          }
        </p>
      </div>
    } @else {
      <!-- The payload had no image. Saying so beats an empty frame the candidate
           waits for. -->
      <p class="hotspot__missing">{{ t('::Take:Hotspot:NoImage') }}</p>
    }
  `,
  styles: `
    :host { display: block; }

    .hotspot__frame {
      position: relative;
      display: inline-block;
      max-inline-size: 100%;
      border-radius: var(--astro-radius-md);
      cursor: crosshair;

      /* The picture is the question. Selecting and dragging it are ways to take
         it out of the room, and neither helps anybody answer. */
      user-select: none;

      &:focus-visible {
        outline: 2px solid var(--accent);
        outline-offset: 2px;
      }
    }

    .hotspot__image {
      display: block;
      max-inline-size: 100%;
      block-size: auto;
      border-radius: var(--astro-radius-md);
      pointer-events: none;
    }

    .hotspot__mark {
      position: absolute;
      inline-size: 2.25rem;
      block-size: 2.25rem;

      /* Centred on the point rather than starting at it, so the crosshair sits
         where the candidate pointed. */
      transform: translate(-50%, -50%);
      pointer-events: none;
    }

    /* Two strokes, dark under light: the mark has to be visible on a photograph
       whose colours nobody chose. */
    .hotspot__ring {
      fill: none;
      stroke: #fff;
      stroke-width: 4;
      paint-order: stroke;
    }

    .hotspot__cross {
      stroke: #fff;
      stroke-width: 4;
      stroke-linecap: round;
    }

    .hotspot__mark {
      filter: drop-shadow(0 0 1.5px rgba(0, 0, 0, .9));
    }

    .hotspot__where {
      margin: var(--astro-space-2) 0 0;
      color: var(--text-secondary);
      font-size: .9375rem;
    }

    .hotspot__missing {
      margin: 0;
      color: var(--status-fail-text);
    }
  `,
})
export class HotspotAnswerComponent {
  readonly t = inject(TranslateService).t;

  readonly question = input.required<TakerQuestion>();
  readonly response = input<string | undefined>();
  readonly responseChange = output<string>();

  private readonly media = inject(MediaService);

  /**
   * The picture, at an address the browser can actually reach.
   * <p>
   * The server sends a path relative to the API, and this was the one
   * candidate-facing binding that used it raw — so it resolved against the app's
   * own origin instead, and a candidate opened a hotspot question to an empty
   * frame with nothing to click. Every sibling on this screen already goes
   * through the same helper.
   * </p>
   * <p>
   * It survived because the browser test feeds it a `data:` URI, which needs no
   * resolving: the test was asserting its own stub.
   * </p>
   */
  readonly imageUrl = computed(() =>
    this.media.absolute(this.question().display?.['imageUrl'] as string | undefined) ?? undefined);

  private readonly chosen = signal<Point | null>(null);

  /**
   * What is on the image: the point just placed, or the one a resumed attempt
   * comes back to.
   */
  readonly point = computed(() => this.chosen() ?? this.saved());

  private readonly saved = computed<Point | null>(() => {
    const stored = this.response();

    if (!stored) {
      return null;
    }

    try {
      const parsed = JSON.parse(stored) as Partial<Point>;

      return typeof parsed.x === 'number' && typeof parsed.y === 'number'
        ? { x: round(parsed.x), y: round(parsed.y) }
        : null;
    } catch {
      // A response this build cannot read is not worth an error on an exam
      // screen. They point again.
      return null;
    }
  });

  place(event: MouseEvent): void {
    const frame = event.currentTarget as HTMLElement;
    const box = frame.getBoundingClientRect();

    if (box.width === 0 || box.height === 0) {
      return;
    }

    this.emit({
      // Measured from the left edge of the picture, and it has to be, because
      // the picture is a picture: a chart, a diagram, a photograph. It does not
      // mirror when the page does, so neither can a point on it.
      x: round(((event.clientX - box.left) / box.width) * 100),
      y: round(((event.clientY - box.top) / box.height) * 100),
    });
  }

  onKey(event: KeyboardEvent): void {
    // Five percent with Shift, one without: coarse to cross the picture, fine to
    // settle on a feature.
    const step = event.shiftKey ? 5 : 1;
    const from = this.point() ?? { x: 50, y: 50 };

    const moved: Record<string, Point> = {
      ArrowLeft: { x: from.x - step, y: from.y },
      ArrowRight: { x: from.x + step, y: from.y },
      ArrowUp: { x: from.x, y: from.y - step },
      ArrowDown: { x: from.x, y: from.y + step },
    };

    // Enter and Space start in the middle when nothing has been placed, so there
    // is something to move. With a mark already down they do nothing: this
    // question has no submit of its own.
    if ((event.key === 'Enter' || event.key === ' ') && !this.point()) {
      event.preventDefault();
      this.emit({ x: 50, y: 50 });
      return;
    }

    const next = moved[event.key];

    if (!next) {
      return;
    }

    // Only once it is handled — the arrow keys still scroll the page everywhere
    // else on this screen.
    event.preventDefault();

    this.emit({ x: clamp(next.x), y: clamp(next.y) });
  }

  private emit(point: Point): void {
    this.chosen.set(point);
    this.responseChange.emit(JSON.stringify(point));
  }
}

const clamp = (value: number): number => Math.min(100, Math.max(0, value));

/** One decimal is finer than anybody can point and keeps the answer readable. */
const round = (value: number): number => Math.round(clamp(value) * 10) / 10;
