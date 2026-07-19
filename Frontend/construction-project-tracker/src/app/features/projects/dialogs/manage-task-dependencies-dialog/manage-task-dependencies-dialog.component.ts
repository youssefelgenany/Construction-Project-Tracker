import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { forkJoin } from 'rxjs';
import { TaskService } from '../../../../core/services/task.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TaskDependency, ValidPrerequisiteTask } from '../../../../core/models/task-dependency.model';
import { Task } from '../../../../core/models/task';
import { TaskStatus } from '../../../../core/enums/task-status';
import { getTaskStatusLabel } from '../../projects.utils';

export interface ManageTaskDependenciesDialogData {
  projectId: number;
  task: Task;
}

@Component({
  selector: 'app-manage-task-dependencies-dialog',
  standalone: true,
  imports: [
    DatePipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatIconModule,
    MatListModule
  ],
  templateUrl: './manage-task-dependencies-dialog.component.html',
  styleUrl: './manage-task-dependencies-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageTaskDependenciesDialogComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<ManageTaskDependenciesDialogComponent, boolean>);
  readonly data = inject<ManageTaskDependenciesDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly dependencies = signal<TaskDependency[]>([]);
  readonly candidates = signal<ValidPrerequisiteTask[]>([]);
  readonly selectedPrerequisiteId = signal<number | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  private changed = false;

  ngOnInit(): void {
    this.reload();
  }

  getStatusLabel(status: number): string {
    return getTaskStatusLabel(status as TaskStatus);
  }

  addDependency(): void {
    const dependsOnTaskId = this.selectedPrerequisiteId();
    if (!dependsOnTaskId || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.taskService
      .addDependency(this.data.task.id, { dependsOnTaskId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: dependency => {
          this.changed = true;
          this.dependencies.update(items => [...items, dependency]);
          this.selectedPrerequisiteId.set(null);
          this.isSaving.set(false);
          this.notificationService.success('Dependency added.');
          this.reloadCandidates();
        },
        error: () => this.isSaving.set(false)
      });
  }

  removeDependency(dependsOnTaskId: number): void {
    if (this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.taskService
      .removeDependency(this.data.task.id, dependsOnTaskId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.changed = true;
          this.dependencies.update(items =>
            items.filter(item => item.dependsOnTaskId !== dependsOnTaskId)
          );
          this.isSaving.set(false);
          this.notificationService.success('Dependency removed.');
          this.reloadCandidates();
        },
        error: () => this.isSaving.set(false)
      });
  }

  close(): void {
    this.dialogRef.close(this.changed);
  }

  private reload(): void {
    forkJoin({
      dependencies: this.taskService.getDependencies(this.data.task.id),
      candidates: this.taskService.getValidPrerequisites(this.data.task.id)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ dependencies, candidates }) => {
          this.dependencies.set(dependencies);
          this.candidates.set(candidates);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }

  private reloadCandidates(): void {
    this.taskService
      .getValidPrerequisites(this.data.task.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: candidates => this.candidates.set(candidates)
      });
  }
}
