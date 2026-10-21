namespace ConsoleMode.Services;

/// <summary>One desktop icon: the shell's parsing name (a path or a ::{CLSID}) and where it sits.</summary>
public readonly record struct DesktopIcon(string Name, int X, int Y);

/// <summary>
/// Windows can reshuffle the desktop icons when the primary display changes or a screen is
/// disconnected (issue #30). The positions are read before the session and put back after it.
/// </summary>
public static class DesktopIconLayout
{
    /// <summary>
    /// The saved icons that are still on the desktop but somewhere else. Icons that were added or
    /// removed during the session are left alone.
    /// </summary>
    public static List<DesktopIcon> Moves(IReadOnlyList<DesktopIcon> saved, IReadOnlyList<DesktopIcon> current)
    {
        var now = new Dictionary<string, DesktopIcon>(StringComparer.OrdinalIgnoreCase);
        foreach (var icon in current)
            if (!string.IsNullOrWhiteSpace(icon.Name)) now[icon.Name] = icon;

        var moves = new List<DesktopIcon>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var icon in saved)
        {
            if (string.IsNullOrWhiteSpace(icon.Name) || !seen.Add(icon.Name)) continue;
            if (now.TryGetValue(icon.Name, out var there) && (there.X != icon.X || there.Y != icon.Y))
                moves.Add(icon);
        }
        return moves;
    }
}
