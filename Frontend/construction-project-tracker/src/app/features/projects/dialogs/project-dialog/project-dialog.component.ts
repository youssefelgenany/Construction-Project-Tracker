import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProjectService } from '../../../../core/services/project.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { CreateProject } from '../../../../core/models/project';
import { ProjectFormComponent } from '../../components/project-form/project-form.component';
import { ProjectDialogData } from '../project-dialog-data';

@Component({
  selector: 'app-project-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    ProjectFormComponent
  ],
  templateUrl: './project-dialog.component.html',
  styleUrl: './project-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDialogComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<ProjectDialogComponent>);
  private readonly data = inject<ProjectDialogData>(MAT_DIALOG_DATA, { optional: true }) ?? {};
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(!!this.data.projectId);
  readonly isSubmitting = signal(false);
  readonly initialValue = signal<CreateProject | null>(null);

  readonly isEditMode = !!this.data.projectId;

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('900px', 'auto');
  }

  ngOnInit(): void {
    if (this.isEditMode && this.data.projectId) {
      this.loadProject(this.data.projectId);
    }
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Project' : 'Create New Project';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? 'Update the project overview, budget, and schedule.'
      : 'Set up the core project details and planning window.';
  }

  onSubmit(project: CreateProject): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    if (this.isEditMode && this.data.projectId) {
      this.projectService
        .update(this.data.projectId, project)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.notificationService.success('Project updated successfully.');
            this.dialogRef.close(true);
          },
          error: () => this.isSubmitting.set(false)
        });

      return;
    }

    this.projectService
      .create(project)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Project created successfully.');
          this.dialogRef.close(true);
        },
        error: () => this.isSubmitting.set(false)
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  private loadProject(id: number): void {
    this.projectService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: project => {
          this.initialValue.set({
            name: project.name,
            description: project.description,
            budget: project.budget,
            startDate: project.startDate,
            endDate: project.endDate
          });
          this.isLoading.set(false);
        },
        error: () => this.dialogRef.close(false)
      });
  }
}
