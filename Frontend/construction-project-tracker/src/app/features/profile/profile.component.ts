import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <h1>Profile</h1>
    <mat-card>
      <mat-card-title>{{ authService.getCurrentUser()?.fullName }}</mat-card-title>
      <mat-card-content>
        <p>Email: {{ authService.getCurrentUser()?.email }}</p>
        <p>Role: {{ authService.getCurrentUser()?.role }}</p>
      </mat-card-content>
    </mat-card>
  `
})
export class ProfileComponent {
  constructor(public authService: AuthService) {}
}
