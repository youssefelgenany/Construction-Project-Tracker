import { TaskPriority } from '../enums/task-priority';
import { TaskStatus } from '../enums/task-status';
import { TaskDependency, TaskPrerequisite } from './task-dependency.model';

export interface TaskCompletionApprovalHistory {
  id: number;
  action: string;
  reviewedBy: string;
  reviewedAt: string;
  rejectionReason?: string | null;
}

export interface TaskCompletionReport {
  id: number;
  taskId: number;
  originalFileName: string;
  extension: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  uploadedBy: string;
  approvalStatus?: string;
  reviewedBy?: string | null;
  reviewedAt?: string | null;
  rejectedBy?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  rejectionComment?: string | null;
  approvalHistory?: TaskCompletionApprovalHistory[];
}

export interface Task {
  id: number;
  projectId: number;
  projectName: string;
  assignedEngineerId?: number | null;
  engineerName?: string | null;
  title: string;
  description: string;
  priority: TaskPriority;
  completionPercentage: number;
  status: TaskStatus;
  startDate: string;
  dueDate: string;
  completionReport?: TaskCompletionReport | null;
  dependencyCount?: number;
  dependencies?: TaskDependency[];
  incompletePrerequisites?: TaskPrerequisite[];
}

export interface TaskDetails extends Task {
  updatedAt: string;
  project?: {
    id: number;
    name: string;
  };
}

export interface CreateTask {
  projectId: number;
  assignedEngineerId: number;
  title: string;
  description: string;
  priority: TaskPriority;
  startDate: string;
  dueDate: string;
}

export interface UpdateTask {
  title: string;
  description: string;
  priority: TaskPriority;
  completionPercentage: number;
  status: TaskStatus;
  startDate: string;
  dueDate: string;
}

export interface TaskQueryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  projectId?: number;
  engineerId?: number;
  priority?: TaskPriority;
  status?: TaskStatus;
  sortBy?: string;
  descending?: boolean;
}

export interface RejectCompletionReport {
  comment: string;
}
