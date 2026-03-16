import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, finalize, switchMap, take, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

let isRefreshingToken = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

/**
 * Appends JWT bearer tokens to outgoing API requests when available.
 */
export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();
  const isAuthEndpoint = request.url.includes('/api/auth/login')
    || request.url.includes('/api/auth/register')
    || request.url.includes('/api/auth/refresh');

  const requestWithAuth = !token || request.headers.has('Authorization') || isAuthEndpoint
    ? request
    : request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

  return next(requestWithAuth).pipe(
    catchError((error) => {
      const refreshToken = authService.getRefreshToken();
      if (error.status !== 401 || isAuthEndpoint || !token || !refreshToken) {
        return throwError(() => error);
      }

      if (isRefreshingToken) {
        return refreshTokenSubject.pipe(
          filter((newToken: string | null): newToken is string => newToken !== null),
          take(1),
          switchMap((newToken: string) => newToken.length > 0
            ? next(
                request.clone({
                  setHeaders: {
                    Authorization: `Bearer ${newToken}`
                  }
                })
              )
            : throwError(() => error))
        );
      }

      isRefreshingToken = true;
      refreshTokenSubject.next(null);

      return authService.refresh({ token, refreshToken }).pipe(
        switchMap((response) => {
          refreshTokenSubject.next(response.token);

          return next(
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${response.token}`
              }
            })
          );
        }),
        catchError((refreshError) => {
          refreshTokenSubject.next('');
          authService.logout();
          return throwError(() => refreshError);
        }),
        finalize(() => {
          isRefreshingToken = false;
        })
      );
    })
  );
};
