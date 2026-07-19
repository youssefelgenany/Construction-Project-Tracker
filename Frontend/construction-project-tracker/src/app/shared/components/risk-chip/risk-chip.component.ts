import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { RiskLevel } from '../../../core/enums/risk-level';

@Component({
  selector: 'app-risk-chip',
  standalone: true,
  imports: [NgClass],
  templateUrl: './risk-chip.component.html',
  styleUrl: './risk-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RiskChipComponent {
  readonly level = input.required<RiskLevel>();
  readonly showHealthy = input(true);
  readonly compact = input(false);

  readonly label = computed(() => {
    switch (this.level()) {
      case RiskLevel.Critical:
        return 'Critical';
      case RiskLevel.High:
        return 'High Risk';
      case RiskLevel.Medium:
        return 'Medium Risk';
      case RiskLevel.Low:
        return 'Low Risk';
      case RiskLevel.None:
      default:
        return this.showHealthy() ? 'Healthy' : 'None';
    }
  });

  readonly className = computed(() => {
    switch (this.level()) {
      case RiskLevel.Critical:
        return 'risk-critical';
      case RiskLevel.High:
        return 'risk-high';
      case RiskLevel.Medium:
        return 'risk-medium';
      case RiskLevel.Low:
        return 'risk-low';
      case RiskLevel.None:
      default:
        return 'risk-none';
    }
  });
}
