export enum ExtensionRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export type DeadlineRequestType = 'Task' | 'Project';

export interface DeadlineExtensionRequest {
  id: number;
  requestType: DeadlineRequestType;
  taskId: number | null;
  taskTitle: string | null;
  projectId: number;
  projectName: string;
  requestedByUserId: number;
  engineerName: string;
  currentDeadline: string;
  requestedDeadline: string;
  requestedExtraDays: number;
  reason: string;
  status: ExtensionRequestStatus;
  adminComment: string | null;
  reviewedByUserId: number | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

export interface CreateTaskDeadlineExtensionRequest {
  requestedDueDate: string;
  reason: string;
}

export interface CreateProjectDeadlineExtensionRequest {
  requestedEndDate: string;
  reason: string;
}

export interface AdminExtendTaskDeadline {
  newDueDate: string;
  reason: string;
}

export interface ApplyTaskDeadlineExtension {
  newDueDate: string;
  reason: string;
  confirmProjectExtension: boolean;
}

export interface AdminExtendProjectDeadline {
  newEndDate: string;
  reason: string;
}

export interface ReviewDeadlineExtension {
  adminComment?: string | null;
  confirmProjectExtension?: boolean;
}

export interface ScheduleImpactTask {
  taskId: number;
  title: string;
  currentStart: string;
  currentDue: string;
  newStart: string;
  newDue: string;
  daysShifted: number;
  assignedEngineerUserId: number | null;
  engineerName: string | null;
}

export interface ScheduleImpactAnalysis {
  sourceTaskId: number;
  sourceTaskTitle: string;
  currentDueDate: string;
  proposedDueDate: string;
  reason: string;
  hasConflicts: boolean;
  affectedTasks: ScheduleImpactTask[];
  affectedTaskCount: number;
  totalShiftWorkingDays: number;
  latestTaskDueDate: string;
  currentProjectEnd: string;
  newRequiredProjectEnd: string;
  requiresProjectExtension: boolean;
  projectId: number;
  projectName: string;
}

export interface ApplyTaskDeadlineExtensionResult {
  message: string;
  shiftedTaskCount: number;
  projectExtended: boolean;
  newProjectEndDate: string | null;
}

export interface TaskDeadlineHistory {
  id: number;
  taskId: number;
  previousStartDate: string | null;
  newStartDate: string | null;
  previousDueDate: string;
  newDueDate: string;
  reason: string;
  changedByUserId: number;
  changedByName: string;
  changedAt: string;
  isAutomatic: boolean;
}

export interface ProjectDeadlineHistory {
  id: number;
  projectId: number;
  previousEndDate: string;
  newEndDate: string;
  reason: string;
  changedByUserId: number;
  changedByName: string;
  changedAt: string;
  isAutomatic: boolean;
}
