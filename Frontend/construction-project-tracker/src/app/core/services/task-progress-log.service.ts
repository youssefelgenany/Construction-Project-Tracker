import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTaskProgressLog, TaskProgressLog } from '../models/task-progress-log';

@Injectable({ providedIn: 'root' })
export class TaskProgressLogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/tasks`;

  getByTaskId(taskId: number): Observable<TaskProgressLog[]> {
    return this.http.get<TaskProgressLog[]>(`${this.apiUrl}/${taskId}/progress-log`);
  }

  create(taskId: number, payload: CreateTaskProgressLog): Observable<TaskProgressLog> {
    return this.http.post<TaskProgressLog>(`${this.apiUrl}/${taskId}/progress-log`, payload);
  }
}
