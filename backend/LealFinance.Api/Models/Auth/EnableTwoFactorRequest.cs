using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents the payload to enable two-factor authentication.
/// </summary>
public sealed class EnableTwoFactorRequest
{
    /// <summary>
    /// Gets or sets the 6-digit authenticator code.
    /// </summary>
    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Verification code must be exactly 6 digits.")]
    public string VerificationCode { get; set; } = string.Empty;
}
