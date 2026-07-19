import { TaskStatus } from '../../core/enums/task-status';
import { Task } from '../../core/models/task';

export type EngineerTaskDueGroup = 'overdue' | 'today' | 'upcoming';

export function getEngineerTaskDueGroup(task: Task, today = startOfDay(new Date())): EngineerTaskDueGroup {
  if (task.status === TaskStatus.Completed) {
    return 'upcoming';
  }

  const due = startOfDay(new Date(task.dueDate));

  if (due < today) {
    return 'overdue';
  }

  if (due.getTime() === today.getTime()) {
    return 'today';
  }

  return 'upcoming';
}

export function sortEngineerTasks(tasks: Task[]): Task[] {
  const today = startOfDay(new Date());
  const groupOrder: Record<EngineerTaskDueGroup, number> = {
    overdue: 0,
    today: 1,
    upcoming: 2
  };

  return [...tasks].sort((a, b) => {
    const groupDiff =
      groupOrder[getEngineerTaskDueGroup(a, today)] - groupOrder[getEngineerTaskDueGroup(b, today)];

    if (groupDiff !== 0) {
      return groupDiff;
    }

    return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
  });
}

export function getEngineerTaskDueGroupLabel(group: EngineerTaskDueGroup): string {
  switch (group) {
    case 'overdue':
      return 'Overdue';
    case 'today':
      return 'Due Today';
    case 'upcoming':
    default:
      return 'Upcoming';
  }
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}
