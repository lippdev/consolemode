using System.Security.Cryptography;

namespace ConsoleMode.Services;

/// <summary>Validates GitHub's published SHA-256 digest for a release asset.</summary>
public static class UpdateIntegrity
{
    private const string Prefix = "sha256:";

    public static bool IsValidSha256Digest(string? digest) =>
        digest is { Length: 71 } &&
        digest.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
        digest.AsSpan(Prefix.Length).IndexOfAnyExcept("0123456789abcdefABCDEF") < 0;

    public static async Task<bool> VerifyFileAsync(string path, string? expectedDigest, CancellationToken ct = default)
    {
        if (!IsValidSha256Digest(expectedDigest)) return false;
        await using var file = File.OpenRead(path);
        var actual = await SHA256.HashDataAsync(file, ct);
        var expected = Convert.FromHexString(expectedDigest![Prefix.Length..]);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
