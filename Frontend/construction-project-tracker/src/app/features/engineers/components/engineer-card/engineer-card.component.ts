import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import {
  EngineerPerformance
} from '../../../../core/models/engineer-performance.model';
import { EngineerWorkload } from '../../../../core/models/engineer-workload.model';
import { PerformanceBadgeComponent } from '../../../../shared/components/performance-badge/performance-badge.component';
import { WorkloadChipComponent } from '../../../../shared/components/workload-chip/workload-chip.component';
import { EngineerStatusChipComponent } from '../engineer-status-chip/engineer-status-chip.component';

@Component({
  selector: 'app-engineer-card',
  standalone: true,
  imports: [
    DecimalPipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    PerformanceBadgeComponent,
    WorkloadChipComponent,
    EngineerStatusChipComponent
  ],
  templateUrl: './engineer-card.component.html',
  styleUrl: './engineer-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerCardComponent {
  readonly engineer = input.required<EngineerWorkload | EngineerPerformance>();
  readonly mode = input<'workload' | 'performance'>('workload');
  readonly view = output<EngineerWorkload | EngineerPerformance>();

  workloadEngineer(): EngineerWorkload {
    return this.engineer() as EngineerWorkload;
  }

  performanceEngineer(): EngineerPerformance {
    return this.engineer() as EngineerPerformance;
  }
}
