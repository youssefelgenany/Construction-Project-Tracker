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
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { map } from 'rxjs';
import { TaskStatus } from '../../../../core/enums/task-status';
import { TaskService } from '../../../../core/services/task.service';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Task } from '../../../../core/models/task';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ConfirmationDialogComponent } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { premiumDialogConfig } from '../../../../shared/dialogs/premium-dialog.config';
import { ConfirmationDialogData } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import { TaskDialogComponent } from '../../dialogs/task-dialog/task-dialog.component';
import { TaskDialogData } from '../../dialogs/task-dialog-data';
import { TaskCardComponent } from '../task-card/task-card.component';
import { ManageTaskDependenciesDialogComponent } from '../../dialogs/manage-task-dependencies-dialog/manage-task-dependencies-dialog.component';
import {
  getTaskPriorityClass,
  getTaskPriorityLabel,
  getTaskStatusClass,
  getTaskStatusLabel
} from '../../projects.utils';

@Component({
  selector: 'app-project-tasks-tab',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressBarModule,
    MatTooltipModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    TaskCardComponent
  ],
  templateUrl: './project-tasks-tab.component.html',
  styleUrl: './project-tasks-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectTasksTabComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly projectId = input.required<number>();
  readonly tasksChanged = output<void>();

  readonly isAdmin = this.authService.isAdmin;
  readonly tasks = signal<Task[]>([]);
  readonly isLoading = signal(false);

  readonly isMobile = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  readonly TaskStatus = TaskStatus;

  readonly baseColumns = [
    'title',
    'engineerName',
    'priority',
    'status',
    'completion',
    'startDate',
    'dueDate'
  ] as const;

  readonly adminColumns = [...this.baseColumns, 'actions'] as const;

  readonly engineerColumns = [
    'title',
    'priority',
    'status',
    'completion',
    'startDate',
    'dueDate',
    'actions'
  ] as const;

  ngOnInit(): void {
    this.loadTasks();
  }

  visibleColumns(): string[] {
    return this.isAdmin() ? [...this.adminColumns] : [...this.engineerColumns];
  }

  openAddDialog(): void {
    this.openTaskDialog();
  }

  openEditDialog(task: Task): void {
    this.openTaskDialog(task);
  }

  openDependencyDialog(task: Task): void {
    this.dialog
      .open(ManageTaskDependenciesDialogComponent, {
        width: '560px',
        data: { projectId: this.projectId(), task }
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(changed => {
        if (changed) {
          this.loadTasks();
          this.tasksChanged.emit();
        }
      });
  }

  isBlocked(task: Task): boolean {
    return task.status === TaskStatus.Blocked;
  }

  prerequisiteTitles(task: Task): string {
    return (task.incompletePrerequisites ?? []).map(p => p.title).join(', ');
  }

  deleteTask(task: Task): void {
    const dialogData: ConfirmationDialogData = {
      title: 'Delete Task',
      message: `Are you sure you want to delete "${task.title}"? This action cannot be undone.`,
      confirmText: 'Delete',
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

        this.taskService.delete(task.id).subscribe({
          next: () => {
            this.notificationService.success('Task deleted successfully.');
            this.loadTasks();
            this.tasksChanged.emit();
          }
        });
      });
  }

  getPriorityLabel = getTaskPriorityLabel;
  getPriorityClass = getTaskPriorityClass;
  getStatusLabel = getTaskStatusLabel;
  getStatusClass = getTaskStatusClass;

  trackByTaskId(_: number, task: Task): number {
    return task.id;
  }

  private openTaskDialog(task?: Task): void {
    const data: TaskDialogData = {
      projectId: this.projectId(),
      task
    };

    this.dialog
      .open(TaskDialogComponent, premiumDialogConfig('780px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadTasks();
          this.tasksChanged.emit();
        }
      });
  }

  private loadTasks(): void {
    this.isLoading.set(true);

    this.taskService
      .getProjectTasks(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.tasks.set(result.items);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }
}
