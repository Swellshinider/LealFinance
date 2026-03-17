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
