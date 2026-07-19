import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RiskLevel } from '../enums/risk-level';
import { CreateTask, Task, TaskDetails, TaskQueryParams, UpdateTask } from '../models/task';
import { PagedResult } from '../models/paged-result';
import { CreateTaskDependency, TaskDependency, ValidPrerequisiteTask } from '../models/task-dependency.model';
import { TaskRisk } from '../models/task-risk.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/tasks`;

  getAll(params: TaskQueryParams = {}): Observable<PagedResult<Task>> {
    return this.http.get<PagedResult<Task>>(this.apiUrl, { params: this.toParams(params) });
  }

  getMyTasks(params: Omit<TaskQueryParams, 'projectId' | 'engineerId' | 'assignedToMe'> = {}): Observable<PagedResult<Task>> {
    return this.http.get<PagedResult<Task>>(`${this.apiUrl}/my`, { params: this.toParams(params) });
  }

  getRiskTasks(
    params: TaskQueryParams & { riskLevel?: RiskLevel | null } = {}
  ): Observable<PagedResult<TaskRisk>> {
    return this.http.get<PagedResult<TaskRisk>>(`${this.apiUrl}/at-risk`, { params: this.toParams(params) });
  }

  getProjectTasks(projectId: number, pageSize = 100): Observable<PagedResult<Task>> {
    const params = new HttpParams()
      .set('pageNumber', '1')
      .set('pageSize', String(pageSize));

    return this.http.get<PagedResult<Task>>(
      `${environment.apiUrl}/projects/${projectId}/tasks`,
      { params }
    );
  }

  getById(id: number): Observable<TaskDetails> {
    return this.http.get<TaskDetails>(`${this.apiUrl}/${id}`);
  }

  create(task: CreateTask): Observable<Task> {
    return this.http.post<Task>(this.apiUrl, task);
  }

  update(id: number, task: UpdateTask): Observable<Task> {
    return this.http.put<Task>(`${this.apiUrl}/${id}`, task);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getDependencies(taskId: number): Observable<TaskDependency[]> {
    return this.http.get<TaskDependency[]>(`${this.apiUrl}/${taskId}/dependencies`);
  }

  getValidPrerequisites(taskId: number): Observable<ValidPrerequisiteTask[]> {
    return this.http.get<ValidPrerequisiteTask[]>(`${this.apiUrl}/${taskId}/valid-prerequisites`);
  }

  addDependency(taskId: number, payload: CreateTaskDependency): Observable<TaskDependency> {
    return this.http.post<TaskDependency>(`${this.apiUrl}/${taskId}/dependencies`, payload);
  }

  removeDependency(taskId: number, dependsOnTaskId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${taskId}/dependencies/${dependsOnTaskId}`);
  }

  private toParams(params: TaskQueryParams): HttpParams {
    let httpParams = new HttpParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value != null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });

    return httpParams;
  }
}
