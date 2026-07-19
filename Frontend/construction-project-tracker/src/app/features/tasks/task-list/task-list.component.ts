import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { map } from 'rxjs';
import { TaskPriority } from '../../../core/enums/task-priority';
import { RiskLevel } from '../../../core/enums/risk-level';
import { TaskStatus } from '../../../core/enums/task-status';
import { TaskService } from '../../../core/services/task.service';
import { EngineerService } from '../../../core/services/engineer.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Task } from '../../../core/models/task';
import { Engineer } from '../../../core/models/engineer';
import { TaskRisk } from '../../../core/models/task-risk.model';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../../shared/components/search-box/search-box.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { NoResultsComponent } from '../../../shared/components/no-results/no-results.component';
import { ConfirmationDialogComponent } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import { TaskCompletionReportService } from '../../../core/services/task-completion-report.service';
import { DocumentPreviewDialogComponent } from '../../projects/dialogs/document-preview-dialog/document-preview-dialog.component';
import { DocumentPreviewDialogData } from '../../projects/dialogs/document-preview-dialog-data';
import { RejectCompletionReportDialogComponent } from '../dialogs/reject-completion-report-dialog/reject-completion-report-dialog.component';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { RejectCompletionReportDialogData } from '../dialogs/reject-completion-report-dialog-data';
import {
  canPreviewCompletionReport,
  getDocumentPreviewType,
  triggerBlobDownload
} from '../completion-report.utils';
import {
  getTaskPriorityClass,
  getTaskPriorityLabel,
  getTaskStatusClass,
  getTaskStatusLabel
} from '../../projects/projects.utils';
import { TaskDialogComponent } from '../../projects/dialogs/task-dialog/task-dialog.component';
import { TaskDialogData } from '../../projects/dialogs/task-dialog-data';
import { RiskChipComponent } from '../../../shared/components/risk-chip/risk-chip.component';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressBarModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
    PageHeaderComponent,
    SearchBoxComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NoResultsComponent,
    RiskChipComponent
  ],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskListComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly completionReportService = inject(TaskCompletionReportService);
  private readonly engineerService = inject(EngineerService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isAdmin = this.authService.isAdmin;
  readonly isMobile = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  readonly tasks = signal<TaskRisk[]>([]);
  readonly engineers = signal<Engineer[]>([]);
  readonly isLoading = signal(false);
  readonly totalCount = signal(0);
  readonly searchTerm = signal('');
  readonly reportActionTaskId = signal<number | null>(null);

  readonly TaskStatus = TaskStatus;

  readonly filterProjectId = signal<number | null>(null);
  readonly filterEngineerId = signal<number | null>(null);
  readonly filterStatus = signal<TaskStatus | null>(null);
  readonly filterPriority = signal<TaskPriority | null>(null);
  readonly filterRiskLevel = signal<RiskLevel | null>(null);
  readonly sortBy = signal<'risk' | 'overdue' | 'deadline' | 'startdate' | 'duedate'>('risk');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');
  readonly RiskLevel = RiskLevel;

  readonly pageTitle = computed(() => (this.isAdmin() ? 'Tasks' : 'My Tasks'));
  readonly pageSubtitle = computed(() =>
    this.isAdmin()
      ? 'Review task progress, priorities, deadlines, and completion status across all projects'
      : 'Tasks assigned to you, sorted by urgency'
  );

  readonly priorityOptions = [
    TaskPriority.Low,
    TaskPriority.Medium,
    TaskPriority.High,
    TaskPriority.Critical
  ];
  readonly statusOptions = [
    TaskStatus.NotStarted,
    TaskStatus.InProgress,
    TaskStatus.PendingReview,
    TaskStatus.Completed
  ];
  readonly riskOptions = [RiskLevel.None, RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical];
  readonly sortOptions = [
    { value: 'risk' as const, label: 'Highest Risk' },
    { value: 'overdue' as const, label: 'Most Overdue' },
    { value: 'deadline' as const, label: 'Closest Deadline' },
    { value: 'startdate' as const, label: 'Start Date' },
    { value: 'duedate' as const, label: 'Due Date' }
  ];

  readonly adminColumns = [
    'title',
    'projectName',
    'engineerName',
    'risk',
    'priority',
    'status',
    'completion',
    'completionReport',
    'startDate',
    'dueDate',
    'actions'
  ] as const;

  readonly engineerColumns = [
    'title',
    'projectName',
    'risk',
    'priority',
    'status',
    'completion',
    'startDate',
    'dueDate',
    'actions'
  ] as const;

  ngOnInit(): void {
    if (this.isAdmin()) {
      this.loadFilterOptions();
    }

    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.searchTerm.set(params.get('search') ?? '');
        this.filterProjectId.set(this.toNullableNumber(params.get('projectId')));
        this.filterEngineerId.set(this.toNullableNumber(params.get('engineerId')));
        this.filterStatus.set(this.toNullableStatus(params.get('status')));
        this.filterPriority.set(this.toNullablePriority(params.get('priority')));
        this.filterRiskLevel.set(this.toNullableRiskLevel(params.get('riskLevel')));
        this.sortBy.set(this.toSortBy(params.get('sortBy')));
        this.sortDirection.set(this.toSortDirection(params.get('sortDir')));
        this.loadTasks();
      });
  }

  visibleColumns(): string[] {
    return this.isAdmin() ? [...this.adminColumns] : [...this.engineerColumns];
  }

  hasActiveFilters(): boolean {
    return !!(
      this.searchTerm() ||
      this.filterProjectId() !== null ||
      this.filterEngineerId() !== null ||
      this.filterStatus() !== null ||
      this.filterPriority() !== null ||
      this.filterRiskLevel() !== null ||
      this.sortBy() !== 'risk' ||
      this.sortDirection() !== 'desc'
    );
  }

  onSearchChange(term: string): void {
    this.updateQueryParams({ search: term || null });
  }

  onFilterChange(
    key: 'projectId' | 'engineerId' | 'status' | 'priority' | 'riskLevel',
    value: string | number | null
  ): void {
    this.updateQueryParams({ [key]: value === '' || value === null ? null : value });
  }

  onSortFilterChange(value: 'risk' | 'overdue' | 'deadline' | 'startdate' | 'duedate'): void {
    this.updateQueryParams({ sortBy: value });
  }

  onSortDirectionToggle(): void {
    this.updateQueryParams({
      sortDir: this.sortDirection() === 'asc' ? 'desc' : 'asc'
    });
  }

  clearFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: null,
        projectId: null,
        engineerId: null,
        status: null,
        priority: null,
        riskLevel: null,
        sortBy: 'risk',
        sortDir: 'desc'
      }
    });
  }

  openTask(task: TaskRisk): void {
    void this.router.navigate(['/tasks', task.id]);
  }

  editTask(task: TaskRisk): void {
    this.taskService
      .getById(task.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: fullTask => {
          const data: TaskDialogData = {
            projectId: fullTask.projectId,
            task: {
              ...fullTask,
              projectName: fullTask.projectName || fullTask.project?.name || ''
            }
          };

          this.dialog
            .open(TaskDialogComponent, premiumDialogConfig('780px', { data }))
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(saved => {
              if (saved) {
                this.loadTasks();
              }
            });
        }
      });
  }

  deleteTask(task: TaskRisk): void {
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
          }
        });
      });
  }

  viewCompletionReport(task: TaskRisk): void {
    const report = task.completionReport;
    if (!report || this.reportActionTaskId() === task.id) {
      return;
    }

    if (!canPreviewCompletionReport(report.extension)) {
      this.notificationService.info('Preview is not available for this file type. Download instead.');
      this.downloadCompletionReport(task);
      return;
    }

    this.reportActionTaskId.set(task.id);

    this.completionReportService
      .download(task.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.reportActionTaskId.set(null);
          const previewType = getDocumentPreviewType(report.extension);

          if (previewType === 'image') {
            const blobUrl = URL.createObjectURL(blob);
            const data: DocumentPreviewDialogData = {
              fileName: report.originalFileName,
              blobUrl
            };
            this.dialog.open(DocumentPreviewDialogComponent, {
              width: '720px',
              maxWidth: '95vw',
              data
            });
            return;
          }

          if (previewType === 'pdf') {
            const blobUrl = URL.createObjectURL(blob);
            window.open(blobUrl, '_blank', 'noopener,noreferrer');
            window.setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000);
            return;
          }

          triggerBlobDownload(blob, report.originalFileName);
        },
        error: () => this.reportActionTaskId.set(null)
      });
  }

  downloadCompletionReport(task: TaskRisk): void {
    const report = task.completionReport;
    if (!report) {
      return;
    }

    this.completionReportService
      .download(task.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => triggerBlobDownload(blob, report.originalFileName)
      });
  }

  approveCompletionReport(task: TaskRisk): void {
    const dialogData: ConfirmationDialogData = {
      title: 'Approve Completion',
      message: `Approve the completion report for "${task.title}" and mark the task as completed?`,
      confirmText: 'Approve',
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

        this.completionReportService.approve(task.id).subscribe({
          next: () => {
            this.notificationService.success('Completion report approved.');
            this.loadTasks();
          }
        });
      });
  }

  rejectCompletionReport(task: TaskRisk): void {
    const data: RejectCompletionReportDialogData = {
      taskId: task.id,
      taskTitle: task.title
    };

    this.dialog
      .open(RejectCompletionReportDialogComponent, premiumDialogConfig('640px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(rejected => {
        if (rejected) {
          this.loadTasks();
        }
      });
  }

  isReportActionPending(taskId: number): boolean {
    return this.reportActionTaskId() === taskId;
  }

  getPriorityLabel = getTaskPriorityLabel;
  getPriorityClass = getTaskPriorityClass;
  getStatusLabel = getTaskStatusLabel;
  getStatusClass = getTaskStatusClass;

  getRiskLabel(level: RiskLevel): string {
    switch (level) {
      case RiskLevel.None:
        return 'Healthy';
      case RiskLevel.Low:
        return 'Low';
      case RiskLevel.Medium:
        return 'Medium';
      case RiskLevel.High:
        return 'High';
      case RiskLevel.Critical:
        return 'Critical';
      default:
        return 'All';
    }
  }

  trackByTaskId(_: number, task: TaskRisk): number {
    return task.id;
  }

  private loadFilterOptions(): void {
    this.engineerService
      .getAll({ pageNumber: 1, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => this.engineers.set(result.items));
  }

  private loadTasks(): void {
    this.isLoading.set(true);

    const request$ = this.taskService.getRiskTasks({
      search: this.searchTerm() || undefined,
      projectId: this.isAdmin() ? this.filterProjectId() ?? undefined : undefined,
      engineerId: this.isAdmin() ? this.filterEngineerId() ?? undefined : undefined,
      status: this.filterStatus() ?? undefined,
      priority: this.filterPriority() ?? undefined,
      riskLevel: this.filterRiskLevel(),
      sortBy: this.sortBy(),
      descending: this.sortDirection() === 'desc',
      pageNumber: 1,
      pageSize: 100
    });

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.tasks.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  private updateQueryParams(params: Record<string, string | number | null>): void {
    const current = this.route.snapshot.queryParamMap;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: current.get('search'),
        projectId: current.get('projectId'),
        engineerId: current.get('engineerId'),
        status: current.get('status'),
        priority: current.get('priority'),
        riskLevel: current.get('riskLevel'),
        sortBy: current.get('sortBy') ?? 'risk',
        sortDir: current.get('sortDir') ?? 'desc',
        ...params
      }
    });
  }

  private toNullableNumber(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : parsed;
  }

  private toNullableStatus(value: string | null): TaskStatus | null {
    if (value === null || value === '') {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : (parsed as TaskStatus);
  }

  private toNullablePriority(value: string | null): TaskPriority | null {
    if (value === null || value === '') {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : (parsed as TaskPriority);
  }

  private toNullableRiskLevel(value: string | null): RiskLevel | null {
    if (value === null || value === '') {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : (parsed as RiskLevel);
  }

  private toSortBy(value: string | null): 'risk' | 'overdue' | 'deadline' | 'startdate' | 'duedate' {
    if (value === 'overdue' || value === 'deadline' || value === 'startdate' || value === 'duedate') {
      return value;
    }
    return 'risk';
  }

  private toSortDirection(value: string | null): 'asc' | 'desc' {
    return value === 'asc' ? 'asc' : 'desc';
  }
}
