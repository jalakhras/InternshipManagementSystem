import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, WritableSignal, inject, signal } from '@angular/core';

import { environment } from '../../environments/environment';

/**
 * Where a stored file actually lives, and how to show one.
 *
 * The application and the API are not the same origin — the SPA is served from
 * one port and ABP from another — so a bare `/api/assessment/media/...` in an
 * `src` attribute resolves against the SPA and 404s. Every media URL in this
 * product was written that way: question images, listening clips, hotspot
 * pictures, the tenant logo, uploaded answers. A candidate could not hear a
 * listening question.
 *
 * There are two kinds of caller and they need different things:
 *
 *  · **A candidate** has no account. Their paper arrives with a signed grant
 *    already in the URL, so the address is the whole credential and it only
 *    needs the right origin in front of it — {@link absolute}.
 *
 *  · **Staff** are signed in, and a browser will not attach an Authorization
 *    header to an `<img src>` no matter what the page wants. So the file is
 *    fetched with the token like any other request and handed to the element as
 *    an object URL — {@link objectUrl}.
 */
@Injectable({ providedIn: 'root' })
export class MediaService {
  private readonly http = inject(HttpClient);

  /** One object URL per blob, so a list of twenty thumbnails is twenty requests, not two hundred. */
  private readonly objects = new Map<string, Signal<string | null>>();

  private readonly api = environment.apis.default.url.replace(/\/+$/, '');

  /**
   * A server-relative path, made absolute against the API.
   *
   * Anything already absolute is returned untouched, so this is safe to apply to
   * a URL whose origin the server may one day include.
   */
  absolute(path: string | null | undefined): string | null {
    if (!path) {
      return null;
    }

    if (/^https?:\/\//i.test(path)) {
      return path;
    }

    return this.api + (path.startsWith('/') ? path : '/' + path);
  }

  /**
   * A stored file as an object URL, fetched with the caller's credentials.
   *
   * Null until it arrives, and null if it cannot be fetched — the caller shows
   * nothing rather than a broken-image icon, which reads as a bug in the page
   * rather than a missing file.
   */
  objectUrl(blobName: string | null | undefined): Signal<string | null> {
    if (!blobName) {
      return signal<string | null>(null).asReadonly();
    }

    const cached = this.objects.get(blobName);

    if (cached) {
      return cached;
    }

    const url = signal<string | null>(null);
    this.objects.set(blobName, url.asReadonly());

    this.http
      .get(`${this.api}/api/assessment/media/${blobName}`, { responseType: 'blob' })
      .subscribe({
        next: blob => url.set(URL.createObjectURL(blob)),

        // Recorded rather than announced. A missing thumbnail is not worth an
        // error banner over the form somebody is filling in — but a caller who
        // needs to say "this could not be opened" has no other way to know,
        // because a null signal means "not here yet" and "never coming" alike.
        //
        // The entry stays in the map on purpose. Deleting it meant the next
        // change-detection pass called this method again, found nothing cached,
        // and issued a fresh request — one failed image became a request per
        // frame, for as long as the screen was open.
        error: () => this.failed(blobName).set(true),
      });

    return url.asReadonly();
  }

  /**
   * Whether this blob was asked for and could not be fetched.
   *
   * <p>
   * For callers where the difference matters. A marker looking at a candidate's
   * uploaded answer needs to be told it cannot be opened; the alternative — and
   * what the product did — is a paperclip that renders, looks like a link, and
   * does nothing when clicked, leaving them to put a number on work they never
   * saw.
   * </p>
   */
  failed(blobName: string): WritableSignal<boolean> {
    let flag = this.failures.get(blobName);

    if (!flag) {
      flag = signal(false);
      this.failures.set(blobName, flag);
    }

    return flag;
  }

  private readonly failures = new Map<string, WritableSignal<boolean>>();
}
