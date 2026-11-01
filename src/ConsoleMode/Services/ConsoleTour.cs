namespace ConsoleMode.Services;

/// <summary>The stops of the first-run guided tour of the console interface, in the order they come.</summary>
public enum ConsoleTourStop
{
    /// <summary>What the app does; nothing on screen is highlighted.</summary>
    Welcome,
    /// <summary>The tabs at the top, LB / RB.</summary>
    Tabs,
    /// <summary>The row of launchers on Home: what opens.</summary>
    Launchers,
    /// <summary>The play button: entering console mode.</summary>
    Play,
    /// <summary>The screens on Session: which one is the game screen and what happens to the others.</summary>
    Screens,
    /// <summary>The quick settings on Session: audio, resolution, HDR, FPS.</summary>
    QuickSettings,
    /// <summary>The categories on System: every setting.</summary>
    System,
    /// <summary>The controller shortcuts (System → Controller): Home button, session menu, back to the PC.</summary>
    Shortcuts,
    /// <summary>Ready; how to see the tour again.</summary>
    Done
}

/// <summary>
/// What the console interface's first-run tour shows: its stops, the tab each one needs and its texts. Pure, so it
/// is tested; the view switches to the tab, highlights the stop's part of the screen and puts the texts beside it.
/// </summary>
public static class ConsoleTour
{
    public static IReadOnlyList<ConsoleTourStop> Stops { get; } = Enum.GetValues<ConsoleTourStop>();

    /// <summary>The tab a stop is shown on: 0 Home, 1 Session, 2 System (the console tab indexes).</summary>
    public static int Tab(ConsoleTourStop stop) => stop switch
    {
        ConsoleTourStop.Screens or ConsoleTourStop.QuickSettings => 1,
        ConsoleTourStop.System or ConsoleTourStop.Shortcuts => 2,
        _ => 0
    };

    /// <summary>The System category a stop opens (the controller one for the shortcuts), or null to leave it as it is.</summary>
    public static int? SystemCategory(ConsoleTourStop stop) => stop switch
    {
        ConsoleTourStop.System => 0,
        ConsoleTourStop.Shortcuts => 2,
        _ => null
    };

    /// <summary>The stops that don't point at anything: the card sits in the middle of the screen.</summary>
    public static bool IsCentered(ConsoleTourStop stop) => stop is ConsoleTourStop.Welcome or ConsoleTourStop.Done;

    public static string TitleKey(ConsoleTourStop stop) => $"ConsoleTour{stop}Title";
    public static string BodyKey(ConsoleTourStop stop) => $"ConsoleTour{stop}Body";

    /// <summary>
    /// Offered by itself until it is finished or skipped, when the window shows; never over a running session (the
    /// window is just coming back from the TV then).
    /// </summary>
    public static bool ShouldOffer(bool tourDone, bool sessionActive) => !tourDone && !sessionActive;

    /// <summary>The stop after (step 1) or before (-1) this one, or null past either end.</summary>
    public static int? Step(int index, int step)
    {
        var next = index + step;
        return next >= 0 && next < Stops.Count ? next : null;
    }
}
