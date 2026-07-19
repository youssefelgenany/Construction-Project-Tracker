import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule],
  template: `
    <h1>Upload Document</h1>
    <mat-card><mat-card-content>
      <p>Document upload form will be implemented here.</p>
      <a mat-button routerLink="/documents">Back to List</a>
    </mat-card-content></mat-card>
  `
})
export class DocumentUploadComponent {}
