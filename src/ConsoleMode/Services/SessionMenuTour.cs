namespace ConsoleMode.Services;

/// <summary>The stops of the session menu's guided tour, in the order they come.</summary>
public enum SessionTourStop
{
    /// <summary>The first time the menu opens: asks whether to take the tour at all.</summary>
    Invite,
    BackToGame,
    ControlFs,
    Volume,
    /// <summary>Resolution, audio output and HDR together.</summary>
    Display,
    /// <summary>FPS limit and counter together; only when the menu shows either.</summary>
    Fps,
    Windows,
    BackToPc,
    Exit
}

/// <summary>
/// What the session menu's guided tour shows: which stops it makes and the texts of each. Pure, so it
/// is tested; the window highlights the rows of the current stop and puts these texts beside them.
/// </summary>
public static class SessionMenuTour
{
    /// <summary>The stops, with the invite first when the tour is offered rather than asked for.</summary>
    public static IReadOnlyList<SessionTourStop> Stops(bool invite, bool fpsRows)
    {
        var stops = new List<SessionTourStop>();
        if (invite) stops.Add(SessionTourStop.Invite);
        stops.AddRange([SessionTourStop.BackToGame, SessionTourStop.ControlFs, SessionTourStop.Volume, SessionTourStop.Display]);
        if (fpsRows) stops.Add(SessionTourStop.Fps);
        stops.AddRange([SessionTourStop.Windows, SessionTourStop.BackToPc, SessionTourStop.Exit]);
        return stops;
    }

    /// <summary>"3 of 8": the position among the real stops (the invite doesn't count); null on the invite.</summary>
    public static (int Number, int Total)? Progress(IReadOnlyList<SessionTourStop> stops, int index)
    {
        if (index < 0 || index >= stops.Count || stops[index] == SessionTourStop.Invite) return null;
        var offset = stops[0] == SessionTourStop.Invite ? 1 : 0;
        return (index - offset + 1, stops.Count - offset);
    }

    /// <summary>The title of a stop: the row's own label where there is one, so the tour names what is on screen.</summary>
    public static string TitleKey(SessionTourStop stop, bool preview) => stop switch
    {
        SessionTourStop.Invite => "SessionTourInviteTitle",
        SessionTourStop.BackToGame => "BackToGame",
        SessionTourStop.ControlFs => "ControlFsCard",
        SessionTourStop.Volume => "VolumeRow",
        SessionTourStop.Display => "SessionTourDisplayTitle",
        SessionTourStop.Fps => "SessionTourFpsTitle",
        SessionTourStop.Windows => "SessionWindowsCard",
        SessionTourStop.BackToPc => preview ? "CloseMenu" : "BackToPc",
        _ => "ExitConsoleMode"
    };

    /// <summary>
    /// What a stop explains. Outside a session (the preview) nothing is restored afterwards, so the stops
    /// about changes and about the ways out say what happens there instead.
    /// </summary>
    public static string BodyKey(SessionTourStop stop, bool preview) => stop switch
    {
        SessionTourStop.Invite => "SessionTourInviteBody",
        SessionTourStop.BackToGame => "SessionTourBackToGameBody",
        SessionTourStop.ControlFs => "SessionTourControlFsBody",
        SessionTourStop.Volume => "SessionTourVolumeBody",
        SessionTourStop.Display => preview ? "SessionTourDisplayPreviewBody" : "SessionTourDisplayBody",
        SessionTourStop.Fps => "SessionTourFpsBody",
        SessionTourStop.Windows => "SessionTourWindowsBody",
        SessionTourStop.BackToPc => preview ? "SessionTourCloseMenuBody" : "SessionTourBackToPcBody",
        _ => preview ? "SessionTourExitPreviewBody" : "SessionTourExitBody"
    };

    /// <summary>
    /// Y opens the tour, unless it is part of a controller shortcut being held right now: Select + Y is the
    /// suggested way to close this very menu, and that press must not start a tour on its way out.
    /// </summary>
    public static bool IsTourPress(IEnumerable<ushort> shortcuts, IEnumerable<ushort> deviceStates)
    {
        var states = deviceStates.ToList();
        return !shortcuts.Any(mask => (mask & ControllerShortcuts.Y) != 0
                                      && ControllerShortcuts.IsHeldOnAnyDevice(mask, states));
    }
}
