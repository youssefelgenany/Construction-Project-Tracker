import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { EngineerWorkload } from '../../../../core/models/engineer-workload.model';
import { WorkloadChipComponent } from '../../../../shared/components/workload-chip/workload-chip.component';

@Component({
  selector: 'app-engineer-workload',
  standalone: true,
  imports: [MatCardModule, MatTableModule, WorkloadChipComponent],
  templateUrl: './engineer-workload.component.html',
  styleUrl: './engineer-workload.component.scss'
})
export class EngineerWorkloadComponent {
  readonly workloads = input.required<EngineerWorkload[]>();

  readonly displayedColumns = [
    'engineerName',
    'assignedProjects',
    'assignedTasks',
    'overdueTasks',
    'workload'
  ];
}
