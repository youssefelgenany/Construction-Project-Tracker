import { Component } from '@angular/core';
import { AppShellComponent } from '../../layout/app-shell/app-shell.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [AppShellComponent],
  template: '<app-app-shell />',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent {}
