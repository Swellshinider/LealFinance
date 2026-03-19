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
  /** True when the user must complete authenticator setup. */
  requiresTwoFactorSetup: boolean;
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
 * Authenticator setup response payload.
 */
export interface TwoFactorSetupResponse {
  /** Current two-factor enabled state. */
  isTwoFactorEnabled: boolean;
  /** Manual shared secret key for authenticator app. */
  manualEntryKey: string;
  /** OTPAUTH URI for QR setup. */
  otpAuthUri: string;
}
