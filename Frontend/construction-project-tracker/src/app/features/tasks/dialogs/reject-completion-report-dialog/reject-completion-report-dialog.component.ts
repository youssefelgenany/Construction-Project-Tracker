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
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TaskCompletionReportService } from '../../../../core/services/task-completion-report.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { RejectCompletionReportDialogData } from '../reject-completion-report-dialog-data';

@Component({
  selector: 'app-reject-completion-report-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './reject-completion-report-dialog.component.html',
  styleUrl: './reject-completion-report-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RejectCompletionReportDialogComponent {
  private readonly completionReportService = inject(TaskCompletionReportService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<RejectCompletionReportDialogComponent>);
  readonly data = inject<RejectCompletionReportDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly commentControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(1000)]
  });

  readonly isSubmitting = signal(false);

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('640px', 'auto');
  }

  reject(): void {
    if (this.commentControl.invalid || this.isSubmitting()) {
      this.commentControl.markAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.completionReportService
      .reject(this.data.taskId, { comment: this.commentControl.value.trim() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Completion report rejected.');
          this.dialogRef.close(true);
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting.set(false);

          const message =
            typeof error.error === 'object' && error.error && 'message' in error.error
              ? String(error.error.message)
              : 'Unable to reject completion report.';
          this.notificationService.warning(message);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
