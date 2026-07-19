import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { forkJoin, Observable } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import {
  AdminDashboardData,
  DashboardSummary,
  EngineerDashboardData,
  EngineerPerformance,
  EngineerWorkload,
  MonthlyProjects,
  ProjectProgressChart,
  ProjectStatusDistribution,
  RecentActivity
} from '../../core/models/dashboard';
import { UserRole, parseUserRole } from '../../core/enums/user-role';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatCardComponent } from './components/stat-card/stat-card.component';
import { ChartCardComponent } from './components/chart-card/chart-card.component';
import { RecentActivityComponent } from './components/recent-activity/recent-activity.component';
import { EngineerWorkloadComponent } from './components/engineer-workload/engineer-workload.component';
import { TopPerformingEngineersComponent } from './components/top-performing-engineers/top-performing-engineers.component';
import { RiskSummaryComponent } from './components/risk-summary/risk-summary.component';
import { ScheduleSummaryComponent } from './components/schedule-summary/schedule-summary.component';
import { ProjectRisk } from '../../core/models/project-risk.model';
import { ScheduleSummary } from '../../core/models/schedule-summary.model';
import { ScheduleService } from '../../core/services/schedule.service';
import { HeroHeaderComponent } from '../../shared/components/hero-header/hero-header.component';

