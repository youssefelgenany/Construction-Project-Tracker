import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ReportFilters } from '../models/report-filters';
import {
  AttentionProject,
  EngineerPerformanceReportRow,
  ExecutiveSummary,
  ProjectHealth,
  ProjectProgressPoint,
  ReportActivity,
  TaskAnalytics,
  WorkloadBar
} from '../models/executive-reports.model';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/reports`;

  getExecutiveSummary(filters: ReportFilters): Observable<ExecutiveSummary> {
    return this.http.get<ExecutiveSummary>(`${this.apiUrl}/executive-summary`, {
      params: this.toParams(filters)
    });
  }

  getProjectHealth(filters: ReportFilters): Observable<ProjectHealth> {
    return this.http.get<ProjectHealth>(`${this.apiUrl}/project-health`, {
      params: this.toParams(filters)
    });
  }

  getProjectProgressTimeline(filters: ReportFilters): Observable<ProjectProgressPoint[]> {
    return this.http.get<ProjectProgressPoint[]>(`${this.apiUrl}/project-progress`, {
      params: this.toParams(filters)
    });
  }

  getEngineerPerformance(filters: ReportFilters): Observable<EngineerPerformanceReportRow[]> {
    return this.http.get<EngineerPerformanceReportRow[]>(`${this.apiUrl}/engineer-performance`, {
      params: this.toParams(filters)
    });
  }

  getWorkload(filters: ReportFilters): Observable<WorkloadBar[]> {
    return this.http.get<WorkloadBar[]>(`${this.apiUrl}/workload`, {
      params: this.toParams(filters)
    });
  }

  getTaskAnalytics(filters: ReportFilters): Observable<TaskAnalytics> {
    return this.http.get<TaskAnalytics>(`${this.apiUrl}/task-analytics`, {
      params: this.toParams(filters)
    });
  }

  getActivity(filters: ReportFilters): Observable<ReportActivity[]> {
    return this.http.get<ReportActivity[]>(`${this.apiUrl}/activity`, {
      params: this.toParams(filters)
    });
  }

  getAttention(filters: ReportFilters): Observable<AttentionProject[]> {
    return this.http.get<AttentionProject[]>(`${this.apiUrl}/attention`, {
      params: this.toParams(filters)
    });
  }

  exportExcel(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/excel`, {
      params: this.toParams(filters),
      responseType: 'blob'
    });
  }

  exportPdf(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/pdf`, {
      params: this.toParams(filters),
      responseType: 'blob'
    });
  }

  private toParams(filters: ReportFilters): HttpParams {
    let params = new HttpParams();

    if (filters.startDate) {
      params = params.set('startDate', filters.startDate);
    }
    if (filters.endDate) {
      params = params.set('endDate', filters.endDate);
    }
    if (filters.projectId != null) {
      params = params.set('projectId', String(filters.projectId));
    }
    if (filters.engineerId != null) {
      params = params.set('engineerId', String(filters.engineerId));
    }
    if (filters.status != null) {
      params = params.set('status', String(filters.status));
    }
    if (filters.riskLevel != null) {
      params = params.set('riskLevel', String(filters.riskLevel));
    }

    return params;
  }
}
