import { ProjectStatus } from '../enums/project-status';
import { RiskLevel } from '../enums/risk-level';

export interface ProjectRisk {
  id: number;
  name: string;
  description: string;
  budget: number;
  startDate: string;
  endDate: string;
  status: ProjectStatus;
  progressPercentage: number;
  riskLevel: RiskLevel;
  reason: string;
  suggestedAction: string;
  activeTaskCount: number;
  atRiskTaskCount: number;
  overdueTaskCount: number;
  hasCriticalOverdueTask: boolean;
}
