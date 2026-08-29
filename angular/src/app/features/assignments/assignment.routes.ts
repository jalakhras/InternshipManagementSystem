import { Routes } from '@angular/router';

/**
 * Sending an exam out. Always in the context of one exam, because "assign" with
 * no exam named is a question rather than an action.
 */
export const ASSIGNMENT_ROUTES: Routes = [
  {
    // The sidebar and the dashboard both link here. Without this route they fell
    // through to the dashboard, so two of the most prominent links in the product
    // did nothing at all. The screen asks the question the route implies rather
    // than pretending the link should not exist.
    path: '',
    loadComponent: () =>
      import('./assignment-picker.component').then(m => m.AssignmentPickerComponent),
  },
  {
    path: ':examId',
    loadComponent: () => import('./assignment.component').then(m => m.AssignmentComponent),
  },
];
