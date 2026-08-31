import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Our own mark: an astrolabe's graduated dial, drawn.
 *
 * Shown wherever an organisation has not uploaded a logo of its own. A page
 * with a mark on it looks like a product; a page with a hole where one should
 * be looks broken, and the screen a candidate opens their exam link on is the
 * worst possible place to look broken.
 *
 * Drawn rather than lettered so it works at 24px, and inline so it inherits
 * `currentColor` — which is what keeps it a quiet device rather than a second
 * brand competing with the organisation's name beside it. It was already drawn
 * once, in the shell; the candidate's screens had nothing, so it lives here now
 * and both read from one definition.
 */
@Component({
  selector: 'astro-mark',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.viewBox]="'0 0 32 32'"
      [attr.width]="size()"
      [attr.height]="size()"
      aria-hidden="true"
      focusable="false">
      <circle cx="16" cy="16" r="13" fill="none" stroke="currentColor" stroke-width="1.6" opacity=".35" />
      <circle cx="16" cy="16" r="9" fill="none" stroke="currentColor" stroke-width="1.6" />

      <!-- graduation marks, the instrument's scale -->
      <g stroke="currentColor" stroke-width="1.5" stroke-linecap="round" opacity=".8">
        <line x1="16" y1="3" x2="16" y2="6" />
        <line x1="29" y1="16" x2="26" y2="16" />
        <line x1="16" y1="29" x2="16" y2="26" />
        <line x1="3" y1="16" x2="6" y2="16" />
      </g>

      <!-- the alidade: the rule you sight along to take a reading -->
      <line x1="7.5" y1="24.5" x2="24.5" y2="7.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
      <circle cx="16" cy="16" r="2" fill="currentColor" />
    </svg>
  `,
  styles: `
    :host { display: inline-flex; }
    svg { display: block; }
  `,
})
export class AstroMarkComponent {
  /** Pixels. The dial is drawn on a 32-unit grid and scales cleanly from 20 up. */
  readonly size = input(28);
}
