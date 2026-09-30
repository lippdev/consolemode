namespace ConsoleMode.Services;

/// <summary>What the window switcher needs to know about a top-level window to decide if it is listed.</summary>
public readonly record struct WindowFacts(
    string Title, string ClassName, bool Visible, bool Cloaked, bool HasOwner,
    bool IsToolWindow, bool IsAppWindow, bool IsOwnProcess);

/// <summary>
/// Pure rules of the in-menu window switcher (the "Alt + Tab" of the session menu), the same ones
/// Windows applies to its own: visible, not cloaked (a suspended UWP app), titled, and neither a
/// tool window nor an owned dialog unless it says it is an app window. Kept apart so it is tested.
/// </summary>
public static class AltTabRules
{
    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow"
    };

    public static bool ShouldList(WindowFacts window)
    {
        if (!window.Visible || window.Cloaked || window.IsOwnProcess) return false;
        if (string.IsNullOrWhiteSpace(window.Title)) return false;
        if (ShellClasses.Contains(window.ClassName)) return false;
        if (window.IsAppWindow) return true;
        return !window.IsToolWindow && !window.HasOwner;
    }

    /// <summary>
    /// The card to focus when the switcher opens: the second one, like Alt + Tab (the first is the
    /// window that was in front, usually the game); the only one when there is just one.
    /// </summary>
    public static int InitialIndex(int count) => count > 1 ? 1 : 0;

    /// <summary>"chrome.exe" → "Chrome"; empty when there is no name.</summary>
    public static string ProcessDisplayName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return "";
        var name = processName.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return name.Length == 0 ? "" : char.ToUpperInvariant(name[0]) + name[1..];
    }
}
