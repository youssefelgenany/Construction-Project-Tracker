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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { AssignmentService } from '../../../../core/services/assignment.service';
import { ProjectService } from '../../../../core/services/project.service';
import { TaskService } from '../../../../core/services/task.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Project } from '../../../../core/models/project';
import { ProjectAssignedEngineer } from '../../../../core/models/project-assigned-engineer';
import { TaskFormComponent } from '../../components/task-form/task-form.component';
import { TaskDialogData } from '../task-dialog-data';
import { TaskFormValue } from '../../models/task-form-value';
import { toUpdateTaskPayload } from '../../projects.utils';

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    TaskFormComponent
  ],
  templateUrl: './task-dialog.component.html',
  styleUrl: './task-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDialogComponent implements OnInit {
  private readonly assignmentService = inject(AssignmentService);
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<TaskDialogComponent>);
  private readonly data = inject<TaskDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly projects = signal<Project[]>([]);
  readonly engineers = signal<ProjectAssignedEngineer[]>([]);
  readonly selectedProjectId = signal<number | null>(this.resolveInitialProjectId());
  readonly projectStartDate = signal<string | null>(null);
  readonly projectEndDate = signal<string | null>(null);
  readonly isLoadingProjects = signal(false);
  readonly isLoadingEngineers = signal(false);
  readonly isSubmitting = signal(false);

  readonly isEditMode = !!this.data.task;

  constructor() {
    this.dialogRef.addPanelClass('ds-premium-dialog-panel');
    this.dialogRef.updateSize('780px', 'auto');
  }

  ngOnInit(): void {
    if (this.isEditMode) {
      this.loadEngineers(this.resolveInitialProjectId());
      this.loadProjectBounds(this.resolveInitialProjectId());
      return;
    }

    if (this.data.projectId) {
      this.loadEngineers(this.data.projectId);
      this.loadProjectBounds(this.data.projectId);
      return;
    }

    this.isLoadingProjects.set(true);

    this.projectService
      .getAll({ pageNumber: 1, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.projects.set(result.items);
          this.isLoadingProjects.set(false);
        },
        error: () => this.isLoadingProjects.set(false)
      });
  }

  get dialogTitle(): string {
    return this.isEditMode ? 'Edit Task' : 'Create New Task';
  }

  get dialogSubtitle(): string {
    return this.isEditMode
      ? 'Update task details, schedule, and assignment settings.'
      : 'Add a new work item to this project.';
  }

  get initialValue(): TaskFormValue | null {
    const task = this.data.task;
    if (!task || task.assignedEngineerId == null) {
      return null;
    }

    return {
      title: task.title,
      description: task.description,
      assignedEngineerId: task.assignedEngineerId,
      priority: task.priority,
      startDate: task.startDate,
      dueDate: task.dueDate
    };
  }

  get showProjectPicker(): boolean {
    return !this.isEditMode && !this.data.projectId;
  }

  onProjectChange(projectId: number | null): void {
    this.selectedProjectId.set(projectId);
    this.engineers.set([]);
    this.projectStartDate.set(null);
    this.projectEndDate.set(null);

    if (!projectId) {
      return;
    }

    this.loadEngineers(projectId);
    this.loadProjectBounds(projectId);
  }

  onSubmit(value: TaskFormValue): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    if (this.isEditMode && this.data.task) {
      const payload = toUpdateTaskPayload(this.data.task, {
        title: value.title,
        description: value.description,
        priority: value.priority,
        startDate: value.startDate,
        dueDate: value.dueDate
      });

      this.taskService
        .update(this.data.task.id, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.notificationService.success('Task updated successfully.');
            this.dialogRef.close(true);
          },
          error: () => this.isSubmitting.set(false)
        });

      return;
    }

    const projectId = this.data.projectId ?? this.selectedProjectId();
    if (!projectId) {
      this.isSubmitting.set(false);
      return;
    }

    this.taskService
      .create({
        projectId,
        assignedEngineerId: value.assignedEngineerId,
        title: value.title,
        description: value.description,
        priority: value.priority,
        startDate: value.startDate,
        dueDate: value.dueDate
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Task created successfully.');
          this.dialogRef.close(true);
        },
        error: () => this.isSubmitting.set(false)
      });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  private resolveInitialProjectId(): number | null {
    if (this.data.task?.projectId) {
      return this.data.task.projectId;
    }

    return this.data.projectId ?? null;
  }

  private loadEngineers(projectId: number | null): void {
    if (!projectId) {
      return;
    }

    this.isLoadingEngineers.set(true);

    this.assignmentService
      .getByProject(projectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: engineers => {
          this.engineers.set(engineers);
          this.isLoadingEngineers.set(false);
        },
        error: () => this.isLoadingEngineers.set(false)
      });
  }

  private loadProjectBounds(projectId: number | null): void {
    if (!projectId) {
      return;
    }

    const fromList = this.projects().find(p => p.id === projectId);
    if (fromList) {
      this.projectStartDate.set(fromList.startDate);
      this.projectEndDate.set(fromList.endDate);
      return;
    }

    this.projectService
      .getById(projectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: project => {
          this.projectStartDate.set(project.startDate);
          this.projectEndDate.set(project.endDate);
        }
      });
  }
}
