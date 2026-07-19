import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-assignments',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <h1>Assignments</h1>
    <mat-card><mat-card-content><p>Project assignments will be managed here.</p></mat-card-content></mat-card>
  `
})
export class AssignmentsComponent {}
