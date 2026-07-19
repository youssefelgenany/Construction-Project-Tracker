import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Document } from '../models/document';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/documents`;

  getByProject(projectId: number): Observable<Document[]> {
    return this.http.get<Document[]>(`${this.apiUrl}/project/${projectId}`);
  }

  upload(projectId: number, file: File, category: string): Observable<HttpEvent<Document>> {
    const formData = new FormData();
    formData.append('projectId', String(projectId));
    formData.append('category', category);
    formData.append('file', file);

    return this.http.post<Document>(`${this.apiUrl}/upload`, formData, {
      reportProgress: true,
      observe: 'events'
    });
  }

  download(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download/${id}`, { responseType: 'blob' });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
