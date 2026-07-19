import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ExecutiveSummary } from '../../../../core/models/executive-reports.model';

@Component({
  selector: 'app-reports-summary-cards',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './reports-summary-cards.component.html',
  styleUrl: './reports-summary-cards.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsSummaryCardsComponent {
  readonly summary = input.required<ExecutiveSummary>();
  readonly loading = input(false);
}
