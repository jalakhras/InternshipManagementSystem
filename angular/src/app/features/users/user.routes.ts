import { Routes } from '@angular/router';

/**
 * Staff accounts. Candidates never appear here — a link is their credential.
 */
export const USER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./user-list.component').then(m => m.UserListComponent),
  },
];
