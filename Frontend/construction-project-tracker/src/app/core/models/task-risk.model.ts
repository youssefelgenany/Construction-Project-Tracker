import { RiskLevel } from '../enums/risk-level';
import { TaskPriority } from '../enums/task-priority';
import { TaskStatus } from '../enums/task-status';

export interface TaskRisk {
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
  completionReport?: {
    id: number;
    taskId: number;
    originalFileName: string;
    extension: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    uploadedBy: string;
    rejectionComment?: string | null;
  } | null;
  riskLevel: RiskLevel;
  reason: string;
  suggestedAction: string;
  isOverdue: boolean;
  daysOverdue: number;
}
