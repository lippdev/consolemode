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

    // Report IDs and offsets: (id, length, dualSense, buttons byte, left stick X byte).
    public static TheoryData<int, int, bool, int, int> SonyLayouts => new()
    {
        { 0x01, 64, true, 8, 1 },   // DualSense, USB
        { 0x31, 78, true, 9, 2 },   // DualSense, Bluetooth
        { 0x01, 64, false, 5, 1 },  // DS4, USB
        { 0x11, 78, false, 7, 3 },  // DS4, Bluetooth
        { 0x01, 10, true, 5, 1 },   // basic Bluetooth report, before the enhanced mode
    };

    /// <summary>Nothing pressed: hat released (8), sticks centered.</summary>
    private static byte[] NeutralReport(int id, int length, int start, int stick)
    {
        var report = new byte[length];
        report[0] = (byte)id;
        report[start] = 0x08;
        report[stick] = report[stick + 1] = 128;
        return report;
    }

    [Theory]
    [MemberData(nameof(SonyLayouts))]
    public void Sony_neutral_report_gives_nothing(int id, int length, bool dualSense, int start, int stick) =>
        Assert.Equal(0, ControllerMapping.SonyButtons(NeutralReport(id, length, start, stick), dualSense));

    // Select + Y on a Sony pad = Create/Share + Triangle.
    [Theory]
    [MemberData(nameof(SonyLayouts))]
    public void Sony_reports_map_create_and_triangle_to_select_and_y(int id, int length, bool dualSense, int start, int stick)
    {
        var report = NeutralReport(id, length, start, stick);
        report[start] |= 0x80;       // Triangle
        report[start + 1] = 0x10;   // Create/Share
        Assert.Equal(0x0020 | 0x8000, ControllerMapping.SonyButtons(report, dualSense));
    }

    [Theory]
    [MemberData(nameof(SonyLayouts))]
    public void Sony_face_buttons_map_to_xbox_positions(int id, int length, bool dualSense, int start, int stick)
    {
        var report = NeutralReport(id, length, start, stick);
        report[start] |= 0x10 | 0x20 | 0x40; // Square, Cross, Circle
        Assert.Equal(0x4000 | 0x1000 | 0x2000, ControllerMapping.SonyButtons(report, dualSense));
    }

    [Theory]
    [InlineData(0, 0x0001)]           // up
    [InlineData(1, 0x0001 | 0x0008)]  // up-right
    [InlineData(2, 0x0008)]           // right
    [InlineData(4, 0x0002)]           // down
    [InlineData(6, 0x0004)]           // left
    [InlineData(7, 0x0001 | 0x0004)]  // up-left
    public void Sony_hat_maps_to_dpad(int hat, int expected)
    {
        var report = NeutralReport(0x01, 64, 8, 1);
        report[8] = (byte)hat;
        Assert.Equal(expected, ControllerMapping.SonyButtons(report, dualSense: true));
    }

    [Theory]
    [InlineData(128, 0, 0x0001)]    // stick up
    [InlineData(128, 255, 0x0002)]  // down
    [InlineData(0, 128, 0x0004)]    // left
    [InlineData(255, 128, 0x0008)]  // right
    [InlineData(150, 100, 0)]       // inside the dead zone
    public void Sony_left_stick_maps_to_dpad(int x, int y, int expected)
    {
        var report = NeutralReport(0x31, 78, 9, 2);
        report[2] = (byte)x;
        report[3] = (byte)y;
        Assert.Equal(expected, ControllerMapping.SonyButtons(report, dualSense: true));
    }

    [Fact]
    public void Sony_options_and_ps_map_to_start_and_guide()
    {
        var report = NeutralReport(0x01, 64, 8, 1);
        report[9] = 0x20;   // Options
        report[10] = 0x01;  // PS
        Assert.Equal(0x0010 | 0x0400, ControllerMapping.SonyButtons(report, dualSense: true));
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x05, 0xFF, 0xFF })]  // unknown report ID
    [InlineData(new byte[] { 0x31, 0, 0 })]         // too short
    public void Unknown_or_short_sony_reports_give_nothing(byte[] report) =>
        Assert.Equal(0, ControllerMapping.SonyButtons(report, dualSense: true));
}
