import { Injectable } from '@angular/core';

/**
 * Paints the accent in the organisation's own colour.
 *
 * The colour was on the settings screen, saved, read back, and applied nowhere —
 * so an academy could choose its colour, watch it save, and see the platform's
 * blue everywhere it looked. The name and the mark were already theirs; this is
 * the third thing that makes a screen feel like it belongs to them rather than
 * to whoever sold them the software.
 *
 * It matters most on the candidate's screens. Staff at least chose this
 * platform. Somebody opening a placement-test link has no relationship with us
 * and no reason to trust a page in a colour they have never seen.
 */
@Injectable({ providedIn: 'root' })
export class BrandService {
  /**
   * Three hex digits or six, and nothing else.
   *
   * This is tenant-supplied text on its way into a style property. A tenant
   * administrator is trusted with their own organisation, not with everybody
   * who opens one of its links — and a value like
   * `red; background-image: url(https://tracker.example/pixel.png)` is a
   * tracking pixel served in our name. The invitation email validates the same
   * value for the same reason.
   */
  private static readonly Hex = /^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/;

  apply(colour: string | null | undefined): void {
    const root = document.documentElement;

    if (!colour || !BrandService.Hex.test(colour.trim())) {
      // Back to the platform's own palette. Clearing rather than leaving the
      // last tenant's colour behind matters on a shared browser: signing out of
      // one organisation and into another must not keep the first one's paint.
      root.style.removeProperty('--accent');
      root.style.removeProperty('--accent-hover');
      root.style.removeProperty('--accent-active');
      return;
    }

    const accent = colour.trim();

    root.style.setProperty('--accent', accent);

    // Derived rather than asked for. Nobody should have to choose three colours
    // to have a brand, and a hover state that does not track the accent reads as
    // a bug. color-mix does the arithmetic in the browser, so this holds for any
    // colour a tenant picks.
    root.style.setProperty('--accent-hover', `color-mix(in srgb, ${accent} 85%, black)`);
    root.style.setProperty('--accent-active', `color-mix(in srgb, ${accent} 70%, black)`);
  }
}
