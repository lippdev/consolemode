namespace ConsoleMode.Services;

/// <summary>
/// The "joyChromium" launch mode: JoyChromium, the team's controller-first browser for the TV, as the console
/// launcher. Follows its integration guide (docs/INTEGRATIONS.md in lippdev/joychromium): per-user install found by
/// its uninstall key, <c>--tv</c> to open full screen for this run only, a single instance that takes the arguments
/// of a second run, and several JoyChromium.exe processes of which only the main one has a window. Pure, so it's tested.
/// </summary>
public static class JoyChromium
{
    public const string Mode = "joyChromium";

    /// <summary>The process to watch, as Process.GetProcessesByName wants it.</summary>
    public const string ProcessName = "JoyChromium";

    /// <summary>Full screen, TV mode, for this run only: the user's saved preference doesn't change.</summary>
    public const string TvArgument = "--tv";

    /// <summary>Back to a window, also for this run only; sent to the open instance when going back to the PC.</summary>
    public const string WindowArgument = "--window";

    /// <summary>The installer's uninstall key (HKCU), a UUID derived from the appId; it doesn't change between versions.</summary>
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\1fc20e06-e3c1-52c5-9ba1-fcb355e6851d";

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
    /// Going back to the PC while it's still open: ask it to leave full screen (the open instance takes
    /// <c>--window</c>) instead of closing it, so the tabs stay. Nothing to do once it has closed by itself.
    /// </summary>
    public static bool ShouldReturnToWindow(string? mode, bool running) => mode == Mode && running;
}
