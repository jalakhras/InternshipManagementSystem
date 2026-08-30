import { bootstrapApplication } from '@angular/platform-browser';

import { environment } from './environments/environment';
import { applyRuntimeConfig } from './environments/runtime-config';

/**
 * The deployed configuration is overlaid before anything else loads.
 *
 * `app.config` and the services under it read values off `environment` as they are
 * evaluated, so the overlay has to finish first — hence the dynamic imports.
 * Importing them at the top of this file would evaluate both modules while the
 * fetch was still in flight, and the API URL a service captured would be the one
 * compiled into the bundle rather than the one this environment was given.
 */
/**
 * A candidate's link is not a staff page, and a stale staff session must not
 * take it away from them.
 * <p>
 * ABP registers its OAuth bootstrap for the whole application. On any route,
 * including the taker's, it finds an expired access token beside a refresh
 * token and tries to refresh. When that refresh fails it calls
 * `navigateToLogin()` with no arguments, which starts a code flow whose
 * `redirect_uri` is the app root — so the deep link is discarded, the still-valid
 * sign-in cookie round-trips straight back, and the candidate lands on the staff
 * dashboard. No error is shown. The link simply does not open.
 * </p>
 * <p>
 * It is worse than it sounds because of who hits it: the person most likely to
 * open an exam link in a browser that once held a staff session is the
 * coordinator checking that the link works. It fails the morning after, not the
 * afternoon they tested it.
 * </p>
 * <p>
 * Only an already-expired session is cleared, and only on the taker's tree. A
 * coordinator who is still signed in stays signed in.
 * </p>
 */
function dropExpiredStaffSessionOnTakerRoutes(): void {
  if (!location.pathname.startsWith('/exam/')) {
    return;
  }

  try {
    const expiresAt = Number(localStorage.getItem('expires_at') ?? 0);

    if (expiresAt && expiresAt >= Date.now()) {
      return;
    }

    for (const key of [
      'access_token',
      'refresh_token',
      'id_token',
      'expires_at',
      'nonce',
      'PKCE_verifier',
    ]) {
      localStorage.removeItem(key);
    }
  } catch {
    // A browser refusing storage has no stale session to clear either.
  }
}

dropExpiredStaffSessionOnTakerRoutes();

applyRuntimeConfig(environment)
  .then(() => Promise.all([import('./app/app.component'), import('./app/app.config')]))
  .then(([{ AppComponent }, { appConfig }]) => bootstrapApplication(AppComponent, appConfig))
  .catch(err => console.error(err));
