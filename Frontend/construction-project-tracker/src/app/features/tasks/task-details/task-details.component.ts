import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';
import { TaskStatus } from '../../../core/enums/task-status';
import { TaskService } from '../../../core/services/task.service';
import { TaskProgressLogService } from '../../../core/services/task-progress-log.service';
import { TaskCompletionReportService } from '../../../core/services/task-completion-report.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TaskDetails } from '../../../core/models/task';
import { TaskProgressLog } from '../../../core/models/task-progress-log';
import { BackLinkComponent } from '../../../shared/components/back-link/back-link.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import {
  ENGINEER_MAX_MANUAL_PROGRESS,
  getTaskPriorityClass,
  getTaskPriorityLabel,
  getTaskStatusClass,
  getTaskStatusLabel
} from '../../projects/projects.utils';
import { SubmitCompletionReportDialogComponent } from '../dialogs/submit-completion-report-dialog/submit-completion-report-dialog.component';
import { SubmitCompletionReportDialogData } from '../dialogs/submit-completion-report-dialog-data';
import { UpdateTaskProgressDialogComponent } from '../dialogs/update-task-progress-dialog/update-task-progress-dialog.component';
import { UpdateTaskProgressDialogData } from '../dialogs/update-task-progress-dialog-data';
import { RejectCompletionReportDialogComponent } from '../dialogs/reject-completion-report-dialog/reject-completion-report-dialog.component';
import { RejectCompletionReportDialogData } from '../dialogs/reject-completion-report-dialog-data';
import { TaskProgressHistoryComponent } from '../components/task-progress-history/task-progress-history.component';
import { TaskDialogComponent } from '../../projects/dialogs/task-dialog/task-dialog.component';
import { TaskDialogData } from '../../projects/dialogs/task-dialog-data';
import {
  canPreviewCompletionReport,
  formatFileSize,
  getDocumentPreviewType,
  triggerBlobDownload
} from '../completion-report.utils';
import { DocumentPreviewDialogComponent } from '../../projects/dialogs/document-preview-dialog/document-preview-dialog.component';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { DocumentPreviewDialogData } from '../../projects/dialogs/document-preview-dialog-data';
import { ConfirmationDialogComponent } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import {
  DeadlineExtensionRequest,
  ExtensionRequestStatus
} from '../../../core/models/deadline-extension.model';
import { RequestDeadlineExtensionDialogComponent } from '../../deadline-extensions/dialogs/request-deadline-extension-dialog.component';
import { AdminExtendDeadlineDialogComponent } from '../../deadline-extensions/dialogs/admin-extend-deadline-dialog.component';

