import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { EngineerService } from '../../../core/services/engineer.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UpdateEngineer } from '../../../core/models/engineer';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import {
  EngineerFormComponent,
  EngineerFormSubmit
} from '../components/engineer-form/engineer-form.component';

@Component({
  selector: 'app-engineer-edit',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EngineerFormComponent
  ],
  templateUrl: './engineer-edit.component.html',
  styleUrl: './engineer-edit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerEditComponent implements OnInit {
  private readonly engineerService = inject(EngineerService);
  private readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly engineerId = signal<number | null>(null);
  readonly editValue = signal<UpdateEngineer | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      void this.router.navigate(['/engineers']);
      return;
    }

    this.engineerId.set(id);
    this.loadEngineer(id);
  }

  onSubmit(result: EngineerFormSubmit): void {
    if (result.mode !== 'edit') {
      return;
    }

    const id = this.engineerId();
    if (!id) {
      return;
    }

    this.isSubmitting.set(true);

    this.engineerService
      .update(id, result.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Engineer updated successfully.');
          void this.router.navigate(['/engineers', id]);
        },
        error: () => this.isSubmitting.set(false),
        complete: () => this.isSubmitting.set(false)
      });
  }

  onCancel(): void {
    const id = this.engineerId();
    void this.router.navigate(id ? ['/engineers', id] : ['/engineers']);
  }

  private loadEngineer(id: number): void {
    this.isLoading.set(true);

    this.engineerService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: engineer => {
          this.editValue.set({
            fullName: engineer.fullName,
            email: engineer.email,
            phoneNumber: engineer.phoneNumber,
            position: engineer.position,
            hireDate: engineer.hireDate,
            isActive: engineer.isActive
          });
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          void this.router.navigate(['/engineers']);
        }
      });
  }
}
