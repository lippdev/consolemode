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

    [Theory]
    [InlineData("console", "console")]
    [InlineData("X360", "x360")]
    [InlineData("ps4", "ps4")]
    [InlineData("ps5", "console")]
    [InlineData(null, "console")]
    public void Unknown_styles_fall_back_to_the_apps_own(string? style, string expected) =>
        Assert.Equal(expected, BootIntro.Normalize(style));

    [Fact]
    public void Each_style_has_its_own_sound()
    {
        Assert.Equal(UiSound.Boot, BootIntro.Sound(BootIntro.ConsoleStyle));
        Assert.Equal(UiSound.BootX360, BootIntro.Sound(BootIntro.Xbox360Style));
        Assert.Equal(UiSound.BootPs4, BootIntro.Sound(BootIntro.Ps4Style));
        Assert.Equal(BootIntro.Styles.Count, BootIntro.Styles.Select(BootIntro.Sound).Distinct().Count());
    }

    [Fact]
    public void A_fast_start_still_shows_each_style_until_its_logo_has_landed()
    {
        foreach (var style in BootIntro.Styles)
        {
            var minimum = BootIntro.MinimumShown(style);
            Assert.Equal(minimum, BootIntro.StillToShow(TimeSpan.Zero, style));
            Assert.Equal(TimeSpan.FromMilliseconds(800), BootIntro.StillToShow(minimum - TimeSpan.FromMilliseconds(800), style));
            Assert.Equal(TimeSpan.Zero, BootIntro.StillToShow(TimeSpan.FromSeconds(5), style));
            Assert.True(BootIntro.LongestShown > minimum);
        }
        Assert.True(BootIntro.MinimumShown(BootIntro.Xbox360Style) > BootIntro.MinimumShown(BootIntro.ConsoleStyle));
    }

    [Fact]
    public void Existing_configs_get_the_opening_its_sound_and_the_apps_own_style()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("{}")!;
        Assert.True(config.IntroAnimation);
        Assert.True(config.IntroSound);
        Assert.Equal(BootIntro.ConsoleStyle, BootIntro.Normalize(config.IntroStyle));
    }
}
