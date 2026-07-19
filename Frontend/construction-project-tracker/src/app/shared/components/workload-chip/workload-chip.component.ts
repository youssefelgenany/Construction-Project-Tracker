import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { WorkloadLevel } from '../../../core/enums/workload-level';

@Component({
  selector: 'app-workload-chip',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './workload-chip.component.html',
  styleUrl: './workload-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkloadChipComponent {
  readonly level = input.required<WorkloadLevel>();
  readonly overdueTasks = input(0);

  readonly label = computed(() => {
    switch (this.level()) {
      case WorkloadLevel.Low:
        return 'Available';
      case WorkloadLevel.Medium:
        return 'Busy';
      case WorkloadLevel.High:
        return this.overdueTasks() > 0 ? 'Overloaded' : 'Busy';
      default:
        return 'Available';
    }
  });

  readonly chipClass = computed(() => {
    switch (this.level()) {
      case WorkloadLevel.Low:
        return 'capacity-available';
      case WorkloadLevel.Medium:
        return 'capacity-busy';
      case WorkloadLevel.High:
        return this.overdueTasks() > 0 ? 'capacity-overloaded' : 'capacity-busy';
      default:
        return 'capacity-available';
    }
  });
}
