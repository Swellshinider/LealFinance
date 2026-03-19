using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the payload used to update the authenticated user profile.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>
    /// Gets or sets the full display name.
    /// </summary>
    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user e-mail.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional profile photo URL or data URL.
    /// </summary>
    [MaxLength(10000000)]
    public string? ProfilePhotoUrl { get; set; }
}
