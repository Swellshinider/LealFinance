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
 * 2FA enable request payload.
 */
export interface EnableTwoFactorRequest {
  /** 6-digit code from authenticator app. */
  verificationCode: string;
}

/**
 * Password recovery request payload.
 */
export interface RecoverPasswordRequest {
  /** New login password. */
  newPassword: string;
  /** 6-digit code from authenticator app. */
  verificationCode: string;
}
