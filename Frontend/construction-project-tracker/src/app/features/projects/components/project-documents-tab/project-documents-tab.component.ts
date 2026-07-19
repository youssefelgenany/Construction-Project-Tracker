import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DocumentService } from '../../../../core/services/document.service';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Document } from '../../../../core/models/document';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SearchBoxComponent } from '../../../../shared/components/search-box/search-box.component';
import { premiumDialogConfig } from '../../../../shared/dialogs/premium-dialog.config';
import { ConfirmationDialogComponent } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import {
  filterDocuments,
  formatFileSize,
  getDocumentPreviewType,
  triggerBlobDownload
} from '../../documents.utils';
import { UploadDocumentDialogComponent } from '../../dialogs/upload-document-dialog/upload-document-dialog.component';
import { UploadDocumentDialogData } from '../../dialogs/upload-document-dialog-data';
import { DocumentPreviewDialogComponent } from '../../dialogs/document-preview-dialog/document-preview-dialog.component';
import { DocumentPreviewDialogData } from '../../dialogs/document-preview-dialog-data';

@Component({
  selector: 'app-project-documents-tab',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    SearchBoxComponent
  ],
  templateUrl: './project-documents-tab.component.html',
  styleUrl: './project-documents-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDocumentsTabComponent implements OnInit {
  private readonly documentService = inject(DocumentService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<number>();
  readonly documentsChanged = output<void>();

  readonly isAdmin = this.authService.isAdmin;
  readonly documents = signal<Document[]>([]);
  readonly searchTerm = signal('');
  readonly isLoading = signal(false);
  readonly viewingDocumentId = signal<number | null>(null);
  readonly downloadingDocumentId = signal<number | null>(null);

  readonly filteredDocuments = computed(() =>
    filterDocuments(this.documents(), this.searchTerm())
  );

  readonly baseColumns = [
    'originalFileName',
    'category',
    'uploadedBy',
    'uploadDate',
    'fileSize',
    'actions'
  ] as const;

  visibleColumns(): string[] {
    return [...this.baseColumns];
  }

  formatSize(bytes: number): string {
    return formatFileSize(bytes);
  }

  ngOnInit(): void {
    this.loadDocuments();
  }

  onSearchChange(term: string): void {
    this.searchTerm.set(term);
  }

  openUploadDialog(): void {
    const data: UploadDocumentDialogData = { projectId: this.projectId() };

    this.dialog
      .open(UploadDocumentDialogComponent, premiumDialogConfig('640px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(uploaded => {
        if (uploaded) {
          this.loadDocuments();
          this.documentsChanged.emit();
        }
      });
  }

  viewDocument(document: Document): void {
    if (this.viewingDocumentId() === document.id) {
      return;
    }

    const previewType = getDocumentPreviewType(document.extension);
    this.viewingDocumentId.set(document.id);

    this.documentService
      .download(document.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.viewingDocumentId.set(null);

          if (previewType === 'image') {
            const blobUrl = URL.createObjectURL(blob);
            const data: DocumentPreviewDialogData = {
              fileName: document.originalFileName,
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

          triggerBlobDownload(blob, document.originalFileName);
        },
        error: () => this.viewingDocumentId.set(null)
      });
  }

  downloadDocument(document: Document): void {
    if (this.downloadingDocumentId() === document.id) {
      return;
    }

    this.downloadingDocumentId.set(document.id);

    this.documentService
      .download(document.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.downloadingDocumentId.set(null);
          triggerBlobDownload(blob, document.originalFileName);
        },
        error: () => this.downloadingDocumentId.set(null)
      });
  }

  deleteDocument(document: Document): void {
    const dialogData: ConfirmationDialogData = {
      title: 'Delete Document',
      message: `Delete "${document.originalFileName}"? This action cannot be undone.`,
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

        this.documentService.delete(document.id).subscribe({
          next: () => {
            this.notificationService.success('Document deleted successfully.');
            this.loadDocuments();
            this.documentsChanged.emit();
          }
        });
      });
  }

  isViewing(documentId: number): boolean {
    return this.viewingDocumentId() === documentId;
  }

  isDownloading(documentId: number): boolean {
    return this.downloadingDocumentId() === documentId;
  }

  private loadDocuments(): void {
    this.isLoading.set(true);

    this.documentService
      .getByProject(this.projectId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: documents => {
          this.documents.set(documents);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        }
      });
  }
}
