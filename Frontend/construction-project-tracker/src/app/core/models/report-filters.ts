import { ProjectStatus } from '../enums/project-status';
import { RiskLevel } from '../enums/risk-level';

export interface ReportFilters {
  startDate?: string | null;
  endDate?: string | null;
  projectId?: number | null;
  engineerId?: number | null;
  status?: ProjectStatus | null;
  riskLevel?: RiskLevel | null;
}
