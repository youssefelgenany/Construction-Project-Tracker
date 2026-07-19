import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin.guard';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./reports.component').then(m => m.ReportsComponent)
  }
];
