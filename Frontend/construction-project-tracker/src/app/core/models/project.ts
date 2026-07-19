import { ProjectStatus } from '../enums/project-status';

export interface Project {
  id: number;
  name: string;
  description: string;
  budget: number;
  startDate: string;
  endDate: string;
  status: ProjectStatus;
  progressPercentage: number;
}

export interface ProjectDetails extends Project {
  assignedEngineersCount: number;
  tasksCount: number;
  documentsCount: number;
}

export interface CreateProject {
  name: string;
  description: string;
  budget: number;
  startDate: string;
  endDate: string;
}

export interface UpdateProject extends CreateProject {}
