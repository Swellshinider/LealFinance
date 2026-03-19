namespace LealFinance.Api.Security;

/// <summary>
/// Encrypts and decrypts user records at rest.
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Generates a new user-specific salt.
    /// </summary>
    /// <returns>Base64 random salt.</returns>
    string GenerateUserSalt();

    /// <summary>
    /// Encrypts a JSON payload for persistent storage.
    /// </summary>
    /// <param name="payload">Plaintext payload.</param>
    /// <param name="masterPasswordHash">Stored master-password hash.</param>
    /// <param name="userSalt">Per-user salt.</param>
    /// <returns>Serialized encrypted payload.</returns>
    string Encrypt(string payload, string masterPasswordHash, string userSalt);

    /// <summary>
    /// Decrypts a persisted encrypted payload.
    /// </summary>
    /// <param name="encryptedPayload">Encrypted payload string.</param>
    /// <param name="masterPasswordHash">Stored master-password hash.</param>
    /// <param name="userSalt">Per-user salt.</param>
    /// <returns>Plaintext payload.</returns>
    string Decrypt(string encryptedPayload, string masterPasswordHash, string userSalt);
}
