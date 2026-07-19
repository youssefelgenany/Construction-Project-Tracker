import { TaskStatus } from '../enums/task-status';
import { TaskPriority } from '../enums/task-priority';

export interface TaskDependency {
  id: number;
  taskId: number;
  dependsOnTaskId: number;
  dependsOnTaskTitle: string;
}

export interface CreateTaskDependency {
  dependsOnTaskId: number;
}

export interface TaskPrerequisite {
  taskId: number;
  title: string;
  status: TaskStatus;
  isComplete: boolean;
}

export interface TimelineTask {
  id: number;
  title: string;
  startDate: string;
  dueDate: string;
  completionPercentage: number;
  status: TaskStatus;
  priority: TaskPriority;
  engineerName?: string | null;
  isOverdue: boolean;
  isCritical: boolean;
  isBlocked: boolean;
  dependsOnTaskIds: number[];
  incompletePrerequisites: TaskPrerequisite[];
}

export interface ValidPrerequisiteTask {
  id: number;
  title: string;
  startDate: string;
  dueDate: string;
  status: number;
}
