import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { getEngineerStatusClass, getEngineerStatusLabel } from '../../engineers.utils';

@Component({
  selector: 'app-engineer-status-chip',
  standalone: true,
  imports: [MatChipsModule],
  templateUrl: './engineer-status-chip.component.html',
  styleUrl: './engineer-status-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerStatusChipComponent {
  readonly isActive = input.required<boolean>();

  getLabel(active: boolean): string {
    return getEngineerStatusLabel(active);
  }

  getClass(active: boolean): string {
    return getEngineerStatusClass(active);
  }
}
