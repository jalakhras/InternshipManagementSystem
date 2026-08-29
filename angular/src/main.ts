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
applyRuntimeConfig(environment)
  .then(() => Promise.all([import('./app/app.component'), import('./app/app.config')]))
  .then(([{ AppComponent }, { appConfig }]) => bootstrapApplication(AppComponent, appConfig))
  .catch(err => console.error(err));
