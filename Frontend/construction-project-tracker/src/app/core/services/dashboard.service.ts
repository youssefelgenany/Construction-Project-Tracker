import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DashboardSummary,
  EngineerPerformance,
  EngineerWorkload,
  MonthlyProjects,
  ProjectProgressChart,
  ProjectStatusDistribution,
  RecentActivity
} from '../models/dashboard';
import { ProjectRisk } from '../models/project-risk.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/dashboard`;

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/summary`);
  }

  getProjectProgress(): Observable<ProjectProgressChart[]> {
    return this.http.get<ProjectProgressChart[]>(`${this.apiUrl}/project-progress`);
  }

  getEngineerWorkload(): Observable<EngineerWorkload[]> {
    return this.http.get<EngineerWorkload[]>(`${this.apiUrl}/engineer-workload`);
  }

  getTopPerformingEngineers(): Observable<EngineerPerformance[]> {
    return this.http.get<EngineerPerformance[]>(`${this.apiUrl}/top-performing-engineers`);
  }

  getRiskSummary(): Observable<{
    projectsAtRiskCount: number;
    tasksAtRiskCount: number;
    overdueTasksCount: number;
    pendingReviewsCount: number;
    projects: ProjectRisk[];
  }> {
    return this.http.get<{
      projectsAtRiskCount: number;
      tasksAtRiskCount: number;
      overdueTasksCount: number;
      pendingReviewsCount: number;
      projects: ProjectRisk[];
    }>(`${this.apiUrl}/risk-summary`);
  }

  getProjectStatusDistribution(): Observable<ProjectStatusDistribution> {
    return this.http.get<ProjectStatusDistribution>(`${this.apiUrl}/project-status`);
  }

  getMonthlyProjects(): Observable<MonthlyProjects[]> {
    return this.http.get<MonthlyProjects[]>(`${this.apiUrl}/monthly-projects`);
  }

  getRecentActivities(): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>(`${this.apiUrl}/recent-activities`);
  }
}
