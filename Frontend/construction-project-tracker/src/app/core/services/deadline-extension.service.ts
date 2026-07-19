import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminExtendProjectDeadline,
  AdminExtendTaskDeadline,
  ApplyTaskDeadlineExtension,
  ApplyTaskDeadlineExtensionResult,
  CreateProjectDeadlineExtensionRequest,
  CreateTaskDeadlineExtensionRequest,
  DeadlineExtensionRequest,
  ExtensionRequestStatus,
  ProjectDeadlineHistory,
  ReviewDeadlineExtension,
  ScheduleImpactAnalysis,
  TaskDeadlineHistory
} from '../models/deadline-extension.model';

@Injectable({ providedIn: 'root' })
export class DeadlineExtensionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  createTaskRequest(
    taskId: number,
    body: CreateTaskDeadlineExtensionRequest
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/tasks/${taskId}/deadline-extension-requests`,
      body
    );
  }

  createProjectRequest(
    projectId: number,
    body: CreateProjectDeadlineExtensionRequest
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/projects/${projectId}/deadline-extension-requests`,
      body
    );
  }

  getLatestTaskRequest(taskId: number): Observable<DeadlineExtensionRequest | null> {
    return this.http.get<DeadlineExtensionRequest | null>(
      `${this.apiUrl}/tasks/${taskId}/deadline-extension-requests/latest`
    );
  }

  getLatestProjectRequest(projectId: number): Observable<DeadlineExtensionRequest | null> {
    return this.http.get<DeadlineExtensionRequest | null>(
      `${this.apiUrl}/projects/${projectId}/deadline-extension-requests/latest`
    );
  }

  getAdminRequests(status?: ExtensionRequestStatus | null): Observable<DeadlineExtensionRequest[]> {
    let params = new HttpParams();
    if (status !== null && status !== undefined) {
      params = params.set('status', String(status));
    }
    return this.http.get<DeadlineExtensionRequest[]>(
      `${this.apiUrl}/deadline-extension-requests`,
      { params }
    );
  }

  approveTaskRequest(
    requestId: number,
    body: ReviewDeadlineExtension = {}
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/deadline-extension-requests/tasks/${requestId}/approve`,
      {
        adminComment: body.adminComment ?? null,
        confirmProjectExtension: body.confirmProjectExtension ?? false
      }
    );
  }

  rejectTaskRequest(
    requestId: number,
    body: ReviewDeadlineExtension
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/deadline-extension-requests/tasks/${requestId}/reject`,
      body
    );
  }

  approveProjectRequest(
    requestId: number,
    body: ReviewDeadlineExtension = {}
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/deadline-extension-requests/projects/${requestId}/approve`,
      body
    );
  }

  rejectProjectRequest(
    requestId: number,
    body: ReviewDeadlineExtension
  ): Observable<DeadlineExtensionRequest> {
    return this.http.post<DeadlineExtensionRequest>(
      `${this.apiUrl}/deadline-extension-requests/projects/${requestId}/reject`,
      body
    );
  }

  analyzeTaskDeadlineExtension(
    taskId: number,
    body: AdminExtendTaskDeadline
  ): Observable<ScheduleImpactAnalysis> {
    return this.http.post<ScheduleImpactAnalysis>(
      `${this.apiUrl}/tasks/${taskId}/extend-deadline/analyze`,
      body
    );
  }

  applyTaskDeadlineExtension(
    taskId: number,
    body: ApplyTaskDeadlineExtension
  ): Observable<ApplyTaskDeadlineExtensionResult> {
    return this.http.post<ApplyTaskDeadlineExtensionResult>(
      `${this.apiUrl}/tasks/${taskId}/extend-deadline/apply`,
      body
    );
  }

  extendTaskDeadline(
    taskId: number,
    body: AdminExtendTaskDeadline
  ): Observable<ApplyTaskDeadlineExtensionResult> {
    return this.http.post<ApplyTaskDeadlineExtensionResult>(
      `${this.apiUrl}/tasks/${taskId}/extend-deadline`,
      body
    );
  }

  extendProjectDeadline(
    projectId: number,
    body: AdminExtendProjectDeadline
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/projects/${projectId}/extend-deadline`,
      body
    );
  }

  getTaskHistory(taskId: number): Observable<TaskDeadlineHistory[]> {
    return this.http.get<TaskDeadlineHistory[]>(`${this.apiUrl}/tasks/${taskId}/deadline-history`);
  }

  getProjectHistory(projectId: number): Observable<ProjectDeadlineHistory[]> {
    return this.http.get<ProjectDeadlineHistory[]>(
      `${this.apiUrl}/projects/${projectId}/deadline-history`
    );
  }
}
