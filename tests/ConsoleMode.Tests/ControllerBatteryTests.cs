using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerBatteryTests
{
    [Theory]
    [InlineData(ControllerBattery.XInputTypeAlkaline, 0, 5)]
    [InlineData(ControllerBattery.XInputTypeAlkaline, 1, 30)]
    [InlineData(ControllerBattery.XInputTypeNimh, 2, 65)]
    [InlineData(ControllerBattery.XInputTypeNimh, 3, 100)]
    public void Xinput_levels_map_to_rough_percentages(byte type, byte level, int expected) =>
        Assert.Equal(expected, ControllerBattery.FromXInput(type, level));

    [Theory]
    [InlineData(ControllerBattery.XInputTypeDisconnected)]
    [InlineData(ControllerBattery.XInputTypeWired)]
    [InlineData(0xFF)] // BATTERY_TYPE_UNKNOWN
    public void Xinput_without_a_battery_shows_nothing(byte type) =>
        Assert.Null(ControllerBattery.FromXInput(type, 3));

    [Fact]
    public void Wgi_report_becomes_a_percentage()
    {
        Assert.Equal((50, false), ControllerBattery.FromWgi(1000, 2000, 1));
        Assert.Equal((33, true), ControllerBattery.FromWgi(1000, 3000, 3));
        Assert.Equal((100, false), ControllerBattery.FromWgi(2500, 2000, 2)); // clamped
    }

    [Fact]
    public void Wgi_without_battery_or_capacity_shows_nothing()
    {
        Assert.Null(ControllerBattery.FromWgi(1000, 2000, 0)); // NotPresent: wired pad
        Assert.Null(ControllerBattery.FromWgi(null, 2000, 1));
        Assert.Null(ControllerBattery.FromWgi(1000, null, 1));
        Assert.Null(ControllerBattery.FromWgi(1000, 0, 1));
    }

    [Theory]
    [InlineData(0x00, 5, false)]
    [InlineData(0x07, 75, false)]
    [InlineData(0x0A, 100, false)]
    [InlineData(0x13, 35, true)]   // charging, level 3
    [InlineData(0x22, 100, true)]  // full
    public void DualSense_status_byte(byte status, int percent, bool charging) =>
        Assert.Equal((percent, charging), ControllerBattery.FromDualSenseStatus(status));

    [Theory]
    [InlineData(0xA5)]
    [InlineData(0xB0)]
    [InlineData(0xF0)]
    public void DualSense_error_states_show_nothing(byte status) =>
        Assert.Null(ControllerBattery.FromDualSenseStatus(status));

    [Theory]
    [InlineData(0x00, 5, false)]
    [InlineData(0x08, 85, false)]
    [InlineData(0x0A, 100, false)]
    [InlineData(0x14, 45, true)]   // cable, level 4
    [InlineData(0x1A, 100, true)]
    [InlineData(0x1B, 100, true)]  // cable, full
    public void Ds4_status_byte(byte status, int percent, bool charging) =>
        Assert.Equal((percent, charging), ControllerBattery.FromDs4Status(status));

    [Fact]
    public void Ds4_cable_error_shows_nothing() => Assert.Null(ControllerBattery.FromDs4Status(0x1E));

    [Fact]
    public void Reads_the_battery_byte_at_each_report_layout()
    {
        var dsUsb = Report(0x01, 64, 53, 0x06);
        Assert.Equal((65, false), ControllerBattery.FromSonyReport(dsUsb, dualSense: true, 64));
        var dsBt = Report(0x31, 78, 54, 0x12);
        Assert.Equal((25, true), ControllerBattery.FromSonyReport(dsBt, dualSense: true, 78));
        var ds4Usb = Report(0x01, 64, 30, 0x09);
        Assert.Equal((95, false), ControllerBattery.FromSonyReport(ds4Usb, dualSense: false, 64));
        var ds4Bt = Report(0x11, 78, 32, 0x15);
        Assert.Equal((55, true), ControllerBattery.FromSonyReport(ds4Bt, dualSense: false, 547));
    }

    [Fact]
    public void Basic_bluetooth_report_has_no_battery()
    {
        // Short report 0x01, and the same report padded to the Bluetooth descriptor size.
        Assert.Null(ControllerBattery.FromSonyReport(Report(0x01, 10, 5, 0x08), dualSense: false, 547));
        Assert.Null(ControllerBattery.FromSonyReport(Report(0x01, 78, 53, 0x08), dualSense: true, 78));
        Assert.Null(ControllerBattery.FromSonyReport(Report(0x01, 547, 30, 0x08), dualSense: false, 547));
        Assert.Null(ControllerBattery.FromSonyReport(Report(0x05, 64, 30, 0x08), dualSense: false, 64));
        Assert.Null(ControllerBattery.FromSonyReport([], dualSense: true));
    }

    [Fact]
    public void Sony_hid_wins_over_windows_for_playstation_pads()
    {
        var hid = Reading("ds", 40, ControllerBattery.SourceSony);
        var wgiSony = (Reading("ds-wgi", 90, ControllerBattery.SourceWgi), true);
        var wgiXbox = (Reading("xbox", 70, ControllerBattery.SourceWgi), false);
        var xinput = Reading("xinput0", 65, ControllerBattery.SourceXInput, approximate: true);

        var all = ControllerBattery.Combine([hid], [wgiSony, wgiXbox], [xinput]);
        Assert.Equal(["ds", "xbox"], all.Select(r => r.Id));
    }

    [Fact]
    public void Xinput_fills_in_only_when_windows_reported_no_xbox_pad()
    {
        var xinput = Reading("xinput0", 65, ControllerBattery.SourceXInput, approximate: true);
        Assert.Equal(["xinput0"], ControllerBattery.Combine([], [], [xinput]).Select(r => r.Id));
        // A Sony pad seen by Windows doesn't hide the Xbox pad XInput reports.
        var wgiSony = (Reading("ds-wgi", 90, ControllerBattery.SourceWgi), true);
        Assert.Equal(["ds-wgi", "xinput0"], ControllerBattery.Combine([], [wgiSony], [xinput]).Select(r => r.Id));
        Assert.Empty(ControllerBattery.Combine([], [], []));
    }

    [Fact]
    public void Header_shows_the_emptiest_pad()
    {
        Assert.Null(ControllerBattery.Lowest([]));
        var lowest = ControllerBattery.Lowest([Reading("a", 80, "wgi"), Reading("b", 20, "wgi"), Reading("c", 55, "wgi")]);
        Assert.Equal("b", lowest!.Id);
    }

    [Fact]
    public void Approximate_levels_get_a_tilde()
    {
        Assert.Equal("65%", ControllerBattery.Format(Reading("a", 65, "wgi")));
        Assert.Equal("~65%", ControllerBattery.Format(Reading("a", 65, "xinput", approximate: true)));
    }

    [Theory]
    [InlineData(0, false, "")]
    [InlineData(4, false, "")]
    [InlineData(54, false, "")]
    [InlineData(95, false, "")]
    [InlineData(100, false, "")]
    [InlineData(0, true, "")]
    [InlineData(80, true, "")]
    [InlineData(100, true, "")]
    public void Glyph_follows_the_level(int percent, bool charging, string glyph) =>
        Assert.Equal(glyph, ControllerBattery.Glyph(percent, charging));

    [Fact]
    public void Low_battery_warns_once_at_15_and_once_at_5()
    {
        var alarm = new LowBatteryAlarm();
        Assert.Null(alarm.Update(40, false));
        Assert.Equal(15, alarm.Update(15, false));
        Assert.Null(alarm.Update(12, false));
        Assert.Null(alarm.Update(16, false)); // a reading that bounces back up doesn't re-arm it
        Assert.Null(alarm.Update(14, false));
        Assert.Equal(5, alarm.Update(5, false));
        Assert.Null(alarm.Update(3, false));
        Assert.Null(alarm.Update(0, false));
    }

    [Fact]
    public void Low_battery_goes_straight_to_critical_when_first_seen_very_low()
    {
        var alarm = new LowBatteryAlarm();
        Assert.Equal(5, alarm.Update(4, false));
        Assert.Null(alarm.Update(10, false));
        Assert.Null(alarm.Update(2, false));
    }

    [Fact]
    public void Charging_rearms_the_warning()
    {
        var alarm = new LowBatteryAlarm();
        Assert.Equal(15, alarm.Update(10, false));
        Assert.Null(alarm.Update(10, true));
        Assert.Equal(15, alarm.Update(10, false));
    }

    [Fact]
    public void Fresh_batteries_rearm_the_warning()
    {
        var alarm = new LowBatteryAlarm();
        Assert.Equal(15, alarm.Update(15, false));
        Assert.Null(alarm.Update(100, false));
        Assert.Equal(15, alarm.Update(15, false));
    }

    private static byte[] Report(byte id, int length, int offset, byte value)
    {
        var report = new byte[length];
        report[0] = id;
        if (offset < length) report[offset] = value;
        return report;
    }

    private static BatteryReading Reading(string id, int percent, string source, bool approximate = false) =>
        new(id, id, percent, false, approximate, source);
}
