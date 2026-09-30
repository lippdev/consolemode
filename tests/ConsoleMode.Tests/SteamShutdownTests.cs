using ConsoleMode.Services;
using static ConsoleMode.Services.SteamShutdown;

namespace ConsoleMode.Tests;

public class SteamShutdownTests
{
    [Fact]
    public void Steam_is_asked_to_quit_when_big_picture_was_the_launcher_nothing_blocks_it()
    {
        Assert.Equal(Decision.Shutdown, Decide("bigPicture", closeSteamSetting: true, steamRunning: true, runningAppId: 0));
    }

    [Theory]
    [InlineData("playnite")]
    [InlineData("xboxMode")]
    [InlineData("")]
    [InlineData("BigPicture")]   // the mode names are exact
    public void Steam_is_left_alone_when_it_was_not_the_launcher(string mode) =>
        Assert.Equal(Decision.NotBigPicture, Decide(mode, true, true, 0));

    [Fact]
    public void The_setting_turns_it_off()
    {
        Assert.Equal(Decision.SettingOff, Decide("bigPicture", closeSteamSetting: false, steamRunning: true, runningAppId: 0));
    }

    [Fact]
    public void A_steam_that_is_not_running_is_not_asked_to_quit()
    {
        // Asking would start it.
        Assert.Equal(Decision.NotRunning, Decide("bigPicture", true, steamRunning: false, runningAppId: 0));
    }

    [Theory]
    [InlineData(1245620)]
    [InlineData(570)]
    [InlineData(1)]
    public void A_running_game_keeps_steam_open(int appId) =>
        Assert.Equal(Decision.GameRunning, Decide("bigPicture", true, true, appId));

    [Fact]
    public void Unknown_game_state_keeps_steam_open() =>
        Assert.Equal(Decision.GameStateUnknown, Decide("bigPicture", true, true, runningAppId: null));

    [Fact]
    public void Missing_running_app_id_remains_unknown_through_the_shutdown_decision_and_log()
    {
        var appId = ParseRunningAppId(null);
        var decision = Decide("bigPicture", closeSteamSetting: true, steamRunning: true, appId);

        Assert.Equal(Decision.GameStateUnknown, decision);
        Assert.Contains("mantida aberta", Describe(decision, appId));
        Assert.Contains("não foi possível verificar", Describe(decision, appId));
    }

    [Fact]
    public void The_setting_wins_over_everything_else()
    {
        Assert.Equal(Decision.SettingOff, Decide("bigPicture", false, false, 570));
    }

    [Theory]
    [InlineData(1245620, 1245620)]
    [InlineData(0, 0)]
    [InlineData(-5, null)]
    [InlineData(570u, 570)]
    [InlineData(4000000000u, null)]  // does not fit an int: state unknown
    [InlineData("730", 730)]
    [InlineData("abc", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void The_running_game_id_is_read_from_whatever_the_registry_holds(object? value, int? expected) =>
        Assert.Equal(expected, ParseRunningAppId(value));

    [Fact]
    public void Every_decision_has_a_log_line_and_the_game_one_names_the_game()
    {
        foreach (var decision in Enum.GetValues<Decision>())
            Assert.False(string.IsNullOrWhiteSpace(Describe(decision, 570)));
        Assert.Contains("570", Describe(Decision.GameRunning, 570));
    }
}
