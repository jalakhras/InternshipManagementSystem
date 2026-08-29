import { Routes } from '@angular/router';

/**
 * Sending an exam out. Always in the context of one exam, because "assign" with
 * no exam named is a question rather than an action.
 */
export const ASSIGNMENT_ROUTES: Routes = [
  {
    path: ':examId',
    loadComponent: () => import('./assignment.component').then(m => m.AssignmentComponent),
  },
];
