import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProjectTimeline } from '../models/project-timeline.model';
import { CriticalPathTask } from '../models/critical-path-task.model';
import { ScheduleSummary } from '../models/schedule-summary.model';

@Injectable({ providedIn: 'root' })
export class ScheduleService {
  private readonly http = inject(HttpClient);
  private readonly projectsUrl = `${environment.apiUrl}/projects`;
  private readonly dashboardUrl = `${environment.apiUrl}/dashboard`;

  getProjectTimeline(projectId: number): Observable<ProjectTimeline> {
    return this.http.get<ProjectTimeline>(`${this.projectsUrl}/${projectId}/timeline`);
  }

  getCriticalPath(projectId: number): Observable<CriticalPathTask[]> {
    return this.http.get<CriticalPathTask[]>(`${this.projectsUrl}/${projectId}/critical-path`);
  }

  getScheduleSummary(): Observable<ScheduleSummary> {
    return this.http.get<ScheduleSummary>(`${this.dashboardUrl}/schedule-summary`);
  }
}
