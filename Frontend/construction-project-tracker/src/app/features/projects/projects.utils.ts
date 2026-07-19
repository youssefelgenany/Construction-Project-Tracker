import { ProjectStatus } from '../../core/enums/project-status';
import { TaskPriority } from '../../core/enums/task-priority';
import { TaskStatus } from '../../core/enums/task-status';
import { Task, UpdateTask } from '../../core/models/task';

export function getProjectStatusLabel(status: ProjectStatus): string {
  switch (status) {
    case ProjectStatus.Completed:
      return 'Completed';
    case ProjectStatus.InProgress:
      return 'In Progress';
    case ProjectStatus.NotStarted:
    default:
      return 'Not Started';
  }
}

export function getProjectStatusClass(status: ProjectStatus): string {
  switch (status) {
    case ProjectStatus.Completed:
      return 'status-completed';
    case ProjectStatus.InProgress:
      return 'status-in-progress';
    case ProjectStatus.NotStarted:
    default:
      return 'status-not-started';
  }
}

export const PROJECT_SORT_FIELDS = {
  name: 'name',
  budget: 'budget',
  startDate: 'startdate',
  endDate: 'enddate',
  risk: 'risk',
  overdue: 'overdue',
  deadline: 'deadline'
} as const;

export type ProjectSortField = keyof typeof PROJECT_SORT_FIELDS;

export function getTaskPriorityLabel(priority: TaskPriority): string {
  switch (priority) {
    case TaskPriority.Critical:
      return 'Critical';
    case TaskPriority.High:
      return 'High';
    case TaskPriority.Medium:
      return 'Medium';
    case TaskPriority.Low:
    default:
      return 'Low';
  }
}

export function getTaskPriorityClass(priority: TaskPriority): string {
  switch (priority) {
    case TaskPriority.Critical:
      return 'priority-critical';
    case TaskPriority.High:
      return 'priority-high';
    case TaskPriority.Medium:
      return 'priority-medium';
    case TaskPriority.Low:
    default:
      return 'priority-low';
  }
}

export function getTaskStatusLabel(status: TaskStatus): string {
  switch (status) {
    case TaskStatus.Completed:
      return 'Completed';
    case TaskStatus.PendingReview:
      return 'Pending Review';
    case TaskStatus.Blocked:
      return 'Blocked';
    case TaskStatus.Ready:
      return 'Ready';
    case TaskStatus.InProgress:
      return 'In Progress';
    case TaskStatus.NotStarted:
    default:
      return 'Not Started';
  }
}

export function getTaskStatusClass(status: TaskStatus): string {
  switch (status) {
    case TaskStatus.Completed:
      return 'status-completed';
    case TaskStatus.PendingReview:
      return 'status-pending-review';
    case TaskStatus.Blocked:
      return 'status-blocked';
    case TaskStatus.Ready:
      return 'status-ready';
    case TaskStatus.InProgress:
      return 'status-in-progress';
    case TaskStatus.NotStarted:
    default:
      return 'status-not-started';
  }
}

export function deriveTaskStatusFromCompletion(completion: number): TaskStatus {
  if (completion >= 100) {
    return TaskStatus.PendingReview;
  }

  if (completion > 0) {
    return TaskStatus.InProgress;
  }

  return TaskStatus.NotStarted;
}

export function completionForStatus(status: TaskStatus, currentCompletion: number): number {
  switch (status) {
    case TaskStatus.Completed:
    case TaskStatus.PendingReview:
      return 100;
    case TaskStatus.NotStarted:
      return 0;
    case TaskStatus.InProgress:
      return currentCompletion > 0 && currentCompletion < 100 ? currentCompletion : 50;
    default:
      return currentCompletion;
  }
}

export const ENGINEER_MAX_MANUAL_PROGRESS = 90;

export function engineerProgressOptions(): number[] {
  return [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90];
}

export function toUpdateTaskPayload(
  task: Task,
  overrides: Partial<{
    title: string;
    description: string;
    priority: TaskPriority;
    completionPercentage: number;
    status: TaskStatus;
    startDate: string;
    dueDate: string;
  }> = {}
): UpdateTask {
  const completion = overrides.completionPercentage ?? task.completionPercentage;

  return {
    title: overrides.title ?? task.title,
    description: overrides.description ?? task.description,
    priority: overrides.priority ?? task.priority,
    completionPercentage: completion,
    status: overrides.status ?? deriveTaskStatusFromCompletion(completion),
    startDate: overrides.startDate ?? task.startDate,
    dueDate: overrides.dueDate ?? task.dueDate
  };
}
