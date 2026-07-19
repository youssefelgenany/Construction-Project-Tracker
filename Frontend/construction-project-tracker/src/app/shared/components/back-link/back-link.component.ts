import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-back-link',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './back-link.component.html',
  styleUrl: './back-link.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BackLinkComponent {
  readonly link = input.required<string | any[]>();
  readonly label = input('Back');
}
