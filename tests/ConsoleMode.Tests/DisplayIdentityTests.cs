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
    public void Monitor_id_has_MultiMonitorTools_format()
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

    private static void WriteText(byte[] edid, int offset, byte tag, string text)
    {
        edid[offset + 3] = tag;
        var bytes = Encoding.ASCII.GetBytes(text + "\n");
        for (var i = 0; i < 13; i++) edid[offset + 5 + i] = i < bytes.Length ? bytes[i] : (byte)0x20;
    }
}
