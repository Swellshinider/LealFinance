using OtpNet;

namespace LealFinance.Api.Security;

/// <summary>
/// TOTP service backed by RFC 6238 implementation.
/// </summary>
public sealed class TwoFactorService : ITwoFactorService
{
    private const string DefaultIssuer = "LealFinance";

    /// <inheritdoc />
    public string GenerateSharedSecret()
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secretBytes);
    }

    /// <inheritdoc />
    public string BuildOtpAuthUri(string issuer, string email, string sharedSecret)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret))
        {
            throw new ArgumentException("Shared secret is required.", nameof(sharedSecret));
        }

        var normalizedIssuer = string.IsNullOrWhiteSpace(issuer) ? DefaultIssuer : issuer.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? "account" : email.Trim();
        var urlEncodedLabel = Uri.EscapeDataString($"{normalizedIssuer}:{normalizedEmail}");
        var urlEncodedIssuer = Uri.EscapeDataString(normalizedIssuer);

        return $"otpauth://totp/{urlEncodedLabel}?secret={sharedSecret}&issuer={urlEncodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    /// <inheritdoc />
    public bool VerifyCode(string sharedSecret, string code)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (!int.TryParse(code, out _))
        {
            return false;
        }

        var secretBytes = Base32Encoding.ToBytes(sharedSecret);
        var totp = new Totp(secretBytes, mode: OtpHashMode.Sha1, step: 30, totpSize: 6);

        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
    }
}
