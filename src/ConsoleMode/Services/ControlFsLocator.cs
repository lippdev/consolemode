namespace ConsoleMode.Services;

/// <summary>
/// Where ControlFS (the controller-first file explorer from the same team) is installed. Pure: the registry and
/// the disk are passed in, so the order of preference is testable.
/// </summary>
public static class ControlFsLocator
{
    public const string ExeName = "ControlFS.exe";
    public const string ReleasesUrl = "https://github.com/nextestudios/ControlFS/releases/latest";

    /// <summary>Folders to look in, most reliable first, without blanks or repeats.</summary>
    public static IReadOnlyList<string> Folders(string? registryInstallLocation, string? localAppData, string? programFiles)
    {
        var folders = new List<string>();
        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var clean = path.Trim().Trim('"').TrimEnd('\\', '/');
            if (clean.Length > 0 && !folders.Contains(clean, StringComparer.OrdinalIgnoreCase)) folders.Add(clean);
        }
        Add(registryInstallLocation);
        if (!string.IsNullOrWhiteSpace(localAppData)) Add(Path.Combine(localAppData, "Programs", "ControlFS"));
        if (!string.IsNullOrWhiteSpace(programFiles)) Add(Path.Combine(programFiles, "ControlFS"));
        return folders;
    }

    /// <summary>
    /// Full screen means the window is exactly the screen. A maximized window is about 16 px bigger (its frame hangs
    /// over the edges), so it is not counted; a pixel of tolerance covers rounding.
    /// </summary>
    public static bool IsFullScreen(int left, int top, int width, int height, int screenX, int screenY, int screenWidth, int screenHeight) =>
        screenWidth > 0 && screenHeight > 0
        && Math.Abs(left - screenX) <= 1 && Math.Abs(top - screenY) <= 1
        && Math.Abs(width - screenWidth) <= 1 && Math.Abs(height - screenHeight) <= 1;

    /// <summary>The first folder that really has ControlFS.exe, or null (not installed).</summary>
    public static string? Resolve(IEnumerable<string> folders, Func<string, bool> fileExists)
    {
        foreach (var folder in folders)
        {
            var exe = Path.Combine(folder, ExeName);
            if (fileExists(exe)) return exe;
        }
        return null;
    }
}
