import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';

import { BrandService } from '../../core/brand.service';
import { MediaService } from '../../core/media.service';
import { TranslateService } from '../../core/translate.service';
import { TakeService } from './take.service';
import { ExamPreview } from './take.models';
import { AstroMarkComponent } from '../../shared/ui/astro-mark.component';
import { takerFailure } from './taker-failure';
import { DirectionService } from '../../core/direction.service';

/**
 * What a candidate sees when they follow their link.
 *
 * Opening a link must not cost an attempt. Somebody who clicks a message on a
 * bus to see how long the exam is has not started it, and a product that treats
 * that click as a start has taken something from them they cannot get back.
 * Nothing here consumes anything; the button does.
 *
 * The other job of this screen is to say plainly why a link does not work.
 * "Invalid link" leaves a candidate with nowhere to go — expired, already used
 * and not yet open are three different problems with three different answers.
 */
@Component({
  selector: 'astro-take-entry',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, AstroMarkComponent],
  templateUrl: './take-entry.component.html',
  styleUrl: './take-entry.component.scss',
})
export class TakeEntryComponent {
  private readonly take = inject(TakeService);
  private readonly router = inject(Router);

  readonly t = inject(TranslateService).t;


  /** The organisation's mark, at the API's origin rather than this app's. */

  src(url: string | null | undefined): string | null {

    return this.media.absolute(url);

  }


  /**
   * Why this link will not open, in words the candidate can act on.
   * <p>
   * Five reasons can arrive — the link is not real, it has expired, it was
   * revoked, its attempts are spent, or the exam is outside its window — and
   * they need five different answers from whoever sent it. Each already has a
   * sentence in both languages; the screen was printing the key beside them.
   * </p>
   * <p>
   * Falls back to the general sentence rather than to the key, because a
   * candidate shown a fragment of our internals is worse off than one told
   * plainly that it is not available.
   * </p>
   */
  whyBlocked(code: string | null | undefined): string {
    if (!code) {
      return this.t('::Take:NotAvailable');
    }

    const said = this.t('::' + code);

    return said === code ? this.t('::Take:NotAvailable') : said;
  }

  private readonly media = inject(MediaService);
  private readonly brand = inject(BrandService);

  readonly token = input.required<string>();

  readonly loading = signal(true);
  readonly starting = signal(false);
  readonly error = signal<string | null>(null);
  readonly preview = signal<ExamPreview | null>(null);

  readonly attemptsLeft = computed(() => {
    const preview = this.preview();

    return preview ? Math.max(preview.attemptsAllowed - preview.attemptsUsed, 0) : 0;
  });

  /** An attempt already running. Resuming continues the same clock rather than restarting it. */
  readonly canResume = computed(() => !!this.preview()?.resumableAttemptId);

  /**
   * Paint the page in the organisation's colour as soon as the link resolves.
   * <p>
   * This screen's reader is the only one in the product who did not choose this
   * platform: no account, no relationship with us, and a link from an
   * organisation they do know. The name and the mark were already theirs here;
   * the colour was not.
   * </p>
   */
  private readonly paint = effect(() =>
    this.brand.apply(this.preview()?.organizationBrandColor));

  private opened?: string;

  constructor() {
    // Read through an effect: withComponentInputBinding() sets a routed
    // component's inputs after construction.
    effect(() => {
      const token = this.token();

      if (!token || token === this.opened) {
        return;
      }

      this.opened = token;
      this.open(token);
    });
  }

  private readonly direction = inject(DirectionService);

  open(token: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.take.open(token).subscribe({
      next: preview => {
        this.preview.set(preview);
        this.take.setSession(preview.sessionToken ?? null);

        // The centre's own language, for somebody who has never chosen one. The
        // setting's hint calls it what everyone gets before they choose, and it
        // reached the staff shell and stopped there — so the one person in this
        // product with no account, no stored preference and no way to have
        // chosen was the one it never reached.
        //
        // The same call the shell makes, so the same rule holds: it does nothing
        // to a candidate who has picked a language for themselves.
        this.direction.useOrganisationDefault(preview.organizationDefaultLanguage);

        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.reason(err));
        this.loading.set(false);
      },
    });
  }

  start(): void {
    this.starting.set(true);
    this.error.set(null);

    this.take.start().subscribe({
      next: state => {
        // The credential for the sitting itself. Without this swap every question
        // after the start is requested against an attempt id of all zeroes.
        if (state.sessionToken) {
          this.take.setSession(state.sessionToken);
        }

        this.router.navigate(['/exam', this.token(), 'sitting']);
      },
      error: err => {
        this.starting.set(false);
        this.error.set(this.reason(err));
      },
    });
  }

  private reason(err: unknown): string {
    return takerFailure(err, this.t);
  }
}
