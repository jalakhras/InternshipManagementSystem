import { Routes } from '@angular/router';

/**
 * What happened when people sat the exam.
 *
 * "questions" is declared before ":attemptId" so it is not read as an id.
 */
export const RESULT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./result-list.component').then(m => m.ResultListComponent),
  },
  {
    path: 'questions',
    loadComponent: () => import('./item-analysis.component').then(m => m.ItemAnalysisComponent),
  },
  {
    // Sittings in progress. Under results because it is the same question asked
    // a few minutes earlier: how is this going.
    path: 'running',
    loadComponent: () => import('./attempt-monitor.component').then(m => m.AttemptMonitorComponent),
  },
  {
    path: ':attemptId',
    loadComponent: () => import('./result-detail.component').then(m => m.ResultDetailComponent),
  },
];
