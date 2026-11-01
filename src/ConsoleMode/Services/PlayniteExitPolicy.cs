namespace ConsoleMode.Services;

/// <summary>
/// When a Playnite session counts as over. Playnite can swap its first big window (a loading
/// screen on slow starts) for the main one, so a vanished window alone is not an exit. Pure, so it's tested.
/// </summary>
public static class PlayniteExitPolicy
{
    /// <summary>How long Playnite may run without a big window before the session ends anyway.</summary>
    public static readonly TimeSpan WindowGrace = TimeSpan.FromSeconds(20);

    /// <param name="windowMissingSince">When the big window went missing, or null if it is showing.</param>
    /// <param name="windowPutAway">
    /// Its window still exists but is hidden or minimized: Playnite does that while a game it started runs, however
    /// long the game lasts.
    /// </param>
    public static bool IsExit(bool processRunning, bool bigWindowShowing, DateTime? windowMissingSince, DateTime now,
        bool windowPutAway = false)
    {
        if (!processRunning) return true;
        if (bigWindowShowing || windowPutAway || windowMissingSince is null) return false;
        return now - windowMissingSince.Value >= WindowGrace;
    }
}
