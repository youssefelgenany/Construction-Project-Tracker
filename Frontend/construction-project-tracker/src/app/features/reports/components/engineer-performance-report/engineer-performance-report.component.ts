import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { EngineerPerformanceReportRow } from '../../../../core/models/executive-reports.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { WorkloadChipComponent } from '../../../../shared/components/workload-chip/workload-chip.component';

type SortKey =
  | 'engineerName'
  | 'projects'
  | 'completedTasks'
  | 'averageCompletionPercent'
  | 'onTimeRate'
  | 'overdueTasks'
  | 'currentWorkloadPercent'
  | 'averageDelayDays'
  | 'performanceScore';

@Component({
  selector: 'app-engineer-performance-report',
  standalone: true,
  imports: [
    DecimalPipe,
    MatIconModule,
    MatSortModule,
    MatTableModule,
    LoadingSpinnerComponent,
    WorkloadChipComponent
  ],
  templateUrl: './engineer-performance-report.component.html',
  styleUrl: './engineer-performance-report.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerPerformanceReportComponent {
  readonly rows = input<EngineerPerformanceReportRow[]>([]);
  readonly loading = input(false);

  readonly sortActive = signal<SortKey>('performanceScore');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  readonly displayedColumns: SortKey[] = [
    'engineerName',
    'performanceScore',
    'projects',
    'completedTasks',
    'averageCompletionPercent',
    'onTimeRate',
    'overdueTasks',
    'currentWorkloadPercent',
    'averageDelayDays'
  ];

  readonly sortedRows = computed(() => {
    const items = [...this.rows()];
    const key = this.sortActive();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;

    return items.sort((a, b) => {
      const av = a[key];
      const bv = b[key];
      if (typeof av === 'string' && typeof bv === 'string') {
        return av.localeCompare(bv) * dir;
      }
      return ((av as number) - (bv as number)) * dir;
    });
  });

  onSortChange(sort: Sort): void {
    if (!sort.active || !sort.direction) {
      this.sortActive.set('performanceScore');
      this.sortDirection.set('desc');
      return;
    }

    this.sortActive.set(sort.active as SortKey);
    this.sortDirection.set(sort.direction);
  }
}
