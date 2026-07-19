import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';
import { ScheduleService } from '../../../../core/services/schedule.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ProjectTimeline } from '../../../../core/models/project-timeline.model';
import { CriticalPathTask } from '../../../../core/models/critical-path-task.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ProjectGanttChartComponent } from '../project-gantt-chart/project-gantt-chart.component';
import { ManageTaskDependenciesDialogComponent } from '../../dialogs/manage-task-dependencies-dialog/manage-task-dependencies-dialog.component';
import { TaskService } from '../../../../core/services/task.service';
import { Task } from '../../../../core/models/task';
import { getTaskStatusLabel } from '../../projects.utils';

@Component({
  selector: 'app-project-timeline-tab',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatCardModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ProjectGanttChartComponent
  ],
  templateUrl: './project-timeline-tab.component.html',
  styleUrl: './project-timeline-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectTimelineTabComponent implements OnInit {
  private readonly scheduleService = inject(ScheduleService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly projectId = input.required<number>();

  readonly isAdmin = this.authService.isAdmin;
  readonly isLoading = signal(true);
  readonly timeline = signal<ProjectTimeline | null>(null);
  readonly criticalPath = signal<CriticalPathTask[]>([]);
  readonly projectTasks = signal<Task[]>([]);

  readonly criticalColumns = ['order', 'title', 'durationDays', 'slackDays', 'status'] as const;
  readonly getStatusLabel = getTaskStatusLabel;

  ngOnInit(): void {
    this.loadTimeline();
  }

  refresh(): void {
    this.loadTimeline();
  }

  openDependencyDialog(task: Task): void {
    this.dialog
      .open(ManageTaskDependenciesDialogComponent, {
        width: '560px',
        data: { projectId: this.projectId(), task }
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(changed => {
        if (changed) {
          this.loadTimeline();
        }
      });
  }

  private loadTimeline(): void {
    this.isLoading.set(true);
    const projectId = this.projectId();

    if (this.isAdmin()) {
      forkJoin({
        timeline: this.scheduleService.getProjectTimeline(projectId),
        criticalPath: this.scheduleService.getCriticalPath(projectId),
        tasks: this.taskService.getProjectTasks(projectId, 100)
      })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: result => {
            this.timeline.set(result.timeline);
            this.criticalPath.set(result.criticalPath);
            this.projectTasks.set(result.tasks.items);
            this.isLoading.set(false);
          },
          error: () => this.isLoading.set(false)
        });
      return;
    }

    forkJoin({
      timeline: this.scheduleService.getProjectTimeline(projectId),
      criticalPath: this.scheduleService.getCriticalPath(projectId)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.timeline.set(result.timeline);
          this.criticalPath.set(result.criticalPath);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }
}
