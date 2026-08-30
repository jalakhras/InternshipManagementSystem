import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideAbpCore, withOptions } from '@abp/ng.core';
import { provideAbpOAuth } from '@abp/ng.oauth';

import localeAr from '@angular/common/locales/ar';
import { registerLocaleData } from '@angular/common';

import { environment } from '../environments/environment';
import { APP_ROUTES } from './app.routes';

/**
 * Application configuration.
 *
 * Only the two ABP packages that carry no UI: ng.core (auth state, localization,
 * permissions, remote config) and ng.oauth (the OIDC client). ABP's component
 * library and its identity / account / tenant screens are deliberately absent —
 * they are built for the theme this app replaced, and keeping them would have
 * pinned Angular to whatever version ng-bootstrap and ngx-datatable had reached.
 * The screens they provide are ours to build, in our own shell.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // Zoneless. The exam screen runs a one-second countdown for up to an hour;
    // under Zone.js every tick would walk the whole component tree. Signals
    // notify only what actually reads the value.
    provideZonelessChangeDetection(),

    provideRouter(
      APP_ROUTES,
      withComponentInputBinding(),
      // Returning to a list should return to where the reader was, not to the top.
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),

    provideHttpClient(withInterceptorsFromDi()),

    provideAbpCore(
      withOptions({
        environment,
        registerLocaleFn: registerLocale(),
      }),
    ),

    // Everywhere except the candidate's tree.
    //
    // ABP's OAuth bootstrap runs on every route. On the taker's it finds a
    // staff session it cannot refresh, calls `navigateToLogin()` with no
    // argument, and starts a code flow whose `redirect_uri` is the app root —
    // so the candidate's deep link is thrown away and the still-valid sign-in
    // cookie lands them on the staff dashboard, with no error anywhere.
    //
    // The taker needs none of it: `TakeService` carries its own `X-Exam-Session`
    // header on a plain `HttpClient` and never asks ABP for a token. So the flow
    // is simply not registered there.
    //
    // An earlier attempt cleared the stored tokens on these routes instead. It
    // worked, and it was wrong: ABP keeps `expires_at` in memory rather than in
    // storage, so the "only if the session has already expired" guard never
    // fired once — and every visit to an exam link deleted a signed-in
    // coordinator's refresh token. Checking a link logged you out of your own
    // product, and the test written to prove the fix passed anyway, because
    // deleting the token does stop the redirect.
    ...(isTakerRoute() ? [] : [provideAbpOAuth()]),
  ],
};

/**
 * Whether this page load is a candidate opening their link.
 * <p>
 * Read from the address rather than the router, because the decision has to be
 * made while the providers are being built — before any route has resolved.
 * </p>
 */
function isTakerRoute(): boolean {
  return typeof location !== 'undefined' && location.pathname.startsWith('/exam/');
}

/**
 * Loads Angular's locale data on demand.
 *
 * Arabic needs its own data for dates, plurals and number formatting. Without it
 * a date pipe silently falls back to English formatting inside an Arabic page,
 * which reads as a bug long before anyone traces it to a missing locale.
 */
function registerLocale() {
  // Registered eagerly rather than imported on demand. The dynamic import was a
  // reasonable saving — a few kilobytes for anyone using English — but it put an
  // extra await between bootstrap and the remote configuration, and that was
  // enough to let a list response arrive before the permission state did. Rows
  // then rendered with every action hidden, in Arabic only.
  registerLocaleData(localeAr);

  return async (_locale: string) => {};
}
