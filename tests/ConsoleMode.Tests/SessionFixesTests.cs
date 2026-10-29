using ConsoleMode.Services;

namespace ConsoleMode.Tests;

// Issue #149: the TV's own audio output that was already on, and a HID stick stuck off-centre.
public class SessionFixesTests
{
    [Theory]
    [InlineData("LG TV SSCR2 (NVIDIA High Definition Audio)", "LG TV SSCR2", true)]
    [InlineData("lg tv sscr2 (NVIDIA High Definition Audio)", "LG TV SSCR2", true)]
    [InlineData("LG TV SSCR2", "LG TV SSCR2", true)]
    [InlineData("25G64 (NVIDIA High Definition Audio)", "LG TV SSCR2", false)]       // the desk monitor's output
    [InlineData("Sound Bar (NVIDIA High Definition Audio)", "LG TV SSCR2", false)]
    [InlineData("LG TV SSCR2 Extra (Realtek)", "LG TV SSCR2", false)]               // only the name followed by the adapter
    [InlineData("Generic PnP Monitor (NVIDIA High Definition Audio)", "Generic PnP Monitor", false)]
    [InlineData("Monitor PnP genérico (AMD High Definition Audio)", "Monitor PnP genérico", false)]
    [InlineData("TV (Intel Display Audio)", "TV", false)]                            // too short to be sure
    [InlineData("LG TV SSCR2 (NVIDIA High Definition Audio)", null, false)]
    [InlineData(null, "LG TV SSCR2", false)]
    public void The_game_screen_output_is_found_by_its_monitor_name(string? device, string? monitor, bool expected) =>
        Assert.Equal(expected, AudioNaming.BelongsToScreen(device, monitor));

    [Fact]
    public void A_new_output_gets_time_to_show_up_before_the_one_already_on_is_used() =>
        Assert.True(AudioNaming.ActiveOutputFallbackAfter >= TimeSpan.FromSeconds(8));

    [Theory]
    [InlineData(0.5, 0.5, true)]
    [InlineData(0.3, 0.7, true)]
    [InlineData(0.5, 0.0, false)]   // an axis resting at 0: would read as Up forever
    [InlineData(1.0, 0.5, false)]
    [InlineData(0.24, 0.5, false)]
    public void A_stick_counts_only_once_seen_centred(double x, double y, bool centred) =>
        Assert.Equal(centred, ControllerMapping.StickCentered(x, y));

    [Fact]
    public void Centred_means_no_direction()
    {
        // Anything StickCentered accepts gives no direction, so a centred stick never moves the focus.
        foreach (var x in new[] { 0.25, 0.5, 0.75 })
        foreach (var y in new[] { 0.25, 0.5, 0.75 })
        {
            Assert.True(ControllerMapping.StickCentered(x, y));
            Assert.Equal((false, false, false, false), ControllerMapping.StickDirections(x, y));
        }
    }
}
