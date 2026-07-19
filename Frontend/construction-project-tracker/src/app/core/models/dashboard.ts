import { ProjectStatus } from '../enums/project-status';
import { EngineerPerformance } from './engineer-performance.model';
import { EngineerWorkload } from './engineer-workload.model';

export type { EngineerPerformance, EngineerWorkload };

export interface DashboardSummary {
  totalProjects: number;
  activeProjects: number;
  completedProjects: number;
  notStartedProjects: number;
  totalEngineers: number;
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  averageProjectProgress: number;
  totalDocuments: number;
}

export interface ProjectProgressChart {
  projectName: string;
  progressPercentage: number;
  status: ProjectStatus;
}

export interface ProjectStatusDistribution {
  completed: number;
  inProgress: number;
  notStarted: number;
}

export interface MonthlyProjects {
  month: string;
  projectsCreated: number;
}

export interface RecentActivity {
  activityType: string;
  description: string;
  createdAt: string;
}

export interface EngineerDashboardData {
  summary: DashboardSummary;
  projectProgress: ProjectProgressChart[];
  engineerWorkload: EngineerWorkload[];
  topPerformers: EngineerPerformance[];
}

export interface AdminDashboardData extends EngineerDashboardData {
  projectStatus: ProjectStatusDistribution;
  monthlyProjects: MonthlyProjects[];
  recentActivities: RecentActivity[];
}
