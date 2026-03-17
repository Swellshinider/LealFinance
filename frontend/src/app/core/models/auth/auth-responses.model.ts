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
