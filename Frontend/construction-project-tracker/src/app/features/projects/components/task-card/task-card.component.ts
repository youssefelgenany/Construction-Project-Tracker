import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TaskStatus } from '../../../../core/enums/task-status';
import { Task } from '../../../../core/models/task';
import {
  getTaskPriorityClass,
  getTaskPriorityLabel,
  getTaskStatusClass,
  getTaskStatusLabel
} from '../../projects.utils';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule
  ],
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCardComponent {
  readonly task = input.required<Task>();
  readonly isAdmin = input(false);

  readonly edit = output<Task>();
  readonly delete = output<Task>();
  readonly manageDependencies = output<Task>();

  readonly TaskStatus = TaskStatus;

  getPriorityLabel = getTaskPriorityLabel;
  getPriorityClass = getTaskPriorityClass;
  getStatusLabel = getTaskStatusLabel;
  getStatusClass = getTaskStatusClass;

  isBlocked(task: Task): boolean {
    return task.status === TaskStatus.Blocked;
  }

  prerequisiteTitles(task: Task): string {
    return (task.incompletePrerequisites ?? []).map(p => p.title).join(', ');
  }
}
