import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ProjectRisk } from '../../../../core/models/project-risk.model';
import { ProjectStatusChipComponent } from '../project-status-chip/project-status-chip.component';
import { RiskChipComponent } from '../../../../shared/components/risk-chip/risk-chip.component';

@Component({
  selector: 'app-project-card',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    ProjectStatusChipComponent,
    RiskChipComponent
  ],
  templateUrl: './project-card.component.html',
  styleUrl: './project-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectCardComponent {
  readonly project = input.required<ProjectRisk>();
  readonly isAdmin = input(false);

  readonly view = output<ProjectRisk>();
  readonly edit = output<ProjectRisk>();
  readonly delete = output<ProjectRisk>();
}
