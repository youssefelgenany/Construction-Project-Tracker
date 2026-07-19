import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog } from '@angular/material/dialog';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import {
  DeadlineExtensionRequest,
  ExtensionRequestStatus
} from '../../../core/models/deadline-extension.model';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { ReviewDeadlineExtensionDialogComponent } from '../dialogs/review-deadline-extension-dialog.component';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';

@Component({
  selector: 'app-deadline-extension-requests',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTabsModule,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './deadline-extension-requests.component.html',
  styleUrl: './deadline-extension-requests.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeadlineExtensionRequestsComponent implements OnInit {
  private readonly service = inject(DeadlineExtensionService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(true);
  readonly requests = signal<DeadlineExtensionRequest[]>([]);
  readonly selectedTab = signal(0);
  readonly ExtensionRequestStatus = ExtensionRequestStatus;

  readonly filteredRequests = computed(() => {
    const status =
      this.selectedTab() === 0
        ? ExtensionRequestStatus.Pending
        : this.selectedTab() === 1
          ? ExtensionRequestStatus.Approved
          : ExtensionRequestStatus.Rejected;
    return this.requests().filter(r => r.status === status);
  });

  ngOnInit(): void {
    this.load();
  }

  onTabChange(index: number): void {
    this.selectedTab.set(index);
  }

  statusClass(status: ExtensionRequestStatus): string {
    switch (status) {
      case ExtensionRequestStatus.Approved:
        return 'badge-approved';
      case ExtensionRequestStatus.Rejected:
        return 'badge-rejected';
      default:
        return 'badge-pending';
    }
  }

  statusLabel(status: ExtensionRequestStatus): string {
    switch (status) {
      case ExtensionRequestStatus.Approved:
        return 'Approved';
      case ExtensionRequestStatus.Rejected:
        return 'Rejected';
      default:
        return 'Pending';
    }
  }

  openReview(request: DeadlineExtensionRequest, mode: 'approve' | 'reject'): void {
    this.dialog
      .open(
        ReviewDeadlineExtensionDialogComponent,
        premiumDialogConfig('640px', { data: { mode, request } })
      )
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.load();
        }
      });
  }

  private load(): void {
    this.isLoading.set(true);
    this.service
      .getAdminRequests()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items: DeadlineExtensionRequest[]) => {
          this.requests.set(items);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }
}
