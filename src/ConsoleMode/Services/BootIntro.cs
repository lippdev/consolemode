namespace ConsoleMode.Services;

/// <summary>
/// The opening of the app: the logo over the interface while it loads, with a sound, in one of a few styles. Pure, so
/// the rules are tested; the view draws the style and fades it out.
/// </summary>
public static class BootIntro
{
    /// <summary>Console Mode's own: the logo comes from close to the camera and settles ("Aproximação").</summary>
    public const string ConsoleStyle = "console";
    /// <summary>In the mood of the Xbox 360's: green ribbons spiralling into an orb that flashes, on a light background.</summary>
    public const string Xbox360Style = "x360";
    /// <summary>In the mood of the PS4's: waves of light and rising particles on a deep blue.</summary>
    public const string Ps4Style = "ps4";

    public static IReadOnlyList<string> Styles { get; } = [ConsoleStyle, Xbox360Style, Ps4Style];

    /// <summary>The picker's last choice, "None (opens straight away)": no opening at all (saved as IntroAnimation off).</summary>
    public const string NoOpening = "none";

    /// <summary>
    /// While no style was ever picked in System → App, now and then (this share of the openings) the app opens with one of
    /// the console-inspired styles instead of its own, as a surprise. Picking any style, its own included, ends that.
    /// </summary>
    public const double SurpriseShare = 0.125;

    /// <summary>
    /// The style to show: the one picked in System → App; with none picked, the app's own, or a console-inspired one
    /// when the roll (0 to 1) falls in the surprise share, half each.
    /// </summary>
    public static string PickForOpening(string? chosen, double roll)
    {
        if (!string.IsNullOrWhiteSpace(chosen)) return Normalize(chosen);
        if (roll < SurpriseShare / 2) return Xbox360Style;
        if (roll < SurpriseShare) return Ps4Style;
        return ConsoleStyle;
    }

    /// <summary>Never longer than this, even if loading hangs: the interface comes up anyway.</summary>
    public static readonly TimeSpan LongestShown = TimeSpan.FromSeconds(8);

    /// <summary>An unknown or missing style (a hand-edited config, a style removed later) is the app's own.</summary>
    public static string Normalize(string? style) =>
        Styles.FirstOrDefault(s => string.Equals(s, style, StringComparison.OrdinalIgnoreCase)) ?? ConsoleStyle;

    /// <summary>
    /// Shown at least this long, so a fast start doesn't cut it: the logo has to have landed. The console-inspired ones
    /// take longer to get there.
    /// </summary>
    public static TimeSpan MinimumShown(string? style) => Normalize(style) switch
    {
        Xbox360Style => TimeSpan.FromMilliseconds(3000),
        Ps4Style => TimeSpan.FromMilliseconds(2800),
        // "Liga o PC": the name comes in at 2.4 s.
        _ => TimeSpan.FromMilliseconds(3000)
    };

    /// <summary>Each style has its own entrance sound.</summary>
    public static UiSound Sound(string? style) => Normalize(style) switch
    {
        Xbox360Style => UiSound.BootX360,
        Ps4Style => UiSound.BootPs4,
        _ => UiSound.Boot
    };

    /// <summary>Only when the window opens to be used: a start that keeps it hidden (--start, the tray) skips it.</summary>
    public static bool ShouldShow(bool enabled, bool windowShown) => enabled && windowShown;

    /// <summary>The sound has its own setting, and plays only with the opening itself.</summary>
    public static bool ShouldPlaySound(bool shown, bool soundEnabled) => shown && soundEnabled;

    /// <summary>How much longer to keep it up once loading is done.</summary>
    public static TimeSpan StillToShow(TimeSpan shownFor, string? style)
    {
        var minimum = MinimumShown(style);
        return shownFor >= minimum ? TimeSpan.Zero : minimum - shownFor;
    }
}