@Component({
  selector: 'app-task-details',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    BackLinkComponent,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    TaskProgressHistoryComponent
  ],
  templateUrl: './task-details.component.html',
  styleUrl: './task-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDetailsComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly progressLogService = inject(TaskProgressLogService);
  private readonly completionReportService = inject(TaskCompletionReportService);
  private readonly deadlineExtensionService = inject(DeadlineExtensionService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  private readonly progressHistory = viewChild(TaskProgressHistoryComponent);

  readonly isAdmin = this.authService.isAdmin;
  readonly task = signal<TaskDetails | null>(null);
  readonly progressLogs = signal<TaskProgressLog[]>([]);
  readonly isLoading = signal(true);
  readonly isRefreshingProgress = signal(false);
  readonly isReportActionPending = signal(false);
  readonly latestExtension = signal<DeadlineExtensionRequest | null>(null);
  readonly TaskStatus = TaskStatus;
  readonly ExtensionRequestStatus = ExtensionRequestStatus;

  readonly canRequestExtension = computed(() => {
    const details = this.task();
    if (!details || this.isAdmin() || details.status === TaskStatus.Completed) {
      return false;
    }
    return this.latestExtension()?.status !== ExtensionRequestStatus.Pending;
  });

  readonly canAdminExtendDeadline = computed(() => {
    const details = this.task();
    return !!details && this.isAdmin() && details.status !== TaskStatus.Completed;
  });

  readonly canUpdateProgress = computed(() => {
    const details = this.task();
    if (!details) {
      return false;
    }

    return (
      details.status !== TaskStatus.Completed &&
      details.status !== TaskStatus.PendingReview &&
      details.status !== TaskStatus.Blocked &&
      details.completionPercentage < ENGINEER_MAX_MANUAL_PROGRESS
    );
  });

  readonly canSubmitCompletionReport = computed(() => {
    const details = this.task();
    if (!details || this.isAdmin()) {
      return false;
    }

    return (
      details.status === TaskStatus.InProgress &&
      details.completionPercentage >= ENGINEER_MAX_MANUAL_PROGRESS
    );
  });

  readonly canApproveOrReject = computed(() => {
    const details = this.task();
    return !!details && this.isAdmin() && details.status === TaskStatus.PendingReview;
  });

  readonly commentEntries = computed(() => {
    const details = this.task();
    const entries: Array<{ author: string; body: string; createdAt: string; kind: string }> = [];

    const rejection =
      details?.completionReport?.rejectionReason ?? details?.completionReport?.rejectionComment;
    if (rejection) {
      entries.push({
        author: details?.completionReport?.rejectedBy || 'Admin Review',
        body: rejection,
        createdAt: details?.completionReport?.rejectedAt || details!.completionReport!.uploadedAt,
        kind: 'rejection'
      });
    }

    for (const log of this.progressLogs()) {
      if (log.description?.trim()) {
        entries.push({
          author: log.engineerName,
          body: log.description,
          createdAt: log.createdAt,
          kind: 'progress'
        });
      }
    }

    return entries;
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      void this.router.navigate(['/tasks']);
      return;
    }

    this.loadTask(id);
  }

  openRequestExtensionDialog(): void {
    const details = this.task();
    if (!details || !this.canRequestExtension()) {
      return;
    }

    this.dialog
      .open(
        RequestDeadlineExtensionDialogComponent,
        premiumDialogConfig('640px', {
          data: {
            target: 'task',
            entityId: details.id,
            entityTitle: details.title,
            currentDeadline: details.dueDate,
            maxDeadline: null,
            projectName: this.projectName(details)
          }
        })
      )
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadExtension(details.id);
        }
      });
  }

  openAdminExtendDeadlineDialog(): void {
    const details = this.task();
    if (!details || !this.canAdminExtendDeadline()) {
      return;
    }

    this.dialog
      .open(
        AdminExtendDeadlineDialogComponent,
        premiumDialogConfig('640px', {
          data: {
            target: 'task',
            entityId: details.id,
            entityTitle: details.title,
            currentDeadline: details.dueDate,
            maxDeadline: null
          }
        })
      )
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadTask(details.id);
        }
      });
  }

  openEditTaskDialog(): void {
    const details = this.task();
    if (!details || !this.isAdmin()) {
      return;
    }

    const data: TaskDialogData = {
      projectId: details.projectId,
      task: details
    };

    this.dialog
      .open(TaskDialogComponent, premiumDialogConfig('780px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadTask(details.id);
        }
      });
  }

  openUpdateProgressDialog(): void {
    const current = this.task();
    if (!current || !this.canUpdateProgress()) {
      return;
    }

    const data: UpdateTaskProgressDialogData = {
      taskId: current.id,
      taskTitle: current.title,
      currentProgress: current.completionPercentage,
      maxProgress: ENGINEER_MAX_MANUAL_PROGRESS
    };

    this.dialog
      .open(UpdateTaskProgressDialogComponent, premiumDialogConfig('640px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.refreshTaskProgress(current.id);
        }
      });
  }

  openSubmitReportDialog(): void {
    const current = this.task();
    if (!current || !this.canSubmitCompletionReport()) {
      return;
    }

    const data: SubmitCompletionReportDialogData = {
      taskId: current.id,
      taskTitle: current.title
    };

    this.dialog
      .open(SubmitCompletionReportDialogComponent, premiumDialogConfig('640px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(submitted => {
        if (submitted) {
          this.loadTask(current.id);
        }
      });
  }

  viewCompletionReport(): void {
    const details = this.task();
    const report = details?.completionReport;
    if (!details || !report || this.isReportActionPending()) {
      return;
    }

    if (!canPreviewCompletionReport(report.extension)) {
      this.notificationService.info('Preview is not available for this file type. Download instead.');
      this.downloadCompletionReport();
      return;
    }

    this.isReportActionPending.set(true);

    this.completionReportService
      .download(details.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.isReportActionPending.set(false);
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
        error: () => this.isReportActionPending.set(false)
      });
  }

  downloadCompletionReport(): void {
    const details = this.task();
    const report = details?.completionReport;
    if (!details || !report) {
      return;
    }

    this.completionReportService
      .download(details.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => triggerBlobDownload(blob, report.originalFileName)
      });
  }

  approveCompletionReport(): void {
    const details = this.task();
    if (!details || !this.canApproveOrReject() || this.isReportActionPending()) {
      return;
    }

    const dialogData: ConfirmationDialogData = {
      title: 'Approve Completion',
      message: `Approve the completion report for "${details.title}" and mark the task as completed?`,
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

        this.isReportActionPending.set(true);
        this.completionReportService.approve(details.id).subscribe({
          next: () => {
            this.isReportActionPending.set(false);
            this.notificationService.success('Completion report approved.');
            this.loadTask(details.id);
          },
          error: () => this.isReportActionPending.set(false)
        });
      });
  }

  rejectCompletionReport(): void {
    const details = this.task();
    if (!details || !this.canApproveOrReject()) {
      return;
    }

    const data: RejectCompletionReportDialogData = {
      taskId: details.id,
      taskTitle: details.title
    };

    this.dialog
      .open(RejectCompletionReportDialogComponent, premiumDialogConfig('640px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(rejected => {
        if (rejected) {
          this.loadTask(details.id);
        }
      });
  }

  formatReportSize(bytes: number): string {
    return formatFileSize(bytes);
  }

  getPriorityLabel = getTaskPriorityLabel;
  getPriorityClass = getTaskPriorityClass;
  getStatusLabel = getTaskStatusLabel;
  getStatusClass = getTaskStatusClass;

  isBlocked(details: TaskDetails): boolean {
    return details.status === TaskStatus.Blocked;
  }

  projectName(task: TaskDetails): string {
    return task.projectName || task.project?.name || '—';
  }

  projectLink(task: TaskDetails): string | null {
    return task.projectId ? `/projects/${task.projectId}` : null;
  }

  private loadTask(id: number): void {
    this.isLoading.set(true);

    forkJoin({
      task: this.taskService.getById(id),
      logs: this.progressLogService.getByTaskId(id)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ task, logs }) => {
          this.task.set(this.normalizeTask(task));
          this.progressLogs.set(logs);
          this.loadExtension(id);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          void this.router.navigate(['/tasks']);
        }
      });
  }

  private loadExtension(taskId: number): void {
    this.deadlineExtensionService
      .getLatestTaskRequest(taskId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: request => this.latestExtension.set(request),
        error: () => this.latestExtension.set(null)
      });
  }

  private refreshTaskProgress(id: number): void {
    this.isRefreshingProgress.set(true);

    forkJoin({
      task: this.taskService.getById(id),
      logs: this.progressLogService.getByTaskId(id)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ task, logs }) => {
          this.task.set(this.normalizeTask(task));
          this.progressLogs.set(logs);
          this.isRefreshingProgress.set(false);
          this.progressHistory()?.reload();
        },
        error: () => this.isRefreshingProgress.set(false)
      });
  }

  private normalizeTask(task: TaskDetails): TaskDetails {
    return {
      ...task,
      projectName: task.projectName || task.project?.name || ''
    };
  }
}
