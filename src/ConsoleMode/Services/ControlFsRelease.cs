using System.Text.Json;

namespace ConsoleMode.Services;

/// <summary>The ControlFS installer of one release: what to download and the SHA-256 it must have.</summary>
public sealed record ControlFsSetup(string Version, string Url, string Name, string Digest, long Size);

/// <summary>
/// Picks the ControlFS installer out of GitHub's list of its releases. Pure, so the rules are tested: only a
/// published release, only the x64 setup, only with GitHub's SHA-256 digest and only from that repository's
/// own downloads. Anything else is not installed from the menu (the download page opens instead).
/// </summary>
public static class ControlFsRelease
{
    public const string Repository = "nextestudios/ControlFS";
    public const string SetupAssetName = "ControlFS-Setup-x64.exe";
    public const string DownloadPrefix = "https://github.com/" + Repository + "/releases/download/";

    /// <summary>The setup of the newest published release (GitHub lists them newest first), or null.</summary>
    public static ControlFsSetup? Pick(JsonElement releases)
    {
        if (releases.ValueKind != JsonValueKind.Array) return null;
        foreach (var release in releases.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object) continue;
            if (release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True) continue;
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            foreach (var asset in assets.EnumerateArray())
            {
                if (!string.Equals(Text(asset, "name"), SetupAssetName, StringComparison.OrdinalIgnoreCase)) continue;
                var url = Text(asset, "browser_download_url");
                var digest = Text(asset, "digest");
                // The newest release decides: without a digest or from somewhere else, nothing is run.
                if (!IsTrustedUrl(url) || !UpdateIntegrity.IsValidSha256Digest(digest)) return null;
                var size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var bytes) ? bytes : 0;
                return new ControlFsSetup(Text(release, "tag_name").TrimStart('v', 'V'), url, SetupAssetName, digest, size);
            }
        }
        return null;
    }

    public static bool IsTrustedUrl(string? url) =>
        url is not null && url.StartsWith(DownloadPrefix, StringComparison.Ordinal);

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";
}
