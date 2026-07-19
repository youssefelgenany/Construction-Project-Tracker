import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth';
import { User } from '../models/user';
import { UserRole, parseUserRole } from '../enums/user-role';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly tokenKey = 'cpt_token';
  private readonly userKey = 'cpt_user';

  private readonly token = signal<string | null>(this.readTokenFromStorage());
  private readonly currentUser = signal<User | null>(this.readUserFromStorage());

  /** Reactive JWT — always read this (or getToken()) instead of localStorage. */
  readonly accessToken = this.token.asReadonly();
  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.token() !== null);
  readonly isAdmin = computed(
    () => parseUserRole(this.currentUser()?.role ?? '') === UserRole.Admin
  );

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => this.setSession(response))
    );
  }

  logout(): void {
    this.clearSession();
  }

  getToken(): string | null {
    return this.token();
  }

  getCurrentUser(): User | null {
    return this.currentUser();
  }

  private setSession(response: LoginResponse): void {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
    this.token.set(response.token);
    this.currentUser.set(response.user);
  }

  private clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.token.set(null);
    this.currentUser.set(null);
  }

  private readTokenFromStorage(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  private readUserFromStorage(): User | null {
    const stored = localStorage.getItem(this.userKey);
    if (!stored) {
      return null;
    }

    try {
      return JSON.parse(stored) as User;
    } catch {
      return null;
    }
  }
}
