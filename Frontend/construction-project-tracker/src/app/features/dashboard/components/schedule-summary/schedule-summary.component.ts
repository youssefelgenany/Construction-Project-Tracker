import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { StatCardComponent } from '../stat-card/stat-card.component';
import { ScheduleSummary } from '../../../../core/models/schedule-summary.model';

@Component({
  selector: 'app-schedule-summary',
  standalone: true,
  imports: [MatCardModule, StatCardComponent],
  templateUrl: './schedule-summary.component.html',
  styleUrl: './schedule-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScheduleSummaryComponent {
  readonly summary = input.required<ScheduleSummary>();
}
