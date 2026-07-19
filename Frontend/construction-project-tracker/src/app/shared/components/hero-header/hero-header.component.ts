import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-hero-header',
  standalone: true,
  imports: [],
  templateUrl: './hero-header.component.html',
  styleUrl: './hero-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeroHeaderComponent {
  readonly title = input.required<string>();
  readonly description = input<string>();
  readonly eyebrow = input<string>('Overview');
}
