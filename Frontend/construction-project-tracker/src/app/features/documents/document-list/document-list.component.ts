import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule],
  template: `
    <div class="page-header">
      <h1>Documents</h1>
      <a mat-flat-button color="primary" routerLink="/documents/upload">Upload Document</a>
    </div>
    <mat-card><mat-card-content><p>Document list will be displayed here.</p></mat-card-content></mat-card>
  `,
  styles: [`.page-header { display: flex; justify-content: space-between; align-items: center; }`]
})
export class DocumentListComponent {}
