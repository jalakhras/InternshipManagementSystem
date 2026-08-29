import { Routes } from '@angular/router';

/**
 * Classes: one group of people moving through a level together.
 */
export const GROUP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./group-list.component').then(m => m.GroupListComponent),
  },
];
