import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSliderModule } from '@angular/material/slider';
import { TaskProgressLogService } from '../../../../core/services/task-progress-log.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { UpdateTaskProgressDialogData } from '../update-task-progress-dialog-data';

@Component({
  selector: 'app-update-task-progress-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSliderModule
  ],
  templateUrl: './update-task-progress-dialog.component.html',
  styleUrl: './update-task-progress-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpdateTaskProgressDialogComponent {
  private readonly progressLogService = inject(TaskProgressLogService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<UpdateTaskProgressDialogComponent>);
  readonly data = inject<UpdateTaskProgressDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSaving = signal(false);

  readonly minNewProgress = Math.min(this.data.currentProgress + 5, this.data.maxProgress);
  readonly initialNewProgress = Math.min(
    Math.max(this.minNewProgress, this.data.currentProgress + 5),
    this.data.maxProgress
  );

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('640px', 'auto');
  }

  readonly form = new FormGroup({
    newProgress: new FormControl(this.initialNewProgress, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(this.minNewProgress),
        Validators.max(this.data.maxProgress)
      ]
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]
    })
  });

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const { newProgress, description } = this.form.getRawValue();

    if (newProgress <= this.data.currentProgress) {
      this.notificationService.warning('New progress must be greater than current progress.');
      return;
    }

    this.isSaving.set(true);

    this.progressLogService
      .create(this.data.taskId, { newProgress, description: description.trim() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Task progress updated.');
          this.dialogRef.close(true);
        },
        error: (error: HttpErrorResponse) => {
          this.isSaving.set(false);
          const message =
            typeof error.error === 'object' && error.error && 'message' in error.error
              ? String(error.error.message)
              : 'Unable to update task progress.';
          this.notificationService.warning(message);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
