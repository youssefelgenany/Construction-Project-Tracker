import { PerformanceTier } from '../enums/performance-tier';

export interface EngineerPerformance {
  engineerId: number;
  engineerName: string;
  email: string;
  position: string;
  isActive: boolean;
  totalProjectsWorkedOn: number;
  projectsCompleted: number;
  totalTasksAssigned: number;
  totalTasksCompleted: number;
  completionRate: number;
  tasksFinishedBeforeDeadline: number;
  tasksFinishedLate: number;
  onTimeCompletionRate: number;
  lateRate: number;
  averageDaysEarlyLate: number;
  averageTaskDuration: number;
  averageProgressUpdatesPerTask: number;
  totalCompletionReportsSubmitted: number;
  currentActiveTasks: number;
  performanceScore: number;
  performanceTier: PerformanceTier;
}

export interface EngineerPerformanceTrendPoint {
  label: string;
  year: number;
  month: number;
  completedTasks: number;
  onTimeRate: number;
  performanceScore: number;
}

export interface EngineerCompletedTaskHistory {
  taskId: number;
  taskTitle: string;
  projectId: number;
  projectName: string;
  completedAt: string;
  dueDate: string;
  finishedOnTime: boolean;
  daysEarlyLate: number;
  durationDays: number;
  progressUpdates: number;
}

export interface EngineerCompletionReportHistory {
  reportId: number;
  taskId: number;
  taskTitle: string;
  projectId: number;
  projectName: string;
  originalFileName: string;
  uploadedAt: string;
  reviewedAt: string | null;
  reviewStatus: string;
}

export interface EngineerPerformanceDetails {
  summary: EngineerPerformance;
  trend: EngineerPerformanceTrendPoint[];
  recentCompletedTasks: EngineerCompletedTaskHistory[];
  recentCompletionReports: EngineerCompletionReportHistory[];
}
