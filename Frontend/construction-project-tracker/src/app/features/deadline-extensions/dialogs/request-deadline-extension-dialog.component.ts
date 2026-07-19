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
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RequestDeadlineExtensionDialogData } from './request-deadline-extension-dialog-data';

@Component({
  selector: 'app-request-deadline-extension-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './request-deadline-extension-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RequestDeadlineExtensionDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(DeadlineExtensionService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<RequestDeadlineExtensionDialogComponent>);
  readonly data = inject<RequestDeadlineExtensionDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    requestedDate: ['', Validators.required],
    reason: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(2000)]]
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

    const { requestedDate, reason } = this.form.getRawValue();
    this.isSubmitting.set(true);

    const request$ =
      this.data.target === 'task'
        ? this.service.createTaskRequest(this.data.entityId, {
            requestedDueDate: requestedDate,
            reason: reason.trim()
          })
        : this.service.createProjectRequest(this.data.entityId, {
            requestedEndDate: requestedDate,
            reason: reason.trim()
          });

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.success('Deadline extension request submitted.');
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
    return 'Unable to submit deadline extension request.';
  }
}
