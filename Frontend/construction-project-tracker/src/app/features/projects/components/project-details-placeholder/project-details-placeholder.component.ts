import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-project-details-placeholder',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: './project-details-placeholder.component.html',
  styleUrl: './project-details-placeholder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailsPlaceholderComponent {
  readonly title = input.required<string>();
  readonly message = input('This section will be available in a future update.');
  readonly icon = input('construction');
}
