import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { of, switchMap } from 'rxjs';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ScheduleImpactAnalysis } from '../../../core/models/deadline-extension.model';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { ReviewDeadlineExtensionDialogData } from './review-deadline-extension-dialog-data';
import { ScheduleImpactAnalysisDialogComponent } from './schedule-impact-analysis-dialog.component';
import { ScheduleImpactAnalysisDialogResult } from './schedule-impact-analysis-dialog-data';

@Component({
  selector: 'app-review-deadline-extension-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './review-deadline-extension-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReviewDeadlineExtensionDialogComponent {
  private readonly service = inject(DeadlineExtensionService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject(MatDialogRef<ReviewDeadlineExtensionDialogComponent>);
  readonly data = inject<ReviewDeadlineExtensionDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);

  readonly commentControl = new FormControl('', {
    nonNullable: true,
    validators:
      this.data.mode === 'reject'
        ? [Validators.required, Validators.maxLength(2000)]
        : [Validators.maxLength(2000)]
  });

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('640px', 'auto');
  }

  confirm(): void {
    if (this.commentControl.invalid || this.isSubmitting()) {
      this.commentControl.markAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const adminComment = this.commentControl.value.trim() || null;
    const request = this.data.request;

    if (this.data.mode === 'approve' && request.requestType === 'Task' && request.taskId) {
      this.approveTaskWithImpact(request.taskId, request.id, request.requestedDeadline, request.reason, adminComment);
      return;
    }

    const body = { adminComment, confirmProjectExtension: false };
    const action$ =
      this.data.mode === 'approve'
        ? this.service.approveProjectRequest(request.id, body)
        : request.requestType === 'Task'
          ? this.service.rejectTaskRequest(request.id, body)
          : this.service.rejectProjectRequest(request.id, body);

    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.success(
          this.data.mode === 'approve' ? 'Request approved.' : 'Request rejected.'
        );
        this.dialogRef.close(true);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.notificationService.warning(this.extractError(error));
      }
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  private approveTaskWithImpact(
    taskId: number,
    requestId: number,
    requestedDeadline: string,
    reason: string,
    adminComment: string | null
  ): void {
    this.service
      .analyzeTaskDeadlineExtension(taskId, {
        newDueDate: requestedDeadline,
        reason: reason.length >= 10 ? reason : `${reason} (approved request)`.padEnd(10, '.')
      })
      .pipe(
        switchMap((analysis: ScheduleImpactAnalysis) => {
          return this.dialog
            .open(ScheduleImpactAnalysisDialogComponent, {
              ...premiumDialogConfig('920px'),
              data: { analysis }
            })
            .afterClosed()
            .pipe(
              switchMap((result: ScheduleImpactAnalysisDialogResult | undefined) => {
                if (!result?.confirmed) {
                  return of(null);
                }

                return this.service.approveTaskRequest(requestId, {
                  adminComment,
                  confirmProjectExtension: result.confirmProjectExtension
                });
              })
            );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          if (!result) {
            this.isSubmitting.set(false);
            return;
          }

          this.notificationService.success('Request approved.');
          this.dialogRef.close(true);
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          this.notificationService.warning(this.extractError(error));
        }
      });
  }

  private extractError(error: HttpErrorResponse): string {
    if (typeof error.error === 'object' && error.error && 'message' in error.error) {
      return String(error.error.message);
    }
    return 'Unable to review request.';
  }
}
