namespace ConsoleMode.Services;

/// <summary>
/// Whether the mode a screen is in is the one asked for. TVs and their drivers run the "60 Hz" of video at
/// 59.94 Hz and list both: asking for 2560x1440@60 can leave the TV at @59 (the owner's LG through NVIDIA did),
/// which is that mode, not a failure. Treating it as one made the session re-apply the mode twice (the TV going
/// blank each time) and log that it couldn't. Pure, so it's tested.
/// </summary>
public static class DisplayModeMatch
{
    /// <summary>Same size, and a refresh rate within 1 Hz (59/60, 23/24, 29/30, 119/120, 143/144…); 0 = any.</summary>
    public static bool IsSame(int wantedWidth, int wantedHeight, int wantedHz, int width, int height, int hz)
    {
        if (wantedWidth > 0 && width != wantedWidth) return false;
        if (wantedHeight > 0 && height != wantedHeight) return false;
        return wantedHz <= 0 || Math.Abs(hz - wantedHz) <= 1;
    }
}
