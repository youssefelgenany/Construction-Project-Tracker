import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-chart-card',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './chart-card.component.html',
  styleUrl: './chart-card.component.scss'
})
export class ChartCardComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
