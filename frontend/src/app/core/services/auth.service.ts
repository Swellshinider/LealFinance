import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap, timeout } from 'rxjs';

/**
 * Registration request payload.
 */
export interface RegisterRequest {
  /** User e-mail address. */
  email: string;
  /** User plain password. */
  password: string;
}

/**
 * Login request payload.
 */
export interface LoginRequest {
  /** User e-mail address. */
  email: string;
  /** User plain password. */
  password: string;
  /** Indicates whether the session should be remembered. */
  rememberMe: boolean;
}

/**
 * Refresh token request payload.
 */
export interface RefreshTokenRequest {
  /** Expired JWT token. */
  token: string;
  /** Active refresh token. */
  refreshToken: string;
}

/**
 * Authentication response payload.
 */
export interface AuthResponse {
  /** Signed JWT token. */
  token: string;
  /** Token expiration timestamp (UTC). */
  expiresAtUtc: string;
  /** Authenticated e-mail. */
  email: string;
  /** Issued refresh token when remember-me is enabled. */
  refreshToken: string | null;
  /** Refresh token expiration timestamp (UTC). */
  refreshTokenExpiresAtUtc: string | null;
}

/**
 * Generic message response payload.
 */
export interface MessageResponse {
  /** Message value returned by backend. */
  message: string;
}

/**
 * Manages user authentication and JWT storage.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBaseUrl = 'http://localhost:5216/api/auth';
  private readonly accessTokenStorageKey = 'lealfinance.jwt';
  private readonly refreshTokenStorageKey = 'lealfinance.refresh-token';
  private readonly requestTimeoutMs = 15000;

  public constructor(private readonly httpClient: HttpClient) {}

  /**
   * Registers a user account.
   */
  public register(request: RegisterRequest): Observable<MessageResponse> {
    return this.httpClient
      .post<MessageResponse>(`${this.apiBaseUrl}/register`, request)
      .pipe(timeout(this.requestTimeoutMs));
  }

  /**
   * Logs in a user and stores the JWT.
   */
  public login(request: LoginRequest): Observable<AuthResponse> {
    return this.httpClient
      .post<AuthResponse>(`${this.apiBaseUrl}/login`, request)
      .pipe(timeout(this.requestTimeoutMs))
      .pipe(tap((response: AuthResponse) => this.setTokens(response)));
  }

  /**
   * Refreshes auth tokens with an expired access token and active refresh token.
   */
  public refresh(request: RefreshTokenRequest): Observable<AuthResponse> {
    return this.httpClient
      .post<AuthResponse>(`${this.apiBaseUrl}/refresh`, request)
      .pipe(timeout(this.requestTimeoutMs))
      .pipe(tap((response: AuthResponse) => this.setTokens(response)));
  }

  /**
   * Logs out the current user.
   */
  public logout(): void {
    localStorage.removeItem(this.accessTokenStorageKey);
    localStorage.removeItem(this.refreshTokenStorageKey);
  }

  /**
   * Gets the currently stored JWT.
   */
  public getToken(): string | null {
    return localStorage.getItem(this.accessTokenStorageKey);
  }

  /**
   * Gets the currently stored refresh token.
   */
  public getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenStorageKey);
  }

  /**
   * Returns whether there is a valid, non-expired JWT.
   */
  public isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    const expirationUnix = this.getTokenExpiration(token);
    if (!expirationUnix) {
      return false;
    }

    return expirationUnix * 1000 > Date.now();
  }

  private setTokens(response: AuthResponse): void {
    localStorage.setItem(this.accessTokenStorageKey, response.token);

    if (response.refreshToken) {
      localStorage.setItem(this.refreshTokenStorageKey, response.refreshToken);
      return;
    }

    localStorage.removeItem(this.refreshTokenStorageKey);
  }

  private getTokenExpiration(token: string): number | null {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) {
        return null;
      }

      const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const normalized = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
      const payload = JSON.parse(atob(normalized)) as { exp?: number };

      return typeof payload.exp === 'number' ? payload.exp : null;
    } catch {
      return null;
    }
  }
}
