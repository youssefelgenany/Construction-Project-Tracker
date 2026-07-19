import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TaskCompletionReportService } from '../../../../core/services/task-completion-report.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  COMPLETION_REPORT_ACCEPT_ATTRIBUTE,
  COMPLETION_REPORT_MAX_FILE_SIZE_BYTES,
  formatFileSize,
  isAllowedCompletionReportExtension
} from '../../completion-report.utils';
import { SubmitCompletionReportDialogData } from '../submit-completion-report-dialog-data';

@Component({
  selector: 'app-submit-completion-report-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './submit-completion-report-dialog.component.html',
  styleUrl: './submit-completion-report-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SubmitCompletionReportDialogComponent {
  private readonly completionReportService = inject(TaskCompletionReportService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<SubmitCompletionReportDialogComponent>);
  readonly data = inject<SubmitCompletionReportDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly acceptAttribute = COMPLETION_REPORT_ACCEPT_ATTRIBUTE;
  readonly maxFileSizeLabel = formatFileSize(COMPLETION_REPORT_MAX_FILE_SIZE_BYTES);

  readonly selectedFile = signal<File | null>(null);
  readonly validationError = signal<string | null>(null);
  readonly isDragging = signal(false);
  readonly isUploading = signal(false);

  readonly canSubmit = computed(
    () => !!this.selectedFile() && !this.validationError() && !this.isUploading()
  );

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('640px', 'auto');
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);

    const file = event.dataTransfer?.files.item(0);
    if (file) {
      this.setSelectedFile(file);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    if (file) {
      this.setSelectedFile(file);
    }
    input.value = '';
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.validationError.set(null);
  }

  submit(): void {
    const file = this.selectedFile();
    if (!file || this.isUploading() || this.validationError()) {
      return;
    }

    this.isUploading.set(true);

    this.completionReportService
      .upload(this.data.taskId, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Completion report submitted for review.');
          this.dialogRef.close(true);
        },
        error: (error: HttpErrorResponse) => {
          this.isUploading.set(false);

          const message =
            typeof error.error === 'object' && error.error && 'message' in error.error
              ? String(error.error.message)
              : 'Unable to submit completion report.';
          this.notificationService.warning(message);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  private setSelectedFile(file: File): void {
    this.selectedFile.set(file);
    this.validateFile(file);
  }

  private validateFile(file: File): void {
    const extension = file.name.includes('.') ? `.${file.name.split('.').pop()}` : '';

    if (!isAllowedCompletionReportExtension(extension)) {
      this.validationError.set(
        'File type is not allowed. Allowed types: PDF, DOC, DOCX, XLS, XLSX, JPG, PNG, DWG, ZIP.'
      );
      return;
    }

    if (file.size > COMPLETION_REPORT_MAX_FILE_SIZE_BYTES) {
      this.validationError.set(`File size must not exceed ${this.maxFileSizeLabel}.`);
      return;
    }

    this.validationError.set(null);
  }
}
