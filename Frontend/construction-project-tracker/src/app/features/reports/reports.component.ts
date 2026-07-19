import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ReportsService } from '../../core/services/reports.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReportFilters } from '../../core/models/report-filters';
import {
  AttentionProject,
  EngineerPerformanceReportRow,
  ExecutiveSummary,
  ProjectHealth,
  ProjectProgressPoint,
  ReportActivity,
  TaskAnalytics,
  WorkloadBar
} from '../../core/models/executive-reports.model';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ReportsSummaryCardsComponent } from './components/reports-summary-cards/reports-summary-cards.component';
import { ReportsFiltersComponent } from './components/reports-filters/reports-filters.component';
import { ReportsChartsComponent } from './components/reports-charts/reports-charts.component';
import { EngineerPerformanceReportComponent } from './components/engineer-performance-report/engineer-performance-report.component';
import { ReportsActivityComponent } from './components/reports-activity/reports-activity.component';
import { ReportsAttentionComponent } from './components/reports-attention/reports-attention.component';
import { buildReportFileName, downloadReportBlob } from './reports.utils';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    PageHeaderComponent,
    ReportsSummaryCardsComponent,
    ReportsFiltersComponent,
    ReportsChartsComponent,
    EngineerPerformanceReportComponent,
    ReportsActivityComponent,
    ReportsAttentionComponent
  ],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsComponent implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private loadGeneration = 0;

  readonly isExporting = signal(false);
  readonly filters = signal<ReportFilters>({});

  readonly summaryLoading = signal(true);
  readonly healthLoading = signal(true);
  readonly progressLoading = signal(true);
  readonly engineerLoading = signal(true);
  readonly workloadLoading = signal(true);
  readonly tasksLoading = signal(true);
  readonly activityLoading = signal(true);
  readonly attentionLoading = signal(true);

  readonly summary = signal<ExecutiveSummary | null>(null);
  readonly health = signal<ProjectHealth | null>(null);
  readonly progress = signal<ProjectProgressPoint[]>([]);
  readonly engineerPerformance = signal<EngineerPerformanceReportRow[]>([]);
  readonly workload = signal<WorkloadBar[]>([]);
  readonly taskAnalytics = signal<TaskAnalytics | null>(null);
  readonly activity = signal<ReportActivity[]>([]);
  readonly attention = signal<AttentionProject[]>([]);

  readonly emptySummary: ExecutiveSummary = {
    totalProjects: 0,
    healthyProjects: 0,
    atRiskProjects: 0,
    delayedProjects: 0,
    totalEngineers: 0,
    activeEngineers: 0,
    totalTasks: 0,
    completedTasks: 0,
    overdueTasks: 0,
    averageProjectCompletion: 0,
    onTimeCompletionRate: 0,
    averageEngineerWorkload: 0
  };

  ngOnInit(): void {
    this.loadAllSections();
  }

  onFiltersChange(filters: ReportFilters): void {
    this.filters.set(filters);
    this.loadAllSections();
  }

  viewProject(projectId: number): void {
    void this.router.navigate(['/projects', projectId]);
  }

  exportPdf(): void {
    this.exportReport('pdf');
  }

  exportExcel(): void {
    this.exportReport('excel');
  }

  private loadAllSections(): void {
    const generation = ++this.loadGeneration;
    const filters = this.filters();
    this.loadSummary(filters, generation);
    this.loadHealth(filters, generation);
    this.loadProgress(filters, generation);
    this.loadEngineerPerformance(filters, generation);
    this.loadWorkload(filters, generation);
    this.loadTaskAnalytics(filters, generation);
    this.loadActivity(filters, generation);
    this.loadAttention(filters, generation);
  }

  private isStale(generation: number): boolean {
    return generation !== this.loadGeneration;
  }

  private loadSummary(filters: ReportFilters, generation: number): void {
    this.summaryLoading.set(true);
    this.reportsService
      .getExecutiveSummary(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.summary.set(data);
          this.summaryLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.summary.set(null);
          this.summaryLoading.set(false);
          this.notificationService.error('Unable to load executive summary.');
        }
      });
  }

  private loadHealth(filters: ReportFilters, generation: number): void {
    this.healthLoading.set(true);
    this.reportsService
      .getProjectHealth(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.health.set(data);
          this.healthLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.health.set(null);
          this.healthLoading.set(false);
        }
      });
  }

  private loadProgress(filters: ReportFilters, generation: number): void {
    this.progressLoading.set(true);
    this.reportsService
      .getProjectProgressTimeline(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.progress.set(data);
          this.progressLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.progress.set([]);
          this.progressLoading.set(false);
        }
      });
  }

  private loadEngineerPerformance(filters: ReportFilters, generation: number): void {
    this.engineerLoading.set(true);
    this.reportsService
      .getEngineerPerformance(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.engineerPerformance.set(data);
          this.engineerLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.engineerPerformance.set([]);
          this.engineerLoading.set(false);
        }
      });
  }

  private loadWorkload(filters: ReportFilters, generation: number): void {
    this.workloadLoading.set(true);
    this.reportsService
      .getWorkload(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.workload.set(data);
          this.workloadLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.workload.set([]);
          this.workloadLoading.set(false);
        }
      });
  }

  private loadTaskAnalytics(filters: ReportFilters, generation: number): void {
    this.tasksLoading.set(true);
    this.reportsService
      .getTaskAnalytics(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.taskAnalytics.set(data);
          this.tasksLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.taskAnalytics.set(null);
          this.tasksLoading.set(false);
        }
      });
  }

  private loadActivity(filters: ReportFilters, generation: number): void {
    this.activityLoading.set(true);
    this.reportsService
      .getActivity(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.activity.set(data);
          this.activityLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.activity.set([]);
          this.activityLoading.set(false);
        }
      });
  }

  private loadAttention(filters: ReportFilters, generation: number): void {
    this.attentionLoading.set(true);
    this.reportsService
      .getAttention(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: data => {
          if (this.isStale(generation)) return;
          this.attention.set(data);
          this.attentionLoading.set(false);
        },
        error: () => {
          if (this.isStale(generation)) return;
          this.attention.set([]);
          this.attentionLoading.set(false);
        }
      });
  }

  private exportReport(type: 'pdf' | 'excel'): void {
    if (this.isExporting()) {
      return;
    }

    this.isExporting.set(true);
    const filters = this.filters();
    const request =
      type === 'pdf'
        ? this.reportsService.exportPdf(filters)
        : this.reportsService.exportExcel(filters);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: blob => {
        const extension = type === 'pdf' ? 'pdf' : 'xlsx';
        downloadReportBlob(blob, buildReportFileName(extension));
        this.notificationService.success(
          type === 'pdf' ? 'Report exported to PDF successfully.' : 'Report exported to Excel successfully.'
        );
        this.isExporting.set(false);
      },
      error: () => {
        this.notificationService.error(
          type === 'pdf' ? 'PDF export failed.' : 'Excel export failed.'
        );
        this.isExporting.set(false);
      }
    });
  }
}
