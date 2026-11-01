namespace ConsoleMode.Services;

/// <summary>
/// The opening of the app: the logo and a loading bar over the interface while it loads, with a sound. Pure, so the
/// rules are tested; the view draws it and fades it out.
/// </summary>
public static class BootIntro
{
    /// <summary>Shown at least this long, so a fast start doesn't just flash the logo.</summary>
    public static readonly TimeSpan MinimumShown = TimeSpan.FromMilliseconds(1800);

    /// <summary>Never longer than this, even if loading hangs: the interface comes up anyway.</summary>
    public static readonly TimeSpan LongestShown = TimeSpan.FromSeconds(8);

    /// <summary>Only when the window opens to be used: a start that keeps it hidden (--start, the tray) skips it.</summary>
    public static bool ShouldShow(bool enabled, bool windowShown) => enabled && windowShown;

    /// <summary>The sound has its own setting, and plays only with the opening itself.</summary>
    public static bool ShouldPlaySound(bool shown, bool soundEnabled) => shown && soundEnabled;

    /// <summary>How much longer to keep it up once loading is done.</summary>
    public static TimeSpan StillToShow(TimeSpan shownFor) => shownFor >= MinimumShown ? TimeSpan.Zero : MinimumShown - shownFor;
}
