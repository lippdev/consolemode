using System.Text.Json;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class SessionMenuTourTests
{
    [Fact]
    public void The_offered_tour_starts_with_the_invite_and_ends_on_the_two_ways_out()
    {
        var stops = SessionMenuTour.Stops(invite: true, fpsRows: true);
        Assert.Equal(SessionTourStop.Invite, stops[0]);
        Assert.Equal([SessionTourStop.BackToPc, SessionTourStop.Exit], stops.TakeLast(2));
        Assert.Contains(SessionTourStop.Fps, stops);
    }

    [Fact]
    public void Asked_for_it_skips_the_invite_and_without_fps_rows_the_fps_stop()
    {
        var stops = SessionMenuTour.Stops(invite: false, fpsRows: false);
        Assert.DoesNotContain(SessionTourStop.Invite, stops);
        Assert.DoesNotContain(SessionTourStop.Fps, stops);
        Assert.Equal(SessionTourStop.BackToGame, stops[0]);
    }

    [Fact]
    public void Progress_counts_the_real_stops_only()
    {
        var offered = SessionMenuTour.Stops(invite: true, fpsRows: true);
        Assert.Null(SessionMenuTour.Progress(offered, 0));
        Assert.Equal((1, offered.Count - 1), SessionMenuTour.Progress(offered, 1));
        Assert.Equal((offered.Count - 1, offered.Count - 1), SessionMenuTour.Progress(offered, offered.Count - 1));

        var asked = SessionMenuTour.Stops(invite: false, fpsRows: false);
        Assert.Equal((1, asked.Count), SessionMenuTour.Progress(asked, 0));
        Assert.Null(SessionMenuTour.Progress(asked, -1));
        Assert.Null(SessionMenuTour.Progress(asked, asked.Count));
    }

    [Fact]
    public void The_ways_out_explain_what_each_one_really_does()
    {
        // In a session the two buttons differ in whether the app stays in the tray; in the preview there is nothing to restore.
        Assert.Equal("BackToPc", SessionMenuTour.TitleKey(SessionTourStop.BackToPc, preview: false));
        Assert.Equal("CloseMenu", SessionMenuTour.TitleKey(SessionTourStop.BackToPc, preview: true));
        Assert.NotEqual(SessionMenuTour.BodyKey(SessionTourStop.BackToPc, false), SessionMenuTour.BodyKey(SessionTourStop.BackToPc, true));
        Assert.NotEqual(SessionMenuTour.BodyKey(SessionTourStop.Exit, false), SessionMenuTour.BodyKey(SessionTourStop.Exit, true));
        Assert.NotEqual(SessionMenuTour.BodyKey(SessionTourStop.Display, false), SessionMenuTour.BodyKey(SessionTourStop.Display, true));
    }

    [Fact]
    public void Every_stop_has_its_texts_in_every_catalog()
    {
        foreach (var language in LocalizationService.SupportedLanguages.Select(l => l.Code))
        {
            var keys = LocalizationService.GetKeys(language).ToHashSet();
            foreach (var stop in Enum.GetValues<SessionTourStop>())
            foreach (var preview in new[] { false, true })
            {
                Assert.Contains(SessionMenuTour.TitleKey(stop, preview), keys);
                Assert.Contains(SessionMenuTour.BodyKey(stop, preview), keys);
            }
        }
    }

    [Fact]
    public void Y_opens_the_tour_unless_it_is_part_of_a_shortcut_being_held()
    {
        const ushort selectY = ControllerShortcuts.Back | ControllerShortcuts.Y;
        const ushort startSelect = ControllerShortcuts.Start | ControllerShortcuts.Back;
        ushort[] shortcuts = [0, selectY, startSelect];

        Assert.True(SessionMenuTour.IsTourPress(shortcuts, [ControllerShortcuts.Y]));
        Assert.False(SessionMenuTour.IsTourPress(shortcuts, [selectY]));
        // Another pad holding Select doesn't make it a chord.
        Assert.True(SessionMenuTour.IsTourPress(shortcuts, [ControllerShortcuts.Y, ControllerShortcuts.Back]));
        // Only shortcuts that use Y count: holding one without it doesn't hide this press.
        Assert.True(SessionMenuTour.IsTourPress([0, startSelect, 0], [startSelect | ControllerShortcuts.Y]));
        Assert.True(SessionMenuTour.IsTourPress([0, 0, 0], [selectY]));
    }

    [Fact]
    public void An_existing_config_has_not_seen_the_session_menu_tour()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("{}")!;
        Assert.False(config.SessionMenuTourDone);
    }
}
