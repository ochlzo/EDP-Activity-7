using System.Security.Cryptography;

namespace edp_gui_app;

public static class PasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "pbkdf2-sha256";

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashBytes);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedPassword)
    {
        if (!storedPassword.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            return password == storedPassword;
        }

        var parts = storedPassword.Split('$');
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public static bool NeedsRehash(string storedPassword)
    {
        return !storedPassword.StartsWith($"{Prefix}${Iterations}$", StringComparison.Ordinal);
    }
}
