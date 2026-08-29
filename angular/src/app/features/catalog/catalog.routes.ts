import { Routes } from '@angular/router';

/**
 * The organisation's domains, levels, topics and its own words for them.
 */
export const CATALOG_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./catalog.component').then(m => m.CatalogComponent),
  },
];
