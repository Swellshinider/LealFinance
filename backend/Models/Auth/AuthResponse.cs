namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents a successful authentication response.
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// Gets or sets the signed JWT token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration timestamp (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user e-mail.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issued refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token expiration timestamp (UTC).
    /// </summary>
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
