import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { EngineerPerformance } from '../../../../core/models/engineer-performance.model';
import { PerformanceBadgeComponent } from '../../../../shared/components/performance-badge/performance-badge.component';

@Component({
  selector: 'app-top-performing-engineers',
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatTableModule, PerformanceBadgeComponent],
  templateUrl: './top-performing-engineers.component.html',
  styleUrl: './top-performing-engineers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopPerformingEngineersComponent {
  readonly engineers = input.required<EngineerPerformance[]>();

  readonly displayedColumns = ['engineerName', 'performanceScore', 'completedTasks', 'onTimeRate', 'tier'];
}
