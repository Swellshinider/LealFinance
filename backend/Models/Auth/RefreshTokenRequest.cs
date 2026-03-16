using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the payload for token refresh.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the expired access token.
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current refresh token.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}
