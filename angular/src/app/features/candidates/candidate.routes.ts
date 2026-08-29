import { Routes } from '@angular/router';

/**
 * Candidates and the cohorts they belong to.
 */
export const CANDIDATE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./candidate-list.component').then(m => m.CandidateListComponent),
  },
];
