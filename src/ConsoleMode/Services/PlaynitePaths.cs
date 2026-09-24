namespace ConsoleMode.Services;

/// <summary>Turns a user-chosen Playnite folder or exe into the fullscreen exe. Pure, so it's tested.</summary>
public static class PlaynitePaths
{
    public const string FullscreenExe = "Playnite.FullscreenApp.exe";

    /// <summary>The fullscreen exe for a chosen folder or exe path, or null when it isn't there.</summary>
    public static string? ResolveCustom(string? chosen)
    {
        if (string.IsNullOrWhiteSpace(chosen)) return null;
        var path = chosen.Trim().Trim('"');
        if (Directory.Exists(path)) path = Path.Combine(path, FullscreenExe);
        else if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return null;
        // Playnite.DesktopApp.exe also counts: swap in its fullscreen sibling when present.
        var fullscreen = Path.Combine(Path.GetDirectoryName(path) ?? "", FullscreenExe);
        if (File.Exists(fullscreen)) return fullscreen;
        return File.Exists(path) ? path : null;
    }
}
