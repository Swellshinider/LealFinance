using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the payload used to recover the login password with 2FA.
/// </summary>
public sealed class RecoverPasswordRequest
{
    /// <summary>
    /// Gets or sets the new login password.
    /// </summary>
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the 6-digit authenticator code.
    /// </summary>
    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Verification code must be exactly 6 digits.")]
    public string VerificationCode { get; set; } = string.Empty;
}
