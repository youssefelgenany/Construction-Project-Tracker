import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RiskLevel } from '../enums/risk-level';
import { CreateProject, Project, ProjectDetails, UpdateProject } from '../models/project';
import { PagedResult, PaginationParams } from '../models/paged-result';
import { ProjectRisk } from '../models/project-risk.model';
import { ProjectDelayPrediction } from '../models/project-delay-prediction.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/projects`;

  getAll(params: PaginationParams = {}): Observable<PagedResult<Project>> {
    return this.http.get<PagedResult<Project>>(this.apiUrl, { params: this.toParams(params) });
  }

  getById(id: number): Observable<ProjectDetails> {
    return this.http.get<ProjectDetails>(`${this.apiUrl}/${id}`);
  }

  getDelayPrediction(id: number): Observable<ProjectDelayPrediction> {
    return this.http.get<ProjectDelayPrediction>(`${this.apiUrl}/${id}/delay-prediction`);
  }

  getRiskProjects(
    params: PaginationParams & { riskLevel?: RiskLevel | null } = {}
  ): Observable<PagedResult<ProjectRisk>> {
    return this.http.get<PagedResult<ProjectRisk>>(`${this.apiUrl}/at-risk`, { params: this.toParams(params) });
  }

  create(project: CreateProject): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, project);
  }

  update(id: number, project: UpdateProject): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}`, project);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  private toParams(params: PaginationParams & { riskLevel?: RiskLevel | null }): HttpParams {
    let httpParams = new HttpParams();

    if (params.pageNumber != null) {
      httpParams = httpParams.set('pageNumber', params.pageNumber);
    }
    if (params.pageSize != null) {
      httpParams = httpParams.set('pageSize', params.pageSize);
    }
    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }
    if (params.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }
    if (params.descending != null) {
      httpParams = httpParams.set('descending', params.descending);
    }
    if (params.riskLevel != null) {
      httpParams = httpParams.set('riskLevel', params.riskLevel);
    }

    return httpParams;
  }
}
