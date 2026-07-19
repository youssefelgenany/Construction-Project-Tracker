import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { AssignmentService } from '../../../../core/services/assignment.service';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { ProjectAssignedEngineer } from '../../../../core/models/project-assigned-engineer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ConfirmationDialogComponent } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import { premiumDialogConfig } from '../../../../shared/dialogs/premium-dialog.config';
import { AssignEngineerDialogComponent } from '../../dialogs/assign-engineer-dialog/assign-engineer-dialog.component';
import { AssignEngineerDialogData } from '../../dialogs/assign-engineer-dialog-data';

@Component({
  selector: 'app-project-engineers-tab',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './project-engineers-tab.component.html',
  styleUrl: './project-engineers-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectEngineersTabComponent implements OnInit {
  private readonly assignmentService = inject(AssignmentService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<number>();
  readonly assignmentChanged = output<void>();

  readonly isAdmin = this.authService.isAdmin;
  readonly assignments = signal<ProjectAssignedEngineer[]>([]);
  readonly isLoading = signal(false);
  readonly hasLoaded = signal(false);

  readonly baseColumns = ['fullName', 'email', 'position', 'assignedDate'] as const;
  readonly adminColumns = [...this.baseColumns, 'actions'] as const;

  visibleColumns(): string[] {
    return this.isAdmin() ? [...this.adminColumns] : [...this.baseColumns];
  }

  ngOnInit(): void {
    this.loadAssignments();
  }

  refresh(): void {
    this.loadAssignments();
  }

  openAssignDialog(): void {
    const data: AssignEngineerDialogData = { projectId: this.projectId() };

    this.dialog
      .open(AssignEngineerDialogComponent, { width: '820px', maxWidth: '95vw', data })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(assigned => {
        if (assigned) {
          this.loadAssignments();
          this.assignmentChanged.emit();
        }
      });
  }

  removeAssignment(assignment: ProjectAssignedEngineer): void {
    const dialogData: ConfirmationDialogData = {
      title: 'Remove Assignment',
      message: `Remove "${assignment.fullName}" from this project?`,
      confirmText: 'Remove',
      cancelText: 'Cancel'
    };

    this.dialog
      .open(ConfirmationDialogComponent, premiumDialogConfig('520px', { data: dialogData }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) {
          return;
        }

        this.assignmentService.remove(assignment.id).subscribe({
          next: () => {
            this.notificationService.success('Engineer removed from project successfully.');
            this.loadAssignments();
            this.assignmentChanged.emit();
          },
          error: () => {
            // Global error interceptor handles snackbar for most errors.
          }
        });
      });
  }

  trackByAssignmentId(_: number, assignment: ProjectAssignedEngineer): number {
    return assignment.id;
  }

  private loadAssignments(): void {
    this.isLoading.set(true);

    this.assignmentService
      .getByProject(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: assignments => {
          this.assignments.set(assignments);
          this.isLoading.set(false);
          this.hasLoaded.set(true);
        },
        error: () => {
          this.isLoading.set(false);
          this.hasLoaded.set(true);
        }
      });
  }
}
