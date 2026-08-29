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
    loadComponent: () => import('./exam-form.component').then(m => m.ExamFormComponent),
  },
  {
    // withComponentInputBinding() in app.config binds this to the component's
    // `id` input, so the component needs no ActivatedRoute of its own.
    path: ':id',
    loadComponent: () => import('./exam-form.component').then(m => m.ExamFormComponent),
  },
];
