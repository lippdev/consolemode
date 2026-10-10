using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class RtssOverlayTests
{
    [Theory]
    [InlineData(null, "off")]
    [InlineData("external", "off")]
    [InlineData("off", "compact")]
    [InlineData("compact", "detailed")]
    [InlineData("detailed", "custom")]
    [InlineData("custom", "external")]
    [InlineData("bogus", "off")]
    public void Menu_row_walks_every_style(string? current, string expected) =>
        Assert.Equal(expected, RtssOverlay.Next(current));

    [Fact]
    public void Default_leaves_afterburner_alone()
    {
        Assert.Equal(RtssOverlay.External, RtssOverlay.Normalize(null));
        Assert.False(RtssOverlay.HidesOthers(null));
    }

    [Fact]
    public void Off_draws_nothing_and_hides_afterburner()
    {
        Assert.Equal("", RtssOverlay.Text(RtssOverlay.Off));
        Assert.True(RtssOverlay.HidesOthers(RtssOverlay.Off));
    }

    [Fact]
    public void External_draws_nothing() => Assert.Equal("", RtssOverlay.Text(RtssOverlay.External));

    [Theory]
    [InlineData("compact")]
    [InlineData("detailed")]
    [InlineData("custom")]
    public void Styles_show_the_live_framerate_and_fit_the_rtss_slot(string style)
    {
        var text = RtssOverlay.Text(style);
        Assert.Contains("<FR>", text);
        Assert.True(text.Length < 4096);
        Assert.All(text, c => Assert.True(c < 128, "the RTSS slot is ASCII"));
    }

    [Fact]
    public void Detailed_adds_frametime_and_api()
    {
        var text = RtssOverlay.Text(RtssOverlay.Detailed);
        Assert.Contains("<FT>", text);
        Assert.Contains("<APP>", text);
    }

    [Fact]
    public void Custom_shows_only_what_is_ticked_on_one_line_in_its_color()
    {
        var text = RtssOverlay.Text(RtssOverlay.Custom,
            new FpsOverlayLayout { ShowApi = false, ShowFps = true, ShowFrameTime = true, SingleLine = true, Color = "6fe07a" });
        Assert.DoesNotContain("<APP>", text);
        Assert.Contains("<FT>", text);
        Assert.DoesNotContain("\n", text);
        Assert.StartsWith("<C0=6FE07A>", text);
    }

    [Fact]
    public void Custom_with_nothing_ticked_still_shows_the_fps()
    {
        var text = RtssOverlay.Text(RtssOverlay.Custom, new FpsOverlayLayout { ShowApi = false, ShowFps = false, ShowFrameTime = false });
        Assert.Contains("<FR>", text);
    }

    [Theory]
    [InlineData("FF0000><C0=000000", "FFFFFF")]
    [InlineData("", "FFFFFF")]
    [InlineData(null, "FFFFFF")]
    [InlineData("ffd54a", "FFD54A")]
    public void Colors_from_the_config_cannot_break_the_tags(string? color, string expected) =>
        Assert.Equal(expected, RtssOverlay.NormalizeColor(color));
}
