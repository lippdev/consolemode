using System.Security.Cryptography;
using System.Text;

namespace ConsoleMode.Services;

/// <summary>
/// Secrets kept on disk (TV tokens and keys), encrypted for the signed-in Windows user with DPAPI.
/// The data folder travels with the portable exe, so plain text would travel with it too.
/// DPAPI data can only be read by the same Windows user on the same PC: anything else
/// comes back as "unreadable" so the caller can ask the user to enter it again.
/// </summary>
public static class SecretProtector
{
    public const string Prefix = "dpapi:";

    public static bool IsProtected(string? stored) =>
        stored is not null && stored.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Protect(string secret)
    {
        if (string.IsNullOrEmpty(secret)) return "";
        var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(data);
    }

    /// <summary>
    /// Plain text (never protected, e.g. a hand-edited config) passes through. Returns false when
    /// the data is protected but can't be read here (another Windows user or another PC).
    /// </summary>
    public static bool TryUnprotect(string? stored, out string secret)
    {
        secret = "";
        if (string.IsNullOrEmpty(stored)) return true;
        if (!IsProtected(stored))
        {
            secret = stored;
            return true;
        }

        try
        {
            var data = ProtectedData.Unprotect(Convert.FromBase64String(stored[Prefix.Length..]), null, DataProtectionScope.CurrentUser);
            secret = Encoding.UTF8.GetString(data);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            AppLog.Write($"Segredo protegido ilegível neste usuário/PC ({ex.GetType().Name})");
            return false;
        }
    }

    /// <summary>Like <see cref="TryUnprotect"/>, with "" for an unreadable secret.</summary>
    public static string Unprotect(string? stored) => TryUnprotect(stored, out var secret) ? secret : "";
}
