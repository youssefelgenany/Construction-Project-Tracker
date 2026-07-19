import { TaskStatus } from '../enums/task-status';

export interface CriticalPathTask {
  order: number;
  taskId: number;
  title: string;
  startDate: string;
  dueDate: string;
  durationDays: number;
  earlyStartDay: number;
  earlyFinishDay: number;
  lateStartDay: number;
  lateFinishDay: number;
  slackDays: number;
  isCritical: boolean;
  status: TaskStatus;
  completionPercentage: number;
}
