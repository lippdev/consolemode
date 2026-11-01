using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class PlayniteExitPolicyTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Exits_when_the_process_is_gone() =>
        Assert.True(PlayniteExitPolicy.IsExit(processRunning: false, bigWindowShowing: false, windowMissingSince: null, T0));

    [Fact]
    public void Stays_while_the_big_window_shows() =>
        Assert.False(PlayniteExitPolicy.IsExit(processRunning: true, bigWindowShowing: true, windowMissingSince: null, T0));

    [Fact]
    public void Waits_while_the_loading_window_is_swapped_for_the_main_one() =>
        Assert.False(PlayniteExitPolicy.IsExit(processRunning: true, bigWindowShowing: false, windowMissingSince: T0, T0 + TimeSpan.FromSeconds(5)));

    [Fact]
    public void Exits_when_the_process_stays_windowless_past_the_grace() =>
        Assert.True(PlayniteExitPolicy.IsExit(processRunning: true, bigWindowShowing: false, windowMissingSince: T0, T0 + PlayniteExitPolicy.WindowGrace));

    [Fact]
    public void Stays_while_playnite_hides_its_window_for_a_game() =>
        // "Minimize after game startup" (Selene turns it on): a game that lasts hours is still the session.
        Assert.False(PlayniteExitPolicy.IsExit(processRunning: true, bigWindowShowing: false, windowMissingSince: T0, T0 + TimeSpan.FromHours(3), windowPutAway: true));

    [Fact]
    public void Exits_when_playnite_closes_while_its_window_is_put_away() =>
        Assert.True(PlayniteExitPolicy.IsExit(processRunning: false, bigWindowShowing: false, windowMissingSince: null, T0, windowPutAway: true));
}
