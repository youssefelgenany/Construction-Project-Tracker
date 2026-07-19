import { ChangeDetectionStrategy, Component, inject, OnDestroy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DocumentPreviewDialogData } from '../document-preview-dialog-data';

@Component({
  selector: 'app-document-preview-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './document-preview-dialog.component.html',
  styleUrl: './document-preview-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentPreviewDialogComponent implements OnDestroy {
  readonly data = inject<DocumentPreviewDialogData>(MAT_DIALOG_DATA);

  ngOnDestroy(): void {
    URL.revokeObjectURL(this.data.blobUrl);
  }
}
