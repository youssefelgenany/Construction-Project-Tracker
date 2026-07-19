import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RejectCompletionReport, Task, TaskCompletionReport } from '../models/task';

@Injectable({ providedIn: 'root' })
export class TaskCompletionReportService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/tasks`;

  upload(taskId: number, file: File): Observable<TaskCompletionReport> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<TaskCompletionReport>(`${this.apiUrl}/${taskId}/completion-report`, formData);
  }

  download(taskId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${taskId}/completion-report/download`, {
      responseType: 'blob'
    });
  }

  approve(taskId: number): Observable<Task> {
    return this.http.post<Task>(`${this.apiUrl}/${taskId}/completion-report/approve`, {});
  }

  reject(taskId: number, payload: RejectCompletionReport): Observable<Task> {
    return this.http.post<Task>(`${this.apiUrl}/${taskId}/completion-report/reject`, payload);
  }
}
