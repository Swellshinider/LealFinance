using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the payload for user login.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the user e-mail.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plain password.
    /// </summary>
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether refresh token persistence is requested.
    /// </summary>
    public bool RememberMe { get; set; }
}
