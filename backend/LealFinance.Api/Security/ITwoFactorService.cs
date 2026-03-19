namespace LealFinance.Api.Security;

/// <summary>
/// Generates and validates authenticator-app based one-time passwords.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new shared secret for TOTP.
    /// </summary>
    /// <returns>Base32 shared secret.</returns>
    string GenerateSharedSecret();

    /// <summary>
    /// Builds an OTPAUTH URI that can be used by authenticator applications.
    /// </summary>
    /// <param name="issuer">Issuer label displayed in app.</param>
    /// <param name="email">User account e-mail label.</param>
    /// <param name="sharedSecret">Base32 shared secret.</param>
    /// <returns>OTPAUTH URI.</returns>
    string BuildOtpAuthUri(string issuer, string email, string sharedSecret);

    /// <summary>
    /// Verifies a user-provided TOTP code.
    /// </summary>
    /// <param name="sharedSecret">Base32 shared secret.</param>
    /// <param name="code">Numeric code provided by user.</param>
    /// <returns><c>true</c> if the code is valid; otherwise <c>false</c>.</returns>
    bool VerifyCode(string sharedSecret, string code);
}
