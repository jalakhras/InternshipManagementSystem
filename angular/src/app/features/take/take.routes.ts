import { Routes } from '@angular/router';

/**
 * The taker's journey — outside the shell and outside authentication.
 *
 * Someone sitting a timed exam has no account and needs no navigation. Their
 * whole credential is the token in the URL, which is exchanged once for a
 * short-lived session scoped to a single attempt.
 *
 * Three screens, and the order matters. Opening a link costs nothing, so a
 * candidate can look at what they are about to sit; starting is the deliberate
 * act that begins the clock; and the result is a separate address so a browser
 * back button lands somewhere sensible rather than back inside a submitted exam.
 */
export const TAKE_ROUTES: Routes = [
  {
    path: ':token',
    loadComponent: () => import('./take-entry.component').then(m => m.TakeEntryComponent),
  },
  {
    path: ':token/sitting',
    loadComponent: () => import('./take-sitting.component').then(m => m.TakeSittingComponent),
  },
  {
    path: ':token/result',
    loadComponent: () => import('./take-result.component').then(m => m.TakeResultComponent),
  },
];
