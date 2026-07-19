import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { switchMap, of } from 'rxjs';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ScheduleImpactAnalysis } from '../../../core/models/deadline-extension.model';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { AdminExtendDeadlineDialogData } from './admin-extend-deadline-dialog-data';
import { ScheduleImpactAnalysisDialogComponent } from './schedule-impact-analysis-dialog.component';
import { ScheduleImpactAnalysisDialogResult } from './schedule-impact-analysis-dialog-data';

@Component({
  selector: 'app-admin-extend-deadline-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './admin-extend-deadline-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminExtendDeadlineDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(DeadlineExtensionService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject(MatDialogRef<AdminExtendDeadlineDialogComponent>);
  readonly data = inject<AdminExtendDeadlineDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    newDeadline: ['', Validators.required],
    reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]]
  });

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('640px', 'auto');
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { newDeadline, reason } = this.form.getRawValue();
    const trimmedReason = reason.trim();
    this.isSubmitting.set(true);

    if (this.data.target === 'project') {
      this.service
        .extendProjectDeadline(this.data.entityId, {
          newEndDate: newDeadline,
          reason: trimmedReason
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.notificationService.success('Deadline extended successfully.');
            this.dialogRef.close(true);
          },
          error: (error: HttpErrorResponse) => {
            this.isSubmitting.set(false);
            this.notificationService.warning(this.extractError(error));
          }
        });
      return;
    }

    this.service
      .analyzeTaskDeadlineExtension(this.data.entityId, {
        newDueDate: newDeadline,
        reason: trimmedReason
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

                return this.service.applyTaskDeadlineExtension(this.data.entityId, {
                  newDueDate: newDeadline,
                  reason: trimmedReason,
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

          const message =
            result.shiftedTaskCount > 0
              ? `Deadline extended. ${result.shiftedTaskCount} dependent task(s) rescheduled.`
              : 'Deadline extended successfully.';
          this.notificationService.success(message);
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

  private extractError(error: HttpErrorResponse): string {
    if (typeof error.error === 'object' && error.error && 'message' in error.error) {
      return String(error.error.message);
    }
    return 'Unable to extend deadline.';
  }
}
