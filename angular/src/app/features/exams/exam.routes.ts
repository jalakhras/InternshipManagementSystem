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
    // Questions live under their exam: a question has no meaning apart from one,
    // and the route says so.
    path: ':examId/questions',
    loadComponent: () =>
      import('../questions/question-list.component').then(m => m.QuestionListComponent),
  },
  {
    path: ':examId/questions/new',
    loadComponent: () =>
      import('../questions/question-form.component').then(m => m.QuestionFormComponent),
  },
  {
    path: ':examId/questions/:questionId',
    loadComponent: () =>
      import('../questions/question-form.component').then(m => m.QuestionFormComponent),
  },
  {
    // The shape of a drawn paper: how many questions of what kind. Without it
    // "fill from the blueprint" on the papers screen has nothing to read.
    path: ':examId/blueprint',
    loadComponent: () => import('./exam-blueprint.component').then(m => m.ExamBlueprintComponent),
  },
  {
    // Sections and passages: how the exam is laid out, as distinct from what is
    // in it.
    path: ':examId/structure',
    loadComponent: () => import('./exam-structure.component').then(m => m.ExamStructureComponent),
  },
  {
    // Papers live under their exam for the same reason questions do.
    path: ':examId/forms',
    loadComponent: () => import('./exam-forms.component').then(m => m.ExamFormsComponent),
  },
  {
    // withComponentInputBinding() in app.config binds this to the component's
    // `id` input, so the component needs no ActivatedRoute of its own.
    path: ':id',
    loadComponent: () => import('./exam-form.component').then(m => m.ExamFormComponent),
  },
];
