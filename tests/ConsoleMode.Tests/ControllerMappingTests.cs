using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerMappingTests
{
    [Fact]
    public void Sony_layout_confirms_with_cross_and_backs_with_circle()
    {
        var (confirm, back, option, alt, menu) = ControllerMapping.HidIndices(ControllerMapping.SonyVendorId);
        Assert.Equal((1, 2, 0, 3, 9), (confirm, back, option, alt, menu));
    }

    [Fact]
    public void Nintendo_layout_swaps_a_and_b()
    {
        var (confirm, back, _, _, _) = ControllerMapping.HidIndices(ControllerMapping.NintendoVendorId);
        Assert.Equal((1, 0), (confirm, back));
    }

    [Theory]
    [InlineData(false, 0, true)]   // plain HID pad: always read
    [InlineData(false, 1, true)]
    [InlineData(true, 1, false)]   // also a Gamepad and XInput answered: XInput covers it
    [InlineData(true, 0, true)]    // also a Gamepad but no XInput slot: read it or the pad is mute
    public void Hid_pads_are_skipped_only_when_xinput_covers_them(bool isGamepad, int xinputPads, bool expected) =>
        Assert.Equal(expected, ControllerMapping.ShouldReadHid(isGamepad, xinputPads));

    [Fact]
    public void Gamepads_are_read_only_without_xinput()
    {
        Assert.True(ControllerMapping.ShouldReadGamepads(0));
        Assert.False(ControllerMapping.ShouldReadGamepads(1));
    }

    [Theory]
    [InlineData(0.5, 0.5, false, false, false, false)]
    [InlineData(0.0, 0.5, true, false, false, false)]
    [InlineData(1.0, 0.5, false, true, false, false)]
    [InlineData(0.5, 0.1, false, false, true, false)]
    [InlineData(0.5, 0.9, false, false, false, true)]
    [InlineData(0.3, 0.7, false, false, false, false)]  // inside the dead zone
    public void Stick_has_a_quarter_dead_zone(double x, double y, bool left, bool right, bool up, bool down) =>
        Assert.Equal((left, right, up, down), ControllerMapping.StickDirections(x, y));
}
