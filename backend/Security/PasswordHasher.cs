using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace LealFinance.Api.Security;

/// <summary>
/// Argon2id password hasher implementation.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 4;
    private const int MemorySizeKb = 65536;
    private const int DegreeOfParallelism = 2;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return $"argon2id$v=19$m={MemorySizeKb},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var segments = passwordHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5 || segments[0] != "argon2id")
        {
            return false;
        }

        try
        {
            var parameterMap = ParseParameters(segments[2]);
            var salt = Convert.FromBase64String(segments[3]);
            var expectedHash = Convert.FromBase64String(segments[4]);

            var computedHash = ComputeHash(password, salt, parameterMap.MemorySizeKb, parameterMap.Iterations, parameterMap.DegreeOfParallelism, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt, int memorySizeKb = MemorySizeKb, int iterations = Iterations, int degreeOfParallelism = DegreeOfParallelism, int hashSize = HashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            Iterations = iterations,
            MemorySize = memorySizeKb,
            DegreeOfParallelism = degreeOfParallelism
        };

        return argon2.GetBytes(hashSize);
    }

    private static (int MemorySizeKb, int Iterations, int DegreeOfParallelism) ParseParameters(string parameterSection)
    {
        var parts = parameterSection.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var memorySizeKb = int.Parse(parts.Single(static p => p.StartsWith("m=", StringComparison.Ordinal)).Split('=')[1]);
        var iterations = int.Parse(parts.Single(static p => p.StartsWith("t=", StringComparison.Ordinal)).Split('=')[1]);
        var degreeOfParallelism = int.Parse(parts.Single(static p => p.StartsWith("p=", StringComparison.Ordinal)).Split('=')[1]);

        return (memorySizeKb, iterations, degreeOfParallelism);
    }
}
