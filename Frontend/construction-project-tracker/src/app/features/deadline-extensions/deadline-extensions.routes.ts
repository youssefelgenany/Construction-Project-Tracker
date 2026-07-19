import { Routes } from '@angular/router';
import { adminGuard } from '../../core/guards/admin.guard';

export const DEADLINE_EXTENSION_ROUTES: Routes = [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./deadline-extension-requests/deadline-extension-requests.component').then(
        m => m.DeadlineExtensionRequestsComponent
      )
  }
];
