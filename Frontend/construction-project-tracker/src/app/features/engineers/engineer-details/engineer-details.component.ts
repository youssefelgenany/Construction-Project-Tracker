import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { EngineerService } from '../../../core/services/engineer.service';
import { AssignmentService } from '../../../core/services/assignment.service';
import { TaskService } from '../../../core/services/task.service';
import { TaskProgressLogService } from '../../../core/services/task-progress-log.service';
import { EngineerDetails } from '../../../core/models/engineer';
import { EngineerPerformanceDetails } from '../../../core/models/engineer-performance.model';
import { Project } from '../../../core/models/project';
import { Task } from '../../../core/models/task';
import { TaskProgressLog } from '../../../core/models/task-progress-log';
import { BackLinkComponent } from '../../../shared/components/back-link/back-link.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PerformanceBadgeComponent } from '../../../shared/components/performance-badge/performance-badge.component';
import { EngineerStatusChipComponent } from '../components/engineer-status-chip/engineer-status-chip.component';
import { ProjectStatusChipComponent } from '../../projects/components/project-status-chip/project-status-chip.component';
import { TaskStatus } from '../../../core/enums/task-status';
import {
  getTaskStatusClass,
  getTaskStatusLabel
} from '../../projects/projects.utils';
import { getEngineerInitials } from '../engineers.utils';

type HistoryEntry = {
  id: string;
  title: string;
  body: string;
  createdAt: string;
  kind: 'progress' | 'report' | 'completed' | 'comment';
};

