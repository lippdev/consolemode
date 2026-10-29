using ConsoleMode.Models;

namespace ConsoleMode.Services;

/// <summary>
/// "Park the pointer" (issue #172): a session that starts leaves the mouse pointer in the middle of the TV, so it is
/// moved to a corner of the game screen, out of the way. Steam and others recenter it while their window opens, so it
/// is done twice. Going back to the PC puts it where it was before the session. Pure, so it's tested.
/// Based on @JarJarShinks' version in #173.
/// </summary>
public static class PointerParking
{
    /// <summary>How far inside the corner the pointer sits (some TVs crop the very edge).</summary>
    public const int Margin = 6;

    /// <summary>How many times the pointer is parked per session.</summary>
    public const int Parks = 2;

    /// <summary>Between two parks, so the second one lands after the launcher has settled.</summary>
    public static readonly TimeSpan Between = TimeSpan.FromSeconds(3);

    /// <summary>Xbox mode shows no window the app can watch: it is parked this long after the launch instead.</summary>
    public static readonly TimeSpan XboxDelay = TimeSpan.FromSeconds(4);

    /// <summary>The bottom-right corner of the screen, a few pixels inside it.</summary>
    public static (int X, int Y) BottomRight(ScreenRect rect) =>
        (rect.X + Math.Max(rect.Width - 1 - Margin, 0), rect.Y + Math.Max(rect.Height - 1 - Margin, 0));

    /// <param name="enabled">The setting is on.</param>
    /// <param name="done">How many times it was parked in this session.</param>
    /// <param name="ready">The launcher is up (its window appeared, or the Xbox delay passed).</param>
    /// <param name="last">When it was last parked, or null.</param>
    public static bool IsDue(bool enabled, int done, bool ready, DateTime? last, DateTime now) =>
        enabled && ready && done < Parks && (last is null || now - last.Value >= Between);

    /// <summary>
    /// Going back to the PC: the pointer returns to where it was before the session, but only when it was parked
    /// in this session (otherwise the user may have moved it on purpose) and that spot is on a screen that is on again.
    /// </summary>
    public static bool ShouldReturn((int X, int Y)? before, int parks, bool onScreen) =>
        before is not null && parks > 0 && onScreen;
}
