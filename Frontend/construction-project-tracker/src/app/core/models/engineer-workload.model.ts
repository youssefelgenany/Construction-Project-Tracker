import { WorkloadLevel } from '../enums/workload-level';

export interface EngineerWorkload {
  engineerId: number;
  engineerName: string;
  email: string;
  position: string;
  phoneNumber: string;
  hireDate: string;
  isActive: boolean;
  totalAssignedProjects: number;
  activeProjects: number;
  totalAssignedTasks: number;
  activeTasks: number;
  completedTasks: number;
  pendingReviewTasks: number;
  overdueTasks: number;
  averageProgress: number;
  earliestUpcomingDeadline: string | null;
  workloadLevel: WorkloadLevel;
  assignedProjects: number;
  assignedTasks: number;
  pendingTasks: number;
  completionRate: number;
}

export type WorkloadFilter = 'all' | 'low' | 'medium' | 'high';