@Component({
  selector: 'app-engineer-details',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule,
    BaseChartDirective,
    BackLinkComponent,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    PerformanceBadgeComponent,
    EngineerStatusChipComponent,
    ProjectStatusChipComponent
  ],
  templateUrl: './engineer-details.component.html',
  styleUrl: './engineer-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerDetailsComponent implements OnInit {
  private readonly engineerService = inject(EngineerService);
  private readonly assignmentService = inject(AssignmentService);
  private readonly taskService = inject(TaskService);
  private readonly progressLogService = inject(TaskProgressLogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(true);
  readonly engineer = signal<EngineerDetails | null>(null);
  readonly assignedProjects = signal<Project[]>([]);
  readonly assignedTasks = signal<Task[]>([]);
  readonly performance = signal<EngineerPerformanceDetails | null>(null);
  readonly progressLogs = signal<TaskProgressLog[]>([]);
  selectedTabIndex = 0;

  readonly projectColumns = ['name', 'status', 'progressPercentage', 'actions'] as const;
  readonly taskColumns = ['title', 'projectName', 'status', 'completion', 'dueDate', 'actions'] as const;
  readonly completedTaskColumns = ['taskTitle', 'projectName', 'completedAt', 'daysEarlyLate', 'durationDays', 'actions'] as const;
  readonly reportColumns = ['taskTitle', 'projectName', 'uploadedAt', 'reviewStatus'] as const;

  readonly TaskStatus = TaskStatus;
  readonly getInitials = getEngineerInitials;
  readonly getStatusLabel = getTaskStatusLabel;
  readonly getStatusClass = getTaskStatusClass;

  readonly completedTasks = computed(() =>
    this.assignedTasks().filter(task => task.status === TaskStatus.Completed)
  );

  readonly activeTasks = computed(() =>
    this.assignedTasks().filter(task => task.status !== TaskStatus.Completed)
  );

  readonly overdueTasksCount = computed(() => {
    const now = Date.now();
    return this.assignedTasks().filter(
      task =>
        task.status !== TaskStatus.Completed &&
        !!task.dueDate &&
        new Date(task.dueDate).getTime() < now
    ).length;
  });

  readonly averageCompletionPercent = computed(() => {
    const tasks = this.assignedTasks();
    if (tasks.length === 0) {
      return 0;
    }
    const total = tasks.reduce((sum, task) => sum + (task.completionPercentage ?? 0), 0);
    return total / tasks.length;
  });

  readonly historyEntries = computed(() => {
    const entries: HistoryEntry[] = [];
    const performance = this.performance();

    for (const log of this.progressLogs()) {
      entries.push({
        id: `log-${log.id}`,
        title: `Progress · ${log.previousProgress}% → ${log.newProgress}%`,
        body: log.description || 'Progress update logged.',
        createdAt: log.createdAt,
        kind: 'progress'
      });
    }

    for (const report of performance?.recentCompletionReports ?? []) {
      entries.push({
        id: `report-${report.reportId}`,
        title: `Completion report · ${report.taskTitle}`,
        body: `${report.originalFileName} · ${report.reviewStatus}`,
        createdAt: report.uploadedAt,
        kind: 'report'
      });
    }

    for (const task of performance?.recentCompletedTasks ?? []) {
      entries.push({
        id: `completed-${task.taskId}`,
        title: `Task completed · ${task.taskTitle}`,
        body: `${task.projectName} · ${task.finishedOnTime ? 'On time' : 'Late'} · ${task.daysEarlyLate} days`,
        createdAt: task.completedAt,
        kind: 'completed'
      });
    }

    return entries.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  });

  readonly commentEntries = computed(() =>
    this.progressLogs()
      .filter(log => !!log.description?.trim())
      .map(log => ({
        id: log.id,
        author: log.engineerName,
        body: log.description,
        createdAt: log.createdAt,
        progress: `${log.previousProgress}% → ${log.newProgress}%`
      }))
  );

  productivityChartData: ChartConfiguration<'line'>['data'] = { labels: [], datasets: [] };
  productivityChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'bottom' }
    },
    scales: {
      y: {
        beginAtZero: true,
        max: 100,
        ticks: { color: '#6b7280' },
        grid: { color: 'rgba(229, 234, 242, 0.9)' }
      },
      x: {
        ticks: { color: '#6b7280' },
        grid: { display: false }
      }
    }
  };

  completionChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  completionChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { precision: 0, color: '#6b7280' },
        grid: { color: 'rgba(229, 234, 242, 0.9)' }
      },
      x: {
        ticks: { color: '#6b7280' },
        grid: { display: false }
      }
    }
  };

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      void this.router.navigate(['/engineers']);
      return;
    }

    this.loadEngineer(id);
  }

  trackByProjectId(_: number, project: Project): number {
    return project.id;
  }

  trackByTaskId(_: number, task: Task): number {
    return task.id;
  }

  viewProject(projectId: number): void {
    void this.router.navigate(['/projects', projectId]);
  }

  viewTask(taskId: number): void {
    void this.router.navigate(['/tasks', taskId]);
  }

  private loadEngineer(id: number): void {
    this.isLoading.set(true);

    forkJoin({
      engineer: this.engineerService.getById(id),
      performance: this.engineerService.getPerformanceById(id),
      assignedProjects: this.assignmentService.getByEngineer(id),
      assignedTasks: this.taskService.getAll({ engineerId: id, pageSize: 100 })
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ engineer, performance, assignedProjects, assignedTasks }) => {
          this.engineer.set(engineer);
          this.performance.set(performance);
          this.assignedProjects.set(assignedProjects);
          this.assignedTasks.set(assignedTasks.items);
          this.buildCharts(performance);
          this.loadProgressLogs(assignedTasks.items);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          void this.router.navigate(['/engineers']);
        }
      });
  }

  private loadProgressLogs(tasks: Task[]): void {
    const taskIds = tasks.slice(0, 15).map(task => task.id);
    if (taskIds.length === 0) {
      this.progressLogs.set([]);
      return;
    }

    forkJoin(
      taskIds.map(taskId =>
        this.progressLogService.getByTaskId(taskId).pipe(
          catchError(() => of([] as TaskProgressLog[]))
        )
      )
    )
      .pipe(
        map(groups => groups.flat().sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(logs => this.progressLogs.set(logs));
  }

  private buildCharts(performance: EngineerPerformanceDetails): void {
    const trend = [...performance.trend].reverse();

    this.productivityChartData = {
      labels: trend.map(point => point.label),
      datasets: [
        {
          data: trend.map(point => point.performanceScore),
          label: 'Performance score',
          borderColor: '#0f2d52',
          backgroundColor: 'rgba(15, 45, 82, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 4,
          pointBackgroundColor: '#0f2d52'
        },
        {
          data: trend.map(point => point.onTimeRate),
          label: 'On-time %',
          borderColor: '#2e7d32',
          backgroundColor: 'transparent',
          tension: 0.35,
          pointRadius: 3,
          pointBackgroundColor: '#2e7d32'
        }
      ]
    };

    this.completionChartData = {
      labels: trend.map(point => point.label),
      datasets: [
        {
          data: trend.map(point => point.completedTasks),
          label: 'Completed tasks',
          backgroundColor: 'rgba(15, 45, 82, 0.85)',
          borderRadius: 8,
          maxBarThickness: 36
        }
      ]
    };
  }
}
