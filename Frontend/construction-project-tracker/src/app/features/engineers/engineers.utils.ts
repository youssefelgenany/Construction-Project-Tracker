import { WorkloadLevel } from '../../core/enums/workload-level';

export type CapacityFilter = 'all' | 'available' | 'balanced' | 'busy' | 'overloaded';

export type EngineerListSortField =
  | 'name'
  | 'activeTasks'
  | 'completedTasks'
  | 'performance'
  | 'hireDate';

export const ENGINEER_LIST_SORT_OPTIONS: { field: EngineerListSortField; label: string }[] = [
  { field: 'name', label: 'Name' },
  { field: 'activeTasks', label: 'Active Tasks' },
  { field: 'completedTasks', label: 'Completed Tasks' },
  { field: 'performance', label: 'Performance' },
  { field: 'hireDate', label: 'Hire Date' }
];

export const CAPACITY_FILTER_OPTIONS: { value: CapacityFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'available', label: 'Available' },
  { value: 'balanced', label: 'Balanced' },
  { value: 'busy', label: 'Busy' },
  { value: 'overloaded', label: 'Overloaded' }
];

/** Maps UI capacity labels to API WorkloadLevel query values. */
export function capacityToApiLevel(filter: CapacityFilter): string | undefined {
  switch (filter) {
    case 'available':
      return 'low';
    case 'balanced':
      return 'medium';
    case 'busy':
    case 'overloaded':
      return 'high';
    default:
      return undefined;
  }
}

/** Directory Availability labels: Available / Busy / Overloaded */
export function getAvailabilityLabel(level: WorkloadLevel, overdueTasks = 0): string {
  switch (level) {
    case WorkloadLevel.Low:
      return 'Available';
    case WorkloadLevel.Medium:
      return 'Busy';
    case WorkloadLevel.High:
      return overdueTasks > 0 ? 'Overloaded' : 'Busy';
    default:
      return 'Available';
  }
}

export function getAvailabilityClass(level: WorkloadLevel, overdueTasks = 0): string {
  switch (level) {
    case WorkloadLevel.Low:
      return 'capacity-available';
    case WorkloadLevel.Medium:
      return 'capacity-busy';
    case WorkloadLevel.High:
      return overdueTasks > 0 ? 'capacity-overloaded' : 'capacity-busy';
    default:
      return 'capacity-available';
  }
}

/** @deprecated Prefer getAvailabilityLabel for directory Availability column */
export function getCapacityLabel(level: WorkloadLevel, overdueTasks = 0): string {
  return getAvailabilityLabel(level, overdueTasks);
}

export function getCapacityClass(level: WorkloadLevel, overdueTasks = 0): string {
  return getAvailabilityClass(level, overdueTasks);
}

export function getEngineerInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

export const ENGINEER_SORT_FIELDS = {
  fullName: 'name',
  email: 'email',
  position: 'position',
  hireDate: 'hiredate'
} as const;

export type EngineerSortField = keyof typeof ENGINEER_SORT_FIELDS;

export function getEngineerStatusLabel(isActive: boolean): string {
  return isActive ? 'Active' : 'Inactive';
}

export function getEngineerStatusClass(isActive: boolean): string {
  return isActive ? 'status-active' : 'status-inactive';
}

function readConflictMessage(error: unknown): string {
  if (!error || typeof error !== 'object') {
    return 'This engineer cannot be deleted because they are assigned to projects or tasks.';
  }

  const httpError = error as { error?: unknown; message?: string };

  if (typeof httpError.error === 'string' && httpError.error.trim()) {
    return httpError.error;
  }

  if (httpError.error && typeof httpError.error === 'object') {
    const body = httpError.error as { message?: string; title?: string };
    if (body.message) {
      return body.message;
    }
    if (body.title) {
      return body.title;
    }
  }

  return 'This engineer cannot be deleted because they are assigned to projects or tasks.';
}

export { readConflictMessage as getEngineerDeleteConflictMessage };
