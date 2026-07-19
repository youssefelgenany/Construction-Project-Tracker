import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AssignEngineer, Assignment } from '../models/assignment';
import { ProjectAssignedEngineer } from '../models/project-assigned-engineer';
import { Project } from '../models/project';

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/project-assignments`;

  assign(dto: AssignEngineer): Observable<Assignment> {
    return this.http.post<Assignment>(this.apiUrl, dto);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getByProject(projectId: number): Observable<ProjectAssignedEngineer[]> {
    return this.http.get<ProjectAssignedEngineer[]>(`${this.apiUrl}/project/${projectId}`);
  }

  getByEngineer(engineerId: number): Observable<Project[]> {
    return this.http.get<Project[]>(`${this.apiUrl}/engineer/${engineerId}`);
  }

  checkAssignment(projectId: number, engineerId: number): Observable<boolean> {
    const params = new HttpParams()
      .set('projectId', projectId)
      .set('engineerId', engineerId);

    return this.http.get<boolean>(`${this.apiUrl}/check`, { params });
  }
}
