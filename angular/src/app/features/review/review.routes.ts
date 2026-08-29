import { Routes } from '@angular/router';

/**
 * The reviewer's work: a queue, and one attempt at a time.
 */
export const REVIEW_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./review-queue.component').then(m => m.ReviewQueueComponent),
  },
  {
    path: ':attemptId',
    loadComponent: () => import('./review-attempt.component').then(m => m.ReviewAttemptComponent),
  },
];
