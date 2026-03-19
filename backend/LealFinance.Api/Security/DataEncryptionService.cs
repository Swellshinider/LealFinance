using System.Security.Cryptography;
using System.Text;

namespace LealFinance.Api.Security;

/// <summary>
/// AES-GCM record encryption service using per-user derived keys.
/// </summary>
public sealed class DataEncryptionService : IDataEncryptionService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int DerivationIterations = 100_000;

    /// <inheritdoc />
    public string GenerateUserSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    }

    /// <inheritdoc />
    public string Encrypt(string payload, string masterPasswordHash, string userSalt)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload cannot be empty.", nameof(payload));
        }

        var key = DeriveKey(masterPasswordHash, userSalt);
        var plaintextBytes = Encoding.UTF8.GetBytes(payload);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, tagSizeInBytes: TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var output = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, output, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, output, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Convert.ToBase64String(output);
    }

    /// <inheritdoc />
    public string Decrypt(string encryptedPayload, string masterPasswordHash, string userSalt)
    {
        if (string.IsNullOrWhiteSpace(encryptedPayload))
        {
            throw new ArgumentException("Encrypted payload cannot be empty.", nameof(encryptedPayload));
        }

        var input = Convert.FromBase64String(encryptedPayload);
        if (input.Length <= NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Encrypted payload format is invalid.");
        }

        var nonce = input.AsSpan(0, NonceSizeBytes).ToArray();
        var tag = input.AsSpan(NonceSizeBytes, TagSizeBytes).ToArray();
        var ciphertext = input.AsSpan(NonceSizeBytes + TagSizeBytes).ToArray();
        var plaintext = new byte[ciphertext.Length];

        var key = DeriveKey(masterPasswordHash, userSalt);

        using var aes = new AesGcm(key, tagSizeInBytes: TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DeriveKey(string masterPasswordHash, string userSalt)
    {
        if (string.IsNullOrWhiteSpace(masterPasswordHash))
        {
            throw new ArgumentException("Master password hash is required.", nameof(masterPasswordHash));
        }

        if (string.IsNullOrWhiteSpace(userSalt))
        {
            throw new ArgumentException("User salt is required.", nameof(userSalt));
        }

        var saltBytes = Convert.FromBase64String(userSalt);

        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(masterPasswordHash),
            saltBytes,
            DerivationIterations,
            HashAlgorithmName.SHA256,
            KeySizeBytes);
    }
}
