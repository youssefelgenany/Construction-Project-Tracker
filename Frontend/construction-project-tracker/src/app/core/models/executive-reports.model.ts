import { RiskLevel } from '../enums/risk-level';
import { WorkloadLevel } from '../enums/workload-level';

export interface ExecutiveSummary {
  totalProjects: number;
  healthyProjects: number;
  atRiskProjects: number;
  delayedProjects: number;
  totalEngineers: number;
  activeEngineers: number;
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  averageProjectCompletion: number;
  onTimeCompletionRate: number;
  averageEngineerWorkload: number;
}

export interface ProjectHealth {
  healthy: number;
  atRisk: number;
  critical: number;
  completed: number;
  total: number;
}

export interface ProjectProgressPoint {
  label: string;
  year: number;
  month: number;
  averageCompletionPercent: number;
}

export interface EngineerPerformanceReportRow {
  engineerId: number;
  engineerName: string;
  projects: number;
  completedTasks: number;
  averageCompletionPercent: number;
  onTimeRate: number;
  overdueTasks: number;
  currentWorkloadPercent: number;
  currentWorkloadLevel: WorkloadLevel;
  averageDelayDays: number;
  performanceScore: number;
  isTopPerformer: boolean;
}

export interface WorkloadBar {
  engineerId: number;
  engineerName: string;
  activeTasks: number;
  overdueTasks: number;
  workloadPercent: number;
}

export interface TaskAnalytics {
  byPriority: {
    low: number;
    medium: number;
    high: number;
    critical: number;
  };
  byStatus: {
    notStarted: number;
    inProgress: number;
    pendingReview: number;
    completed: number;
    blocked: number;
    ready: number;
  };
  overdueVsCompleted: {
    overdue: number;
    completed: number;
  };
  completionTrend: MonthlyCountPoint[];
}

export interface MonthlyCountPoint {
  label: string;
  year: number;
  month: number;
  count: number;
}

export interface ReportActivity {
  time: string;
  user: string;
  action: string;
  projectName?: string | null;
  taskTitle?: string | null;
  projectId?: number | null;
  taskId?: number | null;
}

export interface AttentionProject {
  projectId: number;
  projectName: string;
  riskLevel: RiskLevel;
  completionPercent: number;
  overdueTasks: number;
  assignedEngineers: string[];
  reason: string;
  predictionState?: number | null;
}
