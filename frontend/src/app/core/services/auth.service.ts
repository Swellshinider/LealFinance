import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap, timeout } from 'rxjs';

/**
 * Registration request payload.
 */
export interface RegisterRequest {
  /** User full display name. */
  fullName: string;
  /** User e-mail address. */
  email: string;
  /** User plain password. */
  password: string;
}

/**
 * Authenticated user profile payload.
 */
export interface UserProfile {
  /** User full display name. */
  fullName: string;
  /** User e-mail. */
  email: string;
  /** Optional profile photo URL or data URL. */
  profilePhotoUrl: string | null;
}

/**
 * Profile update request payload.
 */
export interface UpdateProfileRequest {
  /** User full display name. */
  fullName: string;
  /** User e-mail. */
  email: string;
  /** Optional profile photo URL or data URL. */
  profilePhotoUrl: string | null;
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
  /** Authenticated full name. */
  fullName: string;
  /** Optional profile photo URL or data URL. */
  profilePhotoUrl: string | null;
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
  private readonly userProfileSubject = new BehaviorSubject<UserProfile | null>(null);

  /** Current authenticated profile stream. */
  public readonly userProfile$ = this.userProfileSubject.asObservable();

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
      .pipe(tap((response: AuthResponse) => this.setTokens(response)))
      .pipe(
        switchMap((response: AuthResponse) =>
          this.getProfile().pipe(
            map(() => response),
            catchError(() => of(response))
          )
        )
      );
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
   * Fetches the authenticated user's profile.
   */
  public getProfile(): Observable<UserProfile> {
    return this.httpClient
      .get<UserProfile>(`${this.apiBaseUrl}/profile`)
      .pipe(timeout(this.requestTimeoutMs))
      .pipe(tap((profile: UserProfile) => this.setUserProfile(profile)));
  }

  /**
   * Updates the authenticated user's profile.
   */
  public updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.httpClient
      .put<UserProfile>(`${this.apiBaseUrl}/profile`, request)
      .pipe(timeout(this.requestTimeoutMs))
      .pipe(tap((profile: UserProfile) => this.setUserProfile(profile)));
  }

  /**
   * Gets the current cached user profile.
   */
  public getCurrentUserProfile(): UserProfile | null {
    return this.userProfileSubject.value;
  }

  /**
   * Logs out the current user.
   */
  public logout(): void {
    localStorage.removeItem(this.accessTokenStorageKey);
    localStorage.removeItem(this.refreshTokenStorageKey);
    this.userProfileSubject.next(null);
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

    const currentProfile = this.userProfileSubject.value;
    this.setUserProfile({
      fullName: response.fullName,
      email: response.email,
      profilePhotoUrl: response.profilePhotoUrl ?? currentProfile?.profilePhotoUrl ?? null
    });

    if (response.refreshToken) {
      localStorage.setItem(this.refreshTokenStorageKey, response.refreshToken);
      return;
    }

    localStorage.removeItem(this.refreshTokenStorageKey);
  }

  private setUserProfile(profile: UserProfile): void {
    this.userProfileSubject.next(profile);
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
