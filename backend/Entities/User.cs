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
    /// Gets or sets the Argon2id password hash payload.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of creation.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
