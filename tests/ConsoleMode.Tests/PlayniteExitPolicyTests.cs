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
}
