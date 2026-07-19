import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ProjectDetails } from '../../../../core/models/project';
import { ProjectStatusChipComponent } from '../project-status-chip/project-status-chip.component';

@Component({
  selector: 'app-project-details-general',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    ProjectStatusChipComponent
  ],
  templateUrl: './project-details-general.component.html',
  styleUrl: './project-details-general.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailsGeneralComponent {
  readonly project = input.required<ProjectDetails>();
}
