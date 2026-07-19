import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse, HttpEventType } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DocumentCategory } from '../../../../core/enums/document-category';
import { DocumentService } from '../../../../core/services/document.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  DOCUMENT_ACCEPT_ATTRIBUTE,
  DOCUMENT_CATEGORY_OPTIONS,
  DOCUMENT_MAX_FILE_SIZE_BYTES,
  formatFileSize,
  isAllowedDocumentExtension
} from '../../documents.utils';
import { UploadDocumentDialogData } from '../upload-document-dialog-data';

@Component({
  selector: 'app-upload-document-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatIconModule,
    MatProgressBarModule
  ],
  templateUrl: './upload-document-dialog.component.html',
  styleUrl: './upload-document-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UploadDocumentDialogComponent {
  private readonly documentService = inject(DocumentService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<UploadDocumentDialogComponent>);
  private readonly data = inject<UploadDocumentDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly categoryControl = new FormControl<DocumentCategory>(DocumentCategory.Other, {
    nonNullable: true,
    validators: Validators.required
  });
  readonly categoryOptions = DOCUMENT_CATEGORY_OPTIONS;
  readonly acceptAttribute = DOCUMENT_ACCEPT_ATTRIBUTE;
  readonly maxFileSizeLabel = formatFileSize(DOCUMENT_MAX_FILE_SIZE_BYTES);

  readonly selectedFile = signal<File | null>(null);
  readonly validationError = signal<string | null>(null);
  readonly isDragging = signal(false);
  readonly isUploading = signal(false);
  readonly uploadProgress = signal(0);

  readonly canUpload = computed(
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
    this.uploadProgress.set(0);
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file || this.isUploading() || this.validationError()) {
      return;
    }

    this.isUploading.set(true);
    this.uploadProgress.set(0);

    this.documentService
      .upload(this.data.projectId, file, this.categoryControl.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: event => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.uploadProgress.set(Math.round((event.loaded / event.total) * 100));
          }

          if (event.type === HttpEventType.Response) {
            this.notificationService.success('Document uploaded successfully.');
            this.dialogRef.close(true);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.isUploading.set(false);
          this.uploadProgress.set(0);

          if (error.status === 409) {
            const message =
              typeof error.error === 'object' && error.error && 'message' in error.error
                ? String(error.error.message)
                : 'Unable to upload document.';
            this.notificationService.warning(message);
          }
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

    if (!isAllowedDocumentExtension(extension)) {
      this.validationError.set(
        'File type is not allowed. Allowed types: PDF, DOC, DOCX, XLS, XLSX, PNG, JPG, JPEG.'
      );
      return;
    }

    if (file.size > DOCUMENT_MAX_FILE_SIZE_BYTES) {
      this.validationError.set(`File size must not exceed ${this.maxFileSizeLabel}.`);
      return;
    }

    this.validationError.set(null);
  }
}
