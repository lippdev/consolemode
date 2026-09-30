using System.Text;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class DisplayIdentityTests
{
    [Fact]
    public void Device_path_gives_the_hardware_id_and_the_device_instance()
    {
        Assert.True(DisplayIdentity.TryParseDevicePath(
            @"\\?\DISPLAY#GSM5B7F#5&2d8e1c7&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", out var hw, out var instance));
        Assert.Equal("GSM5B7F", hw);
        Assert.Equal(@"DISPLAY\GSM5B7F\5&2d8e1c7&0&UID4352", instance);

        Assert.False(DisplayIdentity.TryParseDevicePath("", out _, out _));
        Assert.False(DisplayIdentity.TryParseDevicePath(@"\\?\DISPLAY#GSM5B7F", out _, out _));
    }

    [Fact]
    public void Monitor_id_has_the_format_saved_by_1_5()
    {
        Assert.Equal(@"MONITOR\GSM5B7F\{4d36e96e-e325-11ce-bfc1-08002be10318}\0004",
            DisplayIdentity.MonitorId("GSM5B7F", @"{4d36e96e-e325-11ce-bfc1-08002be10318}\0004"));
        Assert.Equal("", DisplayIdentity.MonitorId("GSM5B7F", null));
    }

    [Fact]
    public void Edid_gives_name_serial_and_native_resolution()
    {
        var edid = new byte[128];
        // Detailed timing 3840 x 2160 (the preferred mode).
        edid[54] = 0x08; edid[55] = 0xE8;
        edid[56] = 0x00; edid[58] = 0xF0;
        edid[59] = 0x70; edid[61] = 0x80;
        WriteText(edid, 72, 0xFC, "LG TV SSCR2");
        WriteText(edid, 90, 0xFF, "0x01010101");

        var info = DisplayIdentity.ParseEdid(edid);

        Assert.Equal("LG TV SSCR2", info.Name);
        Assert.Equal("0x01010101", info.Serial);
        Assert.Equal(3840, info.PreferredWidth);
        Assert.Equal(2160, info.PreferredHeight);
        Assert.Equal(new EdidInfo("", "", 0, 0), DisplayIdentity.ParseEdid(null));
    }

    [Fact]
    public void Active_monitors_keep_their_source_and_inactive_ones_get_a_free_one()
    {
        IReadOnlyList<int> all = [0, 1, 2];
        var assigned = DisplayIdentity.AssignSources<string, int>(
        [
            ("desk", 0, null, all),
            ("tv", null, 0, all),      // wants 0 (its last one) but the desk has it
            ("side", null, 2, all),    // gets its last one
            ("projector", null, null, all)
        ]);

        Assert.Equal(0, assigned["desk"]);
        Assert.Equal(2, assigned["side"]);
        Assert.Equal(1, assigned["tv"]);
        Assert.False(assigned.ContainsKey("projector")); // no source left
    }

    [Fact]
    public void Layout_file_round_trips_and_follows_renamed_monitors()
    {
        var text = DisplayIdentity.FormatLayout(
        [
            new LayoutEntry(@"\\.\DISPLAY1", @"MONITOR\DEL4083\{x}\0001", "ABC", 32, 2560, 1440, 144, 0, 0),
            new LayoutEntry(@"\\.\DISPLAY3", @"MONITOR\GSM5B7F\{x}\0004", "", 0, 0, 0, 0, 0, 0)
        ]);
        var specs = DisplayIdentity.ParseLayout(text.Split("\r\n"));

        Assert.Equal("2560", specs[@"\\.\DISPLAY1"]["Width"]);
        Assert.Equal("144", specs[@"\\.\DISPLAY1"]["DisplayFrequency"]);
        Assert.Equal("0", specs[@"\\.\DISPLAY3"]["Width"]);

        // The TV came back as DISPLAY2: its section follows it by MonitorID.
        var remapped = DisplayIdentity.RemapLayoutNames(specs,
        [
            (@"MONITOR\DEL4083\{x}\0001", @"\\.\DISPLAY1"),
            (@"MONITOR\GSM5B7F\{x}\0004", @"\\.\DISPLAY2")
        ]);
        Assert.True(remapped.ContainsKey(@"\\.\DISPLAY2"));
        Assert.False(remapped.ContainsKey(@"\\.\DISPLAY3"));
        Assert.Equal(@"\\.\DISPLAY2", remapped[@"\\.\DISPLAY2"]["Name"]);
    }

    [Fact]
    public void Layout_file_keeps_a_rotated_monitor_portrait()
    {
        // A monitor turned to portrait: 1080x1920 at 90 degrees. Restoring it needs the rotation too.
        var text = DisplayIdentity.FormatLayout(
        [
            new LayoutEntry(@"\\.\DISPLAY2", @"MONITOR\HWP3318\{x}\0005", "SERIAL1", 32, 1080, 1920, 60, 1920, -301, Orientation: 1)
        ]);
        var specs = DisplayIdentity.ParseLayout(text.Split("\r\n"));

        Assert.Equal("1", specs[@"\\.\DISPLAY2"]["DisplayOrientation"]);
        Assert.Equal("1080", specs[@"\\.\DISPLAY2"]["Width"]);
        Assert.Equal("1920", specs[@"\\.\DISPLAY2"]["Height"]);
    }

    [Fact]
    public void Layout_file_writes_landscape_when_no_orientation_is_given()
    {
        var text = DisplayIdentity.FormatLayout(
            [new LayoutEntry(@"\\.\DISPLAY1", @"MONITOR\DEL4083\{x}\0001", "", 32, 1920, 1080, 60, 0, 0)]);

        Assert.Contains("DisplayOrientation=0\r\n", text);
    }

    [Fact]
    public void A_backup_made_by_1_5_is_written_back_unchanged()
    {
        // The shape of a real 1.5 backup: a portrait side monitor and a TV that was off.
        string[] original =
        [
            "[Monitor0]", @"Name=\\.\DISPLAY1", @"MonitorID=MONITOR\TCL1003\{4d36e96e-e325-11ce-bfc1-08002be10318}\0003",
            "SerialNumber=", "BitsPerPixel=32", "Width=1920", "Height=1080", "DisplayFlags=0", "DisplayFrequency=240",
            "DisplayOrientation=0", "PositionX=0", "PositionY=0",
            "[Monitor1]", @"Name=\\.\DISPLAY2", @"MonitorID=MONITOR\HWP3318\{4d36e96e-e325-11ce-bfc1-08002be10318}\0005",
            "SerialNumber=SN00000001", "BitsPerPixel=32", "Width=1080", "Height=1920", "DisplayFlags=0", "DisplayFrequency=60",
            "DisplayOrientation=1", "PositionX=1920", "PositionY=-301",
            "[Monitor2]", @"Name=\\.\DISPLAY3", @"MonitorID=MONITOR\GSM0001\{4d36e96e-e325-11ce-bfc1-08002be10318}\0002",
            "SerialNumber=", "BitsPerPixel=0", "Width=0", "Height=0", "DisplayFlags=0", "DisplayFrequency=0",
            "DisplayOrientation=0", "PositionX=0", "PositionY=0"
        ];

        var entries = DisplayIdentity.ParseLayout(original).Values.Select(s => new LayoutEntry(
            s["Name"], s["MonitorID"], s.GetValueOrDefault("SerialNumber") ?? "", int.Parse(s["BitsPerPixel"]),
            int.Parse(s["Width"]), int.Parse(s["Height"]), int.Parse(s["DisplayFrequency"]),
            int.Parse(s["PositionX"]), int.Parse(s["PositionY"]), int.Parse(s["DisplayOrientation"]))).ToList();

        Assert.Equal(original, DisplayIdentity.FormatLayout(entries).Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
    }

    private static void WriteText(byte[] edid, int offset, byte tag, string text)
    {
        edid[offset + 3] = tag;
        var bytes = Encoding.ASCII.GetBytes(text + "\n");
        for (var i = 0; i < 13; i++) edid[offset + 5 + i] = i < bytes.Length ? bytes[i] : (byte)0x20;
    }
}
