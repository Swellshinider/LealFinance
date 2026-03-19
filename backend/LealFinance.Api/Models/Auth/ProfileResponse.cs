namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the authenticated user profile.
/// </summary>
public sealed class ProfileResponse
{
    /// <summary>
    /// Gets or sets the full display name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized e-mail.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional profile photo URL or data URL.
    /// </summary>
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// Gets or sets whether authenticator-based two-factor authentication is enabled.
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; }
}
