import {
  AfterViewInit,
  Directive,
  ElementRef,
  OnDestroy,
  inject,
  output,
} from '@angular/core';

/**
 * The keyboard contract every modal owes, in one place.
 *
 * A dialog that opens without moving focus into it leaves the keyboard behind
 * the scrim: Tab walks the page underneath, Escape does nothing, and closing the
 * dialog drops focus on <body> so the next Tab starts again from the top of the
 * document. None of that is visible with a mouse, which is why six dialogs
 * shipped without it.
 *
 * Four behaviours, and they are not separable — a trap without an escape is a
 * cage, and a trap without a restore loses the reader's place on the way out:
 *
 *   · focus moves to the first control inside on open (the dialog itself if it
 *     has none, which is why the host carries tabindex="-1");
 *   · Tab and Shift+Tab wrap inside rather than leaving;
 *   · Escape asks the host to close, so it closes the same way Cancel does and
 *     the host stays the only thing that knows how;
 *   · focus returns to whatever opened it.
 *
 * Put it on the dialog box, never on the scrim: the scrim is a backdrop, and a
 * trap around it would include the page it is covering.
 */
@Directive({
  selector: '[astroModal]',
  standalone: true,
  host: {
    tabindex: '-1',
    '(keydown)': 'onKeydown($event)',
  },
})
export class ModalDirective implements AfterViewInit, OnDestroy {
  /** Escape was pressed. The host closes itself; this directive never guesses how. */
  readonly dismiss = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /**
   * Whatever had focus when the dialog opened — captured before focus moves, and
   * returned to on the way out. Usually the button that opened it.
   */
  private returnTo: HTMLElement | null = null;

  ngAfterViewInit(): void {
    const active = document.activeElement;
    this.returnTo = active instanceof HTMLElement ? active : null;

    const first = this.focusable()[0];
    (first ?? this.host.nativeElement).focus();
  }

  ngOnDestroy(): void {
    // Only if it is still in the document: the row that opened the dialog can
    // have been the row the dialog deleted.
    if (this.returnTo?.isConnected) {
      this.returnTo.focus();
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.dismiss.emit();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const items = this.focusable();

    if (items.length === 0) {
      event.preventDefault();
      return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;

    if (event.shiftKey && (active === first || active === this.host.nativeElement)) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  /**
   * Read on every keystroke rather than cached. A dialog's contents change while
   * it is open — a list finishes loading, a confirm button appears — and a stale
   * list would trap Tab on controls that are no longer there.
   */
  private focusable(): HTMLElement[] {
    const selector =
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]),' +
      ' textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

    return Array.from(this.host.nativeElement.querySelectorAll<HTMLElement>(selector))
      // offsetParent is null for anything display:none or inside it, which is how
      // a hidden branch of the template stays out of the cycle.
      .filter(el => el.offsetParent !== null || el === document.activeElement);
  }
}
