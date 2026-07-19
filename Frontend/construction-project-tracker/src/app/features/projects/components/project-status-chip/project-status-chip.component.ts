import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { ProjectStatus } from '../../../../core/enums/project-status';
import { getProjectStatusClass, getProjectStatusLabel } from '../../projects.utils';

@Component({
  selector: 'app-project-status-chip',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './project-status-chip.component.html',
  styleUrl: './project-status-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectStatusChipComponent {
  readonly status = input.required<ProjectStatus>();

  getLabel(value: ProjectStatus): string {
    return getProjectStatusLabel(value);
  }

  getClass(value: ProjectStatus): string {
    return getProjectStatusClass(value);
  }
}
