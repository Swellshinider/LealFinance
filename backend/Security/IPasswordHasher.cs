namespace LealFinance.Api.Security;

/// <summary>
/// Defines password hashing and verification behavior.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using Argon2id.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <returns>Persistable hash payload.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain-text password against an existing hash payload.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <param name="passwordHash">Persisted hash payload.</param>
    /// <returns><c>true</c> when valid; otherwise <c>false</c>.</returns>
    bool VerifyPassword(string password, string passwordHash);
}
