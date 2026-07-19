import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DocumentPreviewDialogComponent } from './document-preview-dialog.component';

describe('DocumentPreviewDialogComponent', () => {
  let component: DocumentPreviewDialogComponent;
  let fixture: ComponentFixture<DocumentPreviewDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentPreviewDialogComponent],
      providers: [
        provideAnimationsAsync(),
        {
          provide: MAT_DIALOG_DATA,
          useValue: { fileName: 'photo.png', blobUrl: 'blob:test' }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentPreviewDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