type DashboardRiskSummary = {
  projectsAtRiskCount: number;
  tasksAtRiskCount: number;
  overdueTasksCount: number;
  pendingReviewsCount: number;
  projects: ProjectRisk[];
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    BaseChartDirective,
    LoadingSpinnerComponent,
    HeroHeaderComponent,
    StatCardComponent,
    ChartCardComponent,
    RecentActivityComponent,
    EngineerWorkloadComponent,
    TopPerformingEngineersComponent,
    RiskSummaryComponent,
    ScheduleSummaryComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly scheduleService = inject(ScheduleService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly user = this.authService.user;
  readonly isAdmin = this.authService.isAdmin;

  readonly isLoading = signal(true);
  readonly hasError = signal(false);

  readonly summary = signal<DashboardSummary | null>(null);
  readonly projectProgress = signal<ProjectProgressChart[]>([]);
  readonly engineerWorkload = signal<EngineerWorkload[]>([]);
  readonly topPerformers = signal<EngineerPerformance[]>([]);
  readonly riskSummary = signal<DashboardRiskSummary | null>(null);
  readonly scheduleSummary = signal<ScheduleSummary | null>(null);
  readonly projectStatus = signal<ProjectStatusDistribution | null>(null);
  readonly monthlyProjects = signal<MonthlyProjects[]>([]);
  readonly recentActivities = signal<RecentActivity[]>([]);

  progressChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  progressChartOptions: ChartConfiguration<'bar'>['options'] = this.buildProgressChartOptions();

  statusChartData: ChartConfiguration<'pie'>['data'] = { labels: [], datasets: [] };
  statusChartOptions: ChartConfiguration<'pie'>['options'] = this.buildStatusChartOptions();

  monthlyChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  monthlyChartOptions: ChartConfiguration<'bar'>['options'] = this.buildMonthlyChartOptions();

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.buildDashboardRequest()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => this.applyDashboardData(data),
        error: () => {
          this.hasError.set(true);
          this.isLoading.set(false);
        },
        complete: () => this.isLoading.set(false)
      });
  }

  getRoleLabel(): string {
    const role = this.user()?.role;
    return parseUserRole(role ?? '') === UserRole.Admin ? 'Administrator' : 'Engineer';
  }

  private buildDashboardRequest(): Observable<
    (EngineerDashboardData | AdminDashboardData) & {
      riskSummary: DashboardRiskSummary;
      scheduleSummary: ScheduleSummary;
    }
  > {
    if (this.isAdmin()) {
      return forkJoin({
        summary: this.dashboardService.getSummary(),
        projectProgress: this.dashboardService.getProjectProgress(),
        engineerWorkload: this.dashboardService.getEngineerWorkload(),
        topPerformers: this.dashboardService.getTopPerformingEngineers(),
        riskSummary: this.dashboardService.getRiskSummary(),
        scheduleSummary: this.scheduleService.getScheduleSummary(),
        projectStatus: this.dashboardService.getProjectStatusDistribution(),
        monthlyProjects: this.dashboardService.getMonthlyProjects(),
        recentActivities: this.dashboardService.getRecentActivities()
      });
    }

    return forkJoin({
      summary: this.dashboardService.getSummary(),
      projectProgress: this.dashboardService.getProjectProgress(),
      engineerWorkload: this.dashboardService.getEngineerWorkload(),
      topPerformers: this.dashboardService.getTopPerformingEngineers(),
      riskSummary: this.dashboardService.getRiskSummary(),
      scheduleSummary: this.scheduleService.getScheduleSummary()
    });
  }

  private applyDashboardData(
    data: (EngineerDashboardData | AdminDashboardData) & {
      riskSummary: DashboardRiskSummary;
      scheduleSummary: ScheduleSummary;
    }
  ): void {
    this.summary.set(data.summary);
    this.projectProgress.set(data.projectProgress);
    this.engineerWorkload.set(data.engineerWorkload);
    this.topPerformers.set(data.topPerformers);
    this.riskSummary.set(data.riskSummary);
    this.scheduleSummary.set(data.scheduleSummary);
    this.updateProgressChart(data.projectProgress);

    if ('projectStatus' in data) {
      this.projectStatus.set(data.projectStatus);
      this.monthlyProjects.set(data.monthlyProjects);
      this.recentActivities.set(data.recentActivities);
      this.updateStatusChart(data.projectStatus);
      this.updateMonthlyChart(data.monthlyProjects);
    } else {
      this.projectStatus.set(null);
      this.monthlyProjects.set([]);
      this.recentActivities.set([]);
    }
  }

  private updateProgressChart(items: ProjectProgressChart[]): void {
    this.progressChartData = {
      labels: items.map(item => item.projectName),
      datasets: [
        {
          label: 'Progress %',
          data: items.map(item => item.progressPercentage),
          backgroundColor: '#0f2d52',
          hoverBackgroundColor: '#153760',
          borderRadius: 999,
          borderSkipped: false,
          barThickness: 18
        }
      ]
    };
  }

  private updateStatusChart(distribution: ProjectStatusDistribution): void {
    this.statusChartData = {
      labels: ['Completed', 'In Progress', 'Not Started'],
      datasets: [
        {
          data: [distribution.completed, distribution.inProgress, distribution.notStarted],
          backgroundColor: ['#2e7d32', '#0f2d52', '#cdd5df'],
          hoverBackgroundColor: ['#256628', '#153760', '#bec8d4'],
          borderWidth: 0
        }
      ]
    };
  }

  private updateMonthlyChart(items: MonthlyProjects[]): void {
    this.monthlyChartData = {
      labels: items.map(item => item.month),
      datasets: [
        {
          label: 'Projects Created',
          data: items.map(item => item.projectsCreated),
          backgroundColor: '#8fa6bf',
          hoverBackgroundColor: '#6d86a1',
          borderRadius: 999,
          borderSkipped: false
        }
      ]
    };
  }

  private buildProgressChartOptions(): ChartConfiguration<'bar'>['options'] {
    return {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: '#111827',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          padding: 12,
          displayColors: false
        }
      },
      scales: {
        x: {
          beginAtZero: true,
          max: 100,
          grid: { color: '#e5eaf2' },
          border: { display: false },
          ticks: {
            color: '#6b7280',
            callback: value => `${value}%`
          }
        },
        y: {
          grid: { display: false },
          border: { display: false },
          ticks: { color: '#111827' }
        }
      }
    };
  }

  private buildStatusChartOptions(): ChartConfiguration<'pie'>['options'] {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            color: '#6b7280',
            boxWidth: 12,
            boxHeight: 12,
            usePointStyle: true,
            pointStyle: 'circle',
            padding: 18
          }
        },
        tooltip: {
          backgroundColor: '#111827',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          padding: 12
        }
      }
    };
  }

  private buildMonthlyChartOptions(): ChartConfiguration<'bar'>['options'] {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: '#111827',
          titleColor: '#ffffff',
          bodyColor: '#ffffff',
          padding: 12,
          displayColors: false
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: { color: '#e5eaf2' },
          border: { display: false },
          ticks: {
            color: '#6b7280',
            stepSize: 1
          }
        },
        x: {
          grid: { display: false },
          border: { display: false },
          ticks: { color: '#6b7280' }
        }
      }
    };
  }
}
