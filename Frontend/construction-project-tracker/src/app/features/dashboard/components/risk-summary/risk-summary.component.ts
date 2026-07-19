import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { StatCardComponent } from '../stat-card/stat-card.component';
import { ProjectRisk } from '../../../../core/models/project-risk.model';
import { RiskChipComponent } from '../../../../shared/components/risk-chip/risk-chip.component';

@Component({
  selector: 'app-risk-summary',
  standalone: true,
  imports: [MatCardModule, MatTableModule, StatCardComponent, RiskChipComponent],
  templateUrl: './risk-summary.component.html',
  styleUrl: './risk-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RiskSummaryComponent {
  readonly projectsAtRiskCount = input.required<number>();
  readonly tasksAtRiskCount = input.required<number>();
  readonly overdueTasksCount = input.required<number>();
  readonly pendingReviewsCount = input.required<number>();
  readonly projects = input.required<ProjectRisk[]>();

  readonly displayedColumns = ['project', 'riskLevel', 'reason'] as const;
}
