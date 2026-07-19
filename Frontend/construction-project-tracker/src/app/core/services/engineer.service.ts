import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateEngineer, Engineer, EngineerDetails, UpdateEngineer } from '../models/engineer';
import {
  EngineerPerformance,
  EngineerPerformanceDetails
} from '../models/engineer-performance.model';
import { EngineerWorkload } from '../models/engineer-workload.model';
import { PagedResult, PaginationParams } from '../models/paged-result';

export interface EngineerWorkloadParams extends PaginationParams {
  workloadLevel?: string;
}

export interface EngineerPerformanceParams extends PaginationParams {
  sortBy?: string;
  descending?: boolean;
}

@Injectable({ providedIn: 'root' })
export class EngineerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/engineers`;

  getAll(params: PaginationParams = {}): Observable<PagedResult<Engineer>> {
    return this.http.get<PagedResult<Engineer>>(this.apiUrl, { params: this.toParams(params) });
  }

  getWorkload(params: EngineerWorkloadParams = {}): Observable<PagedResult<EngineerWorkload>> {
    let httpParams = this.toParams(params);

    if (params.workloadLevel) {
      httpParams = httpParams.set('workloadLevel', params.workloadLevel);
    }

    return this.http.get<PagedResult<EngineerWorkload>>(`${this.apiUrl}/workload`, {
      params: httpParams
    });
  }

  getPerformance(params: EngineerPerformanceParams = {}): Observable<PagedResult<EngineerPerformance>> {
    return this.http.get<PagedResult<EngineerPerformance>>(`${this.apiUrl}/performance`, {
      params: this.toParams(params)
    });
  }

  getPerformanceById(id: number): Observable<EngineerPerformanceDetails> {
    return this.http.get<EngineerPerformanceDetails>(`${this.apiUrl}/${id}/performance`);
  }

  getById(id: number): Observable<EngineerDetails> {
    return this.http.get<EngineerDetails>(`${this.apiUrl}/${id}`);
  }

  create(engineer: CreateEngineer): Observable<Engineer> {
    return this.http.post<Engineer>(this.apiUrl, engineer);
  }

  update(id: number, engineer: UpdateEngineer): Observable<Engineer> {
    return this.http.put<Engineer>(`${this.apiUrl}/${id}`, engineer);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  private toParams(params: PaginationParams): HttpParams {
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

    return httpParams;
  }
}
