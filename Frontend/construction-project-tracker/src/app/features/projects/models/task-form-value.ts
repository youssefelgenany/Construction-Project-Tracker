import { TaskPriority } from '../../../core/enums/task-priority';

export interface TaskFormValue {
  title: string;
  description: string;
  assignedEngineerId: number;
  priority: TaskPriority;
  startDate: string;
  dueDate: string;
}
