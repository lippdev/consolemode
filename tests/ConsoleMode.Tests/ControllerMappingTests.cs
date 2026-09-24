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

    // Select + Y on a Sony pad = Create/Share + Triangle, at each report's button offset.
    [Theory]
    [InlineData(0x01, 64, true, 8)]    // DualSense, USB
    [InlineData(0x31, 78, true, 9)]    // DualSense, Bluetooth
    [InlineData(0x01, 64, false, 5)]   // DS4, USB
    [InlineData(0x11, 78, false, 7)]   // DS4, Bluetooth
    [InlineData(0x01, 10, true, 5)]    // basic Bluetooth report, before the enhanced mode
    public void Sony_reports_map_create_and_triangle_to_select_and_y(int id, int length, bool dualSense, int start)
    {
        var report = new byte[length];
        report[0] = (byte)id;
        report[start] = 0x88;       // Triangle, D-pad neutral
        report[start + 1] = 0x10;   // Create/Share
        Assert.Equal(0x0020 | 0x8000, ControllerMapping.SonyChordButtons(report, dualSense));
    }

    [Fact]
    public void Sony_options_and_ps_map_to_start_and_guide()
    {
        var report = new byte[64];
        report[0] = 0x01;
        report[9] = 0x20;   // Options
        report[10] = 0x01;  // PS
        Assert.Equal(0x0010 | 0x0400, ControllerMapping.SonyChordButtons(report, dualSense: true));
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x05, 0xFF, 0xFF })]  // unknown report ID
    [InlineData(new byte[] { 0x31, 0, 0 })]         // too short
    public void Unknown_or_short_sony_reports_give_nothing(byte[] report) =>
        Assert.Equal(0, ControllerMapping.SonyChordButtons(report, dualSense: true));
}
