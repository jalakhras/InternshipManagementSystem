import { Routes } from '@angular/router';

/**
 * The shared question bank.
 *
 * Questions also live under an exam — see EXAM_ROUTES — and the same two screens
 * serve both. The difference is what the question belongs to: a bank question is
 * filed under a domain and a level and can be drawn by every exam at that level,
 * where an exam's own question belongs to that paper alone.
 *
 * This tree was missing, so the sidebar's Question bank link fell through to the
 * dashboard and nothing in the product could write a bank question at all.
 */
export const QUESTION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./question-list.component').then(m => m.QuestionListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./question-form.component').then(m => m.QuestionFormComponent),
  },
  {
    path: ':questionId',
    loadComponent: () =>
      import('./question-form.component').then(m => m.QuestionFormComponent),
  },
];
