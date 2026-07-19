import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { PerformanceTier } from '../../../core/enums/performance-tier';

@Component({
  selector: 'app-performance-badge',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './performance-badge.component.html',
  styleUrl: './performance-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PerformanceBadgeComponent {
  readonly tier = input.required<PerformanceTier>();

  readonly label = computed(() => {
    switch (this.tier()) {
      case PerformanceTier.Excellent:
        return 'Excellent';
      case PerformanceTier.Good:
        return 'Good';
      case PerformanceTier.Average:
        return 'Average';
      default:
        return 'Needs Attention';
    }
  });

  readonly chipClass = computed(() => {
    switch (this.tier()) {
      case PerformanceTier.Excellent:
        return 'tier-excellent';
      case PerformanceTier.Good:
        return 'tier-good';
      case PerformanceTier.Average:
        return 'tier-average';
      default:
        return 'tier-needs-attention';
    }
  });
}
