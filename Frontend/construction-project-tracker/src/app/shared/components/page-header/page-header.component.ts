import { Component, input } from '@angular/core';
import { HeroHeaderComponent } from '../hero-header/hero-header.component';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [HeroHeaderComponent],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss'
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly eyebrow = input<string>('');
}
