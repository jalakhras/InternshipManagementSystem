import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';
import { ShellComponent } from './layout/shell.component';

/**
 * Route table.
 *
 * Two trees on purpose:
 *
 *   · /exam/**  — the taker's journey. No shell, no login, no navigation. Someone
 *     sitting a timed exam should see the exam and nothing else, and they have no
 *     account to authenticate with; a link token is their entire credential.
 *
 *   · everything else — the staff application, behind the shell and a session.
 *
 * Every feature is lazy: an operator who only reviews answers should not download
 * the exam authoring screens to find that out.
 */
export const APP_ROUTES: Routes = [
  {
    path: 'exam',
    loadChildren: () => import('./features/take/take.routes').then(m => m.TAKE_ROUTES),
  },

  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'exams',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Exams.View' },
        loadChildren: () => import('./features/exams/exam.routes').then(m => m.EXAM_ROUTES),
      },
      {
        path: 'questions',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Questions.View' },
        loadChildren: () =>
          import('./features/questions/question.routes').then(m => m.QUESTION_ROUTES),
      },
      {
        path: 'candidates',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Candidates.View' },
        loadChildren: () =>
          import('./features/candidates/candidate.routes').then(m => m.CANDIDATE_ROUTES),
      },
      {
        path: 'groups',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Groups.View' },
        loadChildren: () => import('./features/groups/group.routes').then(m => m.GROUP_ROUTES),
      },
      {
        path: 'assignments',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Assignments.View' },
        loadChildren: () =>
          import('./features/assignments/assignment.routes').then(m => m.ASSIGNMENT_ROUTES),
      },
      {
        path: 'results',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Results.View' },
        loadChildren: () => import('./features/results/result.routes').then(m => m.RESULT_ROUTES),
      },
      {
        path: 'catalog',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Catalog.View' },
        loadChildren: () => import('./features/catalog/catalog.routes').then(m => m.CATALOG_ROUTES),
      },
      {
        // No permission guard: everybody signed in may read the settings, and the
        // screen is read-only without ManageSettings. Knowing the rules the exams
        // run under is not a privilege.
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES),
      },
      {
        path: 'review',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Assessment.Review.ViewQueue' },
        loadChildren: () => import('./features/review/review.routes').then(m => m.REVIEW_ROUTES),
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
