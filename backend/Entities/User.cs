using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Entities;

/// <summary>
/// Represents an application user stored in SQLite.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the normalized e-mail address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full display name.
    /// </summary>
    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Argon2id password hash payload.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of creation.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the persisted refresh token for session renewal.
    /// </summary>
    [MaxLength(512)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration timestamp for the current refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// Gets or sets the optional profile photo URL or data URL.
    /// </summary>
    [MaxLength(10000000)]
    public string? ProfilePhotoUrl { get; set; }
}
