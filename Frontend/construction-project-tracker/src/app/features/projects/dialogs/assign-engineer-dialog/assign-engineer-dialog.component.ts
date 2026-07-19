import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatRadioModule } from '@angular/material/radio';
import { forkJoin } from 'rxjs';
import { AssignmentService } from '../../../../core/services/assignment.service';
import { EngineerService } from '../../../../core/services/engineer.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { EngineerWorkload } from '../../../../core/models/engineer-workload.model';
import { WorkloadChipComponent } from '../../../../shared/components/workload-chip/workload-chip.component';
import { AssignEngineerDialogData } from '../assign-engineer-dialog-data';

@Component({
  selector: 'app-assign-engineer-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatRadioModule,
    WorkloadChipComponent
  ],
  templateUrl: './assign-engineer-dialog.component.html',
  styleUrl: './assign-engineer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssignEngineerDialogComponent implements OnInit {
  private readonly assignmentService = inject(AssignmentService);
  private readonly engineerService = inject(EngineerService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialogRef = inject(MatDialogRef<AssignEngineerDialogComponent>);
  private readonly data = inject<AssignEngineerDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly selectedEngineerId = signal<number | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly availableEngineers = signal<EngineerWorkload[]>([]);

  readonly searchTerm = signal('');

  readonly filteredEngineers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const engineers = this.availableEngineers();

    if (!term) {
      return engineers;
    }

    return engineers.filter(
      engineer =>
        engineer.engineerName.toLowerCase().includes(term) ||
        engineer.email.toLowerCase().includes(term) ||
        engineer.position.toLowerCase().includes(term)
    );
  });

  readonly displayedColumns = [
    'select',
    'engineerName',
    'position',
    'projects',
    'activeTasks',
    'overdueTasks',
    'workload'
  ];

  ngOnInit(): void {
    this.loadAvailableEngineers();

    this.searchControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => this.searchTerm.set(value));
  }

  selectEngineer(engineerId: number): void {
    this.selectedEngineerId.set(engineerId);
  }

  assign(): void {
    const engineerId = this.selectedEngineerId();
    if (!engineerId || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    this.assignmentService
      .assign({ projectId: this.data.projectId, engineerId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.success('Engineer assigned to project successfully.');
          this.dialogRef.close(true);
        },
        error: (error: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          if (error.status === 409) {
            const message =
              typeof error.error === 'object' && error.error && 'message' in error.error
                ? String(error.error.message)
                : 'This engineer is already assigned to the project.';
            this.notificationService.warning(message);
          }
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  trackByEngineerId(_: number, engineer: EngineerWorkload): number {
    return engineer.engineerId;
  }

  private loadAvailableEngineers(): void {
    this.isLoading.set(true);

    forkJoin({
      workload: this.engineerService.getWorkload({ pageNumber: 1, pageSize: 500 }),
      assigned: this.assignmentService.getByProject(this.data.projectId)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ workload, assigned }) => {
          const assignedIds = new Set(assigned.map(a => a.engineerId));
          this.availableEngineers.set(
            workload.items.filter(e => e.isActive && !assignedIds.has(e.engineerId))
          );
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }
}
