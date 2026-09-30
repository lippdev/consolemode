using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ConsoleMode.Services.Tv;

/// <summary>Pure transport and trust rules for webOS pairing.</summary>
public static class WebOsSecurity
{
    public const int MaxMessageBytes = 64 * 1024;

    public static string HostId(string host) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(host.Trim().ToLowerInvariant())));

    public static string Fingerprint(X509Certificate certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));

    public static bool MatchesPin(string expected, string actual) =>
        expected.Length == 64 && actual.Length == 64 &&
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(actual));

    public static bool MaySendSavedKey(bool secure, bool hasPinnedCertificate, bool allowInsecure) =>
        secure ? hasPinnedCertificate : allowInsecure;

    public static bool WithinMessageLimit(long currentLength, int nextCount) =>
        nextCount >= 0 && currentLength >= 0 && currentLength <= MaxMessageBytes - nextCount;
}
