using System.Security.Cryptography;

namespace JobTrack.Api.Services;

/// <summary>
/// Hashes passwords with PBKDF2 (built into .NET). A hash is a one-way scramble:
/// we can check a password against it, but we can never get the password back.
/// Stored format: iterations.salt.hash (salt and hash are base64)
// Why the salt matters: two users with the same password password123 get different hashes, because their salts differ..
/// </summary>
public class PasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Fixed-time comparison so an attacker can't learn anything from timing.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
