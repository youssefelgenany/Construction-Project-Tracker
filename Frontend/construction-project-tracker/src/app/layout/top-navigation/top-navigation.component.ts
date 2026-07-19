import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { UserMenuComponent } from '../user-menu/user-menu.component';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  adminOnly?: boolean;
}

@Component({
  selector: 'app-top-navigation',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    UserMenuComponent,
    NotificationBellComponent
  ],
  templateUrl: './top-navigation.component.html',
  styleUrl: './top-navigation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopNavigationComponent {
  private readonly authService = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isAdmin = this.authService.isAdmin;
  readonly mobileMenuOpen = signal(false);

  readonly showDesktopNav = toSignal(
    this.breakpointObserver.observe('(min-width: 1024px)').pipe(map(result => result.matches)),
    { initialValue: false }
  );

  readonly primaryNavItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Projects', route: '/projects', icon: 'domain' },
    { label: 'Engineers', route: '/engineers', icon: 'engineering', adminOnly: true },
    { label: 'Tasks', route: '/tasks', icon: 'assignment' },
    { label: 'Reports', route: '/reports', icon: 'bar_chart', adminOnly: true },
    {
      label: 'Extensions',
      route: '/deadline-extensions',
      icon: 'event_available',
      adminOnly: true
    }
  ];

  readonly mobileNavItems: NavItem[] = [
    ...this.primaryNavItems,
    { label: 'Assignments', route: '/assignments', icon: 'group_work', adminOnly: true }
  ];

  visibleItems(items: NavItem[]): NavItem[] {
    return items.filter(item => !item.adminOnly || this.isAdmin());
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(open => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }
}
