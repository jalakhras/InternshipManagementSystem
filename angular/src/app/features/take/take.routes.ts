import { Routes } from '@angular/router';

/**
 * The taker's journey — outside the shell and outside authentication.
 *
 * Someone sitting a timed exam has no account and needs no navigation. Their
 * whole credential is the token in the URL, which is exchanged once for a
 * short-lived session scoped to a single attempt.
 */
export const TAKE_ROUTES: Routes = [
  {
    path: ':token',
    loadComponent: () =>
      import('../placeholder/placeholder.component').then(m => m.PlaceholderComponent),
    data: { titleKey: '::Exam:Take' },
  },
];
