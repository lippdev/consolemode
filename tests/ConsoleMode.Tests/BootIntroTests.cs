using System.Text.Json;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class BootIntroTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void It_shows_only_when_on_and_the_window_opens_to_be_used(bool enabled, bool windowShown, bool expected) =>
        Assert.Equal(expected, BootIntro.ShouldShow(enabled, windowShown));

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void The_sound_has_its_own_switch_and_only_plays_with_the_opening(bool shown, bool soundEnabled, bool expected) =>
        Assert.Equal(expected, BootIntro.ShouldPlaySound(shown, soundEnabled));

    [Fact]
    public void A_fast_start_still_shows_the_logo_for_the_minimum()
    {
        Assert.Equal(BootIntro.MinimumShown, BootIntro.StillToShow(TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromMilliseconds(800), BootIntro.StillToShow(BootIntro.MinimumShown - TimeSpan.FromMilliseconds(800)));
        Assert.Equal(TimeSpan.Zero, BootIntro.StillToShow(TimeSpan.FromSeconds(5)));
        Assert.True(BootIntro.LongestShown > BootIntro.MinimumShown);
    }

    [Fact]
    public void Existing_configs_get_the_opening_and_its_sound()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("{}")!;
        Assert.True(config.IntroAnimation);
        Assert.True(config.IntroSound);
    }
}
