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
 * Keeps a candidate on their own link when the sign-in flow bounces the browser.
 * <p>
 * ABP's OAuth bootstrap runs on every route. On a taker's link it can meet a
 * staff session it cannot refresh, and then calls `navigateToLogin()` with no
 * argument — starting a code flow whose `redirect_uri` is the app root. The
 * deep link is discarded, the still-valid sign-in cookie completes the round
 * trip silently, and the candidate arrives at the staff dashboard. Nothing
 * errors; the link simply does not open.
 * </p>
 * <p>
 * The address is remembered on the way in and restored if the app is entered at
 * the root moments later. Sixty seconds, because this is only ever repairing a
 * redirect that has just happened — a coordinator who opens a link and then
 * deliberately goes to the dashboard a minute later is not bounced back.
 * </p>
 * <p>
 * Not solved by leaving ABP's OAuth unregistered on these routes, which was
 * tried: ABP's fallback route guard then refuses every guarded route and logs an
 * error on the candidate's own screen — a worse thing to ship than the redirect.
 * </p>
 */
const TAKER_RETURN = 'astro.takerReturn';

function rememberOrRestoreTakerLink(): void {
  try {
    if (location.pathname.startsWith('/exam/')) {
      sessionStorage.setItem(TAKER_RETURN, JSON.stringify({ at: Date.now(), url: location.href }));
      return;
    }

    if (location.pathname !== '/') {
      return;
    }

    const kept = sessionStorage.getItem(TAKER_RETURN);

    if (!kept) {
      return;
    }

    sessionStorage.removeItem(TAKER_RETURN);

    const { at, url } = JSON.parse(kept) as { at: number; url: string };

    if (Date.now() - at < 60_000) {
      location.replace(url);
    }
  } catch {
    // A browser refusing storage keeps the old behaviour, which is the
    // behaviour everything else already handles.
  }
}

rememberOrRestoreTakerLink();

applyRuntimeConfig(environment)
  .then(() => Promise.all([import('./app/app.component'), import('./app/app.config')]))
  .then(([{ AppComponent }, { appConfig }]) => bootstrapApplication(AppComponent, appConfig))
  .catch(err => console.error(err));
