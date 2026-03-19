namespace LealFinance.Api.Models.Auth;

/// <summary>
/// Represents 2FA setup details for authenticator enrollment.
/// </summary>
public sealed class TwoFactorSetupResponse
{
    /// <summary>
    /// Gets or sets whether two-factor authentication is already enabled.
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the manual shared secret key.
    /// </summary>
    public string ManualEntryKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OTPAUTH URI for QR generation.
    /// </summary>
    public string OtpAuthUri { get; set; } = string.Empty;
}
