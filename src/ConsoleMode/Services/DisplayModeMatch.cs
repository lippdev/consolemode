namespace ConsoleMode.Services;

/// <summary>
/// Whether the mode a screen is in is the one asked for. TVs and their drivers run the "60 Hz" of video at
/// 59.94 Hz and list both: asking for 2560x1440@60 can leave the TV at @59 (the owner's LG through NVIDIA did),
/// which is that mode, not a failure. Treating it as one made the session re-apply the mode twice (the TV going
/// blank each time) and log that it couldn't. Only that way round: a screen at @60 when @59 was picked is in a
/// different mode from the list, so that one is still applied. Pure, so it's tested.
/// </summary>
public static class DisplayModeMatch
{
    /// <summary>Same size, and the rate asked for or the video one just under it (60→59, 24→23, 30→29, 120→119, 144→143); 0 = any.</summary>
    public static bool IsSame(int wantedWidth, int wantedHeight, int wantedHz, int width, int height, int hz)
    {
        if (wantedWidth > 0 && width != wantedWidth) return false;
        if (wantedHeight > 0 && height != wantedHeight) return false;
        return wantedHz <= 0 || hz == wantedHz || hz == wantedHz - 1;
    }
}
