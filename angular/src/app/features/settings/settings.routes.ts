import { Routes } from '@angular/router';

/**
 * What this organisation changes about the platform for itself.
 */
export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./settings.component').then(m => m.SettingsComponent),
  },
];
