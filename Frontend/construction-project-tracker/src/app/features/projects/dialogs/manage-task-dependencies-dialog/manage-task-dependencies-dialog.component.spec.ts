import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TaskPriority } from '../../../../core/enums/task-priority';
import { TaskStatus } from '../../../../core/enums/task-status';
import { ManageTaskDependenciesDialogComponent } from './manage-task-dependencies-dialog.component';

describe('ManageTaskDependenciesDialogComponent', () => {
  let component: ManageTaskDependenciesDialogComponent;
  let fixture: ComponentFixture<ManageTaskDependenciesDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageTaskDependenciesDialogComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimationsAsync(),
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } },
        {
          provide: MAT_DIALOG_DATA,
          useValue: {
            projectId: 1,
            task: {
              id: 2,
              projectId: 1,
              projectName: 'Test',
              title: 'Task B',
              description: '',
              priority: TaskPriority.Medium,
              completionPercentage: 0,
              status: TaskStatus.NotStarted,
              startDate: '2026-01-01',
              dueDate: '2026-01-31'
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ManageTaskDependenciesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
