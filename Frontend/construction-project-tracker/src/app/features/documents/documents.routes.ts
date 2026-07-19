import { Routes } from '@angular/router';

export const DOCUMENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./document-list/document-list.component').then(m => m.DocumentListComponent)
  },
  {
    path: 'upload',
    loadComponent: () => import('./document-upload/document-upload.component').then(m => m.DocumentUploadComponent)
  }
];
