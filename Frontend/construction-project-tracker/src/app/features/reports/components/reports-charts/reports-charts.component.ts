import {
  ChangeDetectionStrategy,
  Component,
  OnChanges,
  SimpleChanges,
  input
} from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { ChartCardComponent } from '../../../dashboard/components/chart-card/chart-card.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import {
  ProjectHealth,
  ProjectProgressPoint,
  TaskAnalytics,
  WorkloadBar
} from '../../../../core/models/executive-reports.model';

@Component({
  selector: 'app-reports-charts',
  standalone: true,
  imports: [BaseChartDirective, ChartCardComponent, LoadingSpinnerComponent],
  templateUrl: './reports-charts.component.html',
  styleUrl: './reports-charts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsChartsComponent implements OnChanges {
  readonly health = input<ProjectHealth | null>(null);
  readonly progress = input<ProjectProgressPoint[]>([]);
  readonly workload = input<WorkloadBar[]>([]);
  readonly taskAnalytics = input<TaskAnalytics | null>(null);
  readonly healthLoading = input(false);
  readonly progressLoading = input(false);
  readonly workloadLoading = input(false);
  readonly tasksLoading = input(false);

  healthChartData: ChartConfiguration<'doughnut'>['data'] = { labels: [], datasets: [] };
  progressChartData: ChartConfiguration<'line'>['data'] = { labels: [], datasets: [] };
  workloadChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  priorityChartData: ChartConfiguration<'doughnut'>['data'] = { labels: [], datasets: [] };
  statusChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  overdueChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  trendChartData: ChartConfiguration<'line'>['data'] = { labels: [], datasets: [] };

  readonly doughnutOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } }
  };

  readonly lineOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, max: 100, ticks: { color: '#6b7280' }, grid: { color: 'rgba(229,234,242,0.9)' } },
      x: { ticks: { color: '#6b7280' }, grid: { display: false } }
    }
  };

  readonly horizontalBarOptions: ChartConfiguration<'bar'>['options'] = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } },
    scales: {
      x: { beginAtZero: true, max: 100, ticks: { color: '#6b7280' }, grid: { color: 'rgba(229,234,242,0.9)' } },
      y: { ticks: { color: '#6b7280' }, grid: { display: false } }
    }
  };

  readonly barOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { precision: 0, color: '#6b7280' }, grid: { color: 'rgba(229,234,242,0.9)' } },
      x: { ticks: { color: '#6b7280' }, grid: { display: false } }
    }
  };

  readonly trendOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { precision: 0, color: '#6b7280' }, grid: { color: 'rgba(229,234,242,0.9)' } },
      x: { ticks: { color: '#6b7280' }, grid: { display: false } }
    }
  };

  ngOnChanges(_: SimpleChanges): void {
    this.updateCharts();
  }

  private updateCharts(): void {
    const health = this.health();
    if (health) {
      this.healthChartData = {
        labels: ['Healthy', 'At Risk', 'Critical', 'Completed'],
        datasets: [
          {
            data: [health.healthy, health.atRisk, health.critical, health.completed],
            backgroundColor: ['#2e7d32', '#ef6c00', '#c62828', '#0f2d52'],
            borderWidth: 0
          }
        ]
      };
    }

    const progress = this.progress();
    this.progressChartData = {
      labels: progress.map(p => p.label),
      datasets: [
        {
          data: progress.map(p => p.averageCompletionPercent),
          label: 'Avg completion %',
          borderColor: '#0f2d52',
          backgroundColor: 'rgba(15,45,82,0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 3,
          pointBackgroundColor: '#0f2d52'
        }
      ]
    };

    const workload = this.workload().slice(0, 12);
    this.workloadChartData = {
      labels: workload.map(w => w.engineerName),
      datasets: [
        {
          data: workload.map(w => w.workloadPercent),
          label: 'Workload %',
          backgroundColor: 'rgba(15,45,82,0.85)',
          borderRadius: 6,
          maxBarThickness: 22
        },
        {
          data: workload.map(w => w.overdueTasks),
          label: 'Overdue tasks',
          backgroundColor: 'rgba(198,40,40,0.75)',
          borderRadius: 6,
          maxBarThickness: 22
        }
      ]
    };

    const tasks = this.taskAnalytics();
    if (tasks) {
      this.priorityChartData = {
        labels: ['Low', 'Medium', 'High', 'Critical'],
        datasets: [
          {
            data: [tasks.byPriority.low, tasks.byPriority.medium, tasks.byPriority.high, tasks.byPriority.critical],
            backgroundColor: ['#2e7d32', '#1565c0', '#ef6c00', '#c62828'],
            borderWidth: 0
          }
        ]
      };

      this.statusChartData = {
        labels: ['Not Started', 'In Progress', 'Pending Review', 'Completed', 'Blocked', 'Ready'],
        datasets: [
          {
            data: [
              tasks.byStatus.notStarted,
              tasks.byStatus.inProgress,
              tasks.byStatus.pendingReview,
              tasks.byStatus.completed,
              tasks.byStatus.blocked,
              tasks.byStatus.ready
            ],
            backgroundColor: 'rgba(15,45,82,0.85)',
            borderRadius: 8,
            maxBarThickness: 36
          }
        ]
      };

      this.overdueChartData = {
        labels: ['Overdue', 'Completed'],
        datasets: [
          {
            data: [tasks.overdueVsCompleted.overdue, tasks.overdueVsCompleted.completed],
            backgroundColor: ['#c62828', '#2e7d32'],
            borderRadius: 8,
            maxBarThickness: 48
          }
        ]
      };

      this.trendChartData = {
        labels: tasks.completionTrend.map(p => p.label),
        datasets: [
          {
            data: tasks.completionTrend.map(p => p.count),
            borderColor: '#0f2d52',
            backgroundColor: 'rgba(15,45,82,0.12)',
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: '#0f2d52'
          }
        ]
      };
    }
  }
}
