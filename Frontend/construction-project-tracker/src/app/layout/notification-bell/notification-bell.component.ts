import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { AppNotificationService } from '../../core/services/app-notification.service';
import { AppNotification, NotificationType } from '../../core/models/notification.model';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule, MatBadgeModule, MatTooltipModule],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationBellComponent implements OnInit {
  private readonly notificationApi = inject(AppNotificationService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly menuTrigger = viewChild(MatMenuTrigger);

  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = this.notificationApi.unreadCount;

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.refresh();
    }
  }

  onMenuOpened(): void {
    this.refresh();
  }

  markAllRead(event?: Event): void {
    event?.stopPropagation();
    this.notificationApi
      .markAllAsRead()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notifications.update(items => items.map(n => ({ ...n, isRead: true })));
        }
      });
  }

  openNotification(notification: AppNotification): void {
    if (!notification.isRead) {
      this.notificationApi
        .markAsRead(notification.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.notifications.update(items =>
              items.map(n => (n.id === notification.id ? { ...n, isRead: true } : n))
            );
          }
        });
    }

    this.menuTrigger()?.closeMenu();

    if (notification.relatedEntityType === 'Task' && notification.relatedEntityId) {
      void this.router.navigate(['/tasks', notification.relatedEntityId]);
    } else if (notification.relatedEntityType === 'Project' && notification.relatedEntityId) {
      void this.router.navigate(['/projects', notification.relatedEntityId]);
    } else if (this.authService.isAdmin()) {
      void this.router.navigate(['/deadline-extensions']);
    }
  }

  iconFor(type: NotificationType): string {
    switch (type) {
      case NotificationType.DeadlineExtensionApproved:
        return 'check_circle';
      case NotificationType.DeadlineExtensionRejected:
        return 'cancel';
      case NotificationType.DeadlineExtended:
        return 'event_available';
      default:
        return 'schedule_send';
    }
  }

  iconClassFor(type: NotificationType): string {
    switch (type) {
      case NotificationType.DeadlineExtensionApproved:
        return 'icon--success';
      case NotificationType.DeadlineExtensionRejected:
        return 'icon--danger';
      case NotificationType.DeadlineExtended:
        return 'icon--primary';
      default:
        return 'icon--warning';
    }
  }

  relativeTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const diffMs = Date.now() - date.getTime();
    const minutes = Math.max(0, Math.floor(diffMs / 60_000));

    if (minutes < 1) {
      return 'Just now';
    }
    if (minutes < 60) {
      return `${minutes} min ago`;
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
    }

    const days = Math.floor(hours / 24);
    if (days === 1) {
      return 'Yesterday';
    }
    if (days < 7) {
      return `${days} days ago`;
    }

    return date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  private refresh(): void {
    this.notificationApi.refreshUnreadCount();
    this.notificationApi
      .getMine(20)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: items => this.notifications.set(items),
        error: () => this.notifications.set([])
      });
  }
}
