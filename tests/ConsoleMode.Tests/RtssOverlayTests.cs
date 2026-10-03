using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class RtssOverlayTests
{
    [Theory]
    [InlineData(null, "compact")]
    [InlineData("off", "compact")]
    [InlineData("compact", "detailed")]
    [InlineData("detailed", "off")]
    [InlineData("bogus", "compact")]
    public void Menu_row_cycles_off_compact_detailed(string? current, string expected) =>
        Assert.Equal(expected, RtssOverlay.Next(current));

    [Fact]
    public void Off_draws_nothing() => Assert.Equal("", RtssOverlay.Text(RtssOverlay.Off));

    [Theory]
    [InlineData("compact")]
    [InlineData("detailed")]
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
}
