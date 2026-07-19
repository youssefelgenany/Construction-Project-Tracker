import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AttentionProject } from '../../../../core/models/executive-reports.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { RiskChipComponent } from '../../../../shared/components/risk-chip/risk-chip.component';

@Component({
  selector: 'app-reports-attention',
  standalone: true,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
    LoadingSpinnerComponent,
    RiskChipComponent
  ],
  templateUrl: './reports-attention.component.html',
  styleUrl: './reports-attention.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsAttentionComponent {
  readonly projects = input<AttentionProject[]>([]);
  readonly loading = input(false);
  readonly viewProject = output<number>();

  readonly displayedColumns = [
    'projectName',
    'riskLevel',
    'completionPercent',
    'overdueTasks',
    'engineers',
    'reason',
    'actions'
  ];
}
