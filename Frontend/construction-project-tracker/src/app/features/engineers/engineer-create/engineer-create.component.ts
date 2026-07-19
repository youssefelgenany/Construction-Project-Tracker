import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { EngineerService } from '../../../core/services/engineer.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import {
  EngineerFormComponent,
  EngineerFormSubmit
} from '../components/engineer-form/engineer-form.component';

@Component({
  selector: 'app-engineer-create',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, PageHeaderComponent, EngineerFormComponent],
  templateUrl: './engineer-create.component.html',
  styleUrl: './engineer-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerCreateComponent {
  private readonly engineerService = inject(EngineerService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isSubmitting = signal(false);

  onSubmit(result: EngineerFormSubmit): void {
    if (result.mode !== 'create') {
      return;
    }

    this.isSubmitting.set(true);

    this.engineerService
      .create(result.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Engineer created successfully.');
          void this.router.navigate(['/engineers']);
        },
        error: () => this.isSubmitting.set(false),
        complete: () => this.isSubmitting.set(false)
      });
  }

  onCancel(): void {
    void this.router.navigate(['/engineers']);
  }
}
