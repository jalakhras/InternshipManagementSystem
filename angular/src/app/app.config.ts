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

    provideAbpOAuth(),
  ],
};

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
