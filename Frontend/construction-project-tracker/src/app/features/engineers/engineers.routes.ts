import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin.guard';

export const ENGINEERS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./engineer-list/engineer-list.component').then(m => m.EngineerListComponent)
  },
  {
    path: ':id',
    canActivate: [adminGuard],
    loadComponent: () => import('./engineer-details/engineer-details.component').then(m => m.EngineerDetailsComponent)
  }
];
