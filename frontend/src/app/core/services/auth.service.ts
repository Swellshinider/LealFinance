import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap, timeout } from 'rxjs';

import {
  AuthResponse,
  LoginRequest,
  MessageResponse,
  RefreshTokenRequest,
  RegisterRequest,
  UpdateProfileRequest,
  UserProfile
} from '../models/auth';
import { environment } from '../../../environments/environment';

/**
 * Manages user authentication and JWT storage.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/auth`;
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
