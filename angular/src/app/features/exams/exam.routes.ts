import { Routes } from '@angular/router';

/**
 * Exams routes. The screens land here in phase 3b; the route exists now so the
 * shell's navigation is wired end to end and nothing links into a void.
 */
export const EXAM_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../placeholder/placeholder.component').then(m => m.PlaceholderComponent),
    data: { titleKey: '::Nav:Exams' },
  },
];
