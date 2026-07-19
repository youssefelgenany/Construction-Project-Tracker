export interface ReportProjectRow {
  projectId: number;
  projectName: string;
  manager: string | null;
  progressPercentage: number;
  openTasks: number;
  completedTasks: number;
  overdueTasks: number;
  documentsCount: number;
  engineersAssigned: number;
}
