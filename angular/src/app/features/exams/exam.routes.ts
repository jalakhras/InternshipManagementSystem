import { Routes } from '@angular/router';

/**
 * Exam authoring. Lazy per screen, so opening the list does not also download the
 * editor and the question builder.
 */
export const EXAM_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./exam-list.component').then(m => m.ExamListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('../placeholder/placeholder.component').then(m => m.PlaceholderComponent),
    data: { titleKey: '::Exam:Create' },
  },
  {
    path: ':id',
    loadComponent: () =>
      import('../placeholder/placeholder.component').then(m => m.PlaceholderComponent),
    data: { titleKey: '::Exam:Edit' },
  },
];
