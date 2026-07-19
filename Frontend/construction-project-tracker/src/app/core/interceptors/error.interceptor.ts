import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const notificationService = inject(NotificationService);
  const router = inject(Router);
  const isApiRequest = req.url.startsWith(environment.apiUrl);
  const isLoginRequest = req.url.includes('/auth/login');

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (!isApiRequest) {
        return throwError(() => error);
      }

      const message = getErrorMessage(error);

      if (error.status === 401) {
        if (!isLoginRequest) {
          authService.logout();
          router.navigate(['/auth/login']);
          notificationService.error(message);
        }

        return throwError(() => error);
      }

      if ([400, 403, 404, 500].includes(error.status) && !isLoginRequest) {
        notificationService.error(message);
      }

      return throwError(() => error);
    })
  );
};

function getErrorMessage(error: HttpErrorResponse): string {
  const body = error.error;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body?.message) {
    return body.message;
  }

  if (body?.title) {
    return body.title;
  }

  if (Array.isArray(body?.errors)) {
    return body.errors.join(', ');
  }

  if (typeof body?.errors === 'object' && body.errors !== null) {
    const messages = Object.values(body.errors).flat();
    if (messages.length) {
      return messages.join(', ');
    }
  }

  switch (error.status) {
    case 400:
      return 'The request could not be processed. Please check your input.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 500:
      return 'An unexpected server error occurred. Please try again later.';
    default:
      return 'An unexpected error occurred. Please try again.';
  }
}
