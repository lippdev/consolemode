using System.Text.Json;

namespace ConsoleMode.Services;

/// <summary>The JoyChromium installer of one release: what to download and the SHA-256 it must have.</summary>
public sealed record JoyChromiumSetup(string Version, string Url, string Name, string Digest, long Size);

/// <summary>
/// JoyChromium, the team's controller-first browser for the TV, as a helper of the session menu (like ControlFS): not
/// a launcher, but something to open over the game when the user wants the web with the controller. Follows its
/// integration guide (docs/INTEGRATIONS.md in lippdev/joychromium): per-user install found by its uninstall key,
/// <c>--tv</c> to open full screen for this run only, a single instance that takes the arguments of a second run, and
/// several JoyChromium.exe processes of which only the main one has a window. Pure, so it's tested.
/// </summary>
public static class JoyChromium
{
    /// <summary>The process to look for, as Process.GetProcessesByName wants it.</summary>
    public const string ProcessName = "JoyChromium";

    /// <summary>Full screen, TV mode, for this run only: the user's saved preference doesn't change.</summary>
    public const string TvArgument = "--tv";

    /// <summary>Back to a window, also for this run only; sent to the open instance when going back to the PC.</summary>
    public const string WindowArgument = "--window";

    /// <summary>The installer's uninstall key (HKCU), a UUID derived from the appId; it doesn't change between versions.</summary>
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\1fc20e06-e3c1-52c5-9ba1-fcb355e6851d";

    public const string Repository = "lippdev/joychromium";
    public const string ReleasesUrl = "https://github.com/" + Repository + "/releases";
    public const string DownloadPrefix = "https://github.com/" + Repository + "/releases/download/";

    /// <summary>NSIS one-click, per user: <c>/S</c> installs silently, without an admin prompt.</summary>
    public const string SilentArgument = "/S";

    /// <summary>Where the per-user installer puts it: %LOCALAPPDATA%\Programs\joychromium\JoyChromium.exe.</summary>
    public static string DefaultPath(string localAppData) =>
        Path.Combine(localAppData, "Programs", "joychromium", "JoyChromium.exe");

    /// <summary>
    /// The exe from the uninstall key's DisplayIcon (<c>"C:\…\JoyChromium.exe",0</c>): InstallLocation comes empty,
    /// so the icon path is the one to trust. Null when it isn't an exe path.
    /// </summary>
    public static string? ExeFromDisplayIcon(string? displayIcon)
    {
        var value = (displayIcon ?? "").Trim();
        if (value.Length == 0) return null;

        // An icon index after the path: ",0" (or ",-101"), outside the quotes when there are any.
        var comma = value.LastIndexOf(',');
        if (comma > 0 && int.TryParse(value[(comma + 1)..].Trim(), out _)) value = value[..comma];

        value = CustomApp.CleanPath(value);
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? value : null;
    }

    /// <summary>The first candidate that exists: the default install folder, then the path from the uninstall key.</summary>
    public static string? Resolve(string? defaultPath, string? displayIcon, Func<string, bool> exists)
    {
        foreach (var candidate in new[] { defaultPath, ExeFromDisplayIcon(displayIcon) })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Going back to the PC while the JoyChromium opened from the session menu is still open: ask it to leave full screen
    /// (the open instance takes <c>--window</c>) instead of closing it, so the tabs stay. A JoyChromium the user had open
    /// on the desk on their own is left alone.
    /// </summary>
    public static bool ShouldReturnToWindow(bool openedFromMenu, bool running) => openedFromMenu && running;

    /// <summary>"JoyChromium-0.1.3-alpha-win-x64.exe": the installer for this PC's processor (not the both-in-one "-win.exe").</summary>
    public static string SetupSuffix(bool arm64) => arm64 ? "-win-arm64.exe" : "-win-x64.exe";

    /// <summary>
    /// The installer of the newest release (GitHub lists them newest first). Its releases are pre-releases (alpha), so
    /// those count; drafts don't. Only from this repository's downloads and only with GitHub's SHA-256 digest: the
    /// newest release decides, and without both nothing is run (the release page opens instead).
    /// </summary>
    public static JoyChromiumSetup? PickSetup(JsonElement releases, bool arm64)
    {
        if (releases.ValueKind != JsonValueKind.Array) return null;
        var suffix = SetupSuffix(arm64);
        foreach (var release in releases.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object) continue;
            if (release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True) continue;
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            foreach (var asset in assets.EnumerateArray())
            {
                var name = Text(asset, "name");
                if (!name.StartsWith("JoyChromium-", StringComparison.OrdinalIgnoreCase)
                    || !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                var url = Text(asset, "browser_download_url");
                var digest = Text(asset, "digest");
                if (!IsTrustedUrl(url) || !UpdateIntegrity.IsValidSha256Digest(digest)) return null;
                var size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var bytes) ? bytes : 0;
                return new JoyChromiumSetup(Text(release, "tag_name").TrimStart('v', 'V'), url, name, digest, size);
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
