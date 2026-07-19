import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin.guard';

export const ASSIGNMENTS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./assignments.component').then(m => m.AssignmentsComponent)
  }
];
