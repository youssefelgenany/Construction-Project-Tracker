import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TaskProgressLogService } from '../../../../core/services/task-progress-log.service';
import { TaskProgressLog } from '../../../../core/models/task-progress-log';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-task-progress-history',
  standalone: true,
  imports: [
    DatePipe,
    MatCardModule,
    MatIconModule,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './task-progress-history.component.html',
  styleUrl: './task-progress-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskProgressHistoryComponent implements OnInit {
  private readonly progressLogService = inject(TaskProgressLogService);
  private readonly destroyRef = inject(DestroyRef);

  readonly taskId = input.required<number>();
  readonly refreshToken = input(0);

  readonly logs = signal<TaskProgressLog[]>([]);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.loadHistory();
  }

  reload(): void {
    this.loadHistory();
  }

  private loadHistory(): void {
    const taskId = this.taskId();
    if (!taskId) {
      return;
    }

    this.isLoading.set(true);

    this.progressLogService
      .getByTaskId(taskId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: logs => {
          this.logs.set(logs);
          this.isLoading.set(false);
        },
        error: () => {
          this.logs.set([]);
          this.isLoading.set(false);
        }
      });
  }
}
