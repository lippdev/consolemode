namespace ConsoleMode.Services;

/// <summary>One pad's battery. <paramref name="Approximate"/>: XInput only gives four steps (shown with "~").</summary>
public sealed record BatteryReading(string Id, string Name, int Percent, bool Charging, bool Approximate, string Source);

/// <summary>
/// Pure battery logic (issue #70): turns what Windows.Gaming.Input, XInput and the Sony HID
/// reports say into a percentage, picks what to show and decides when to warn.
/// Read on Windows by ControllerBatteryMonitor.
/// </summary>
public static class ControllerBattery
{
    public const string SourceSony = "hid";
    public const string SourceWgi = "wgi";
    public const string SourceXInput = "xinput";

    // XInput BATTERY_TYPE_* and BATTERY_LEVEL_*.
    public const byte XInputTypeDisconnected = 0x00;
    public const byte XInputTypeWired = 0x01;
    public const byte XInputTypeAlkaline = 0x02;
    public const byte XInputTypeNimh = 0x03;

    /// <summary>
    /// Percentage for an XInput battery (type, level 0 empty .. 3 full); null when the slot is
    /// empty, wired or unknown, which have no battery to show.
    /// </summary>
    public static int? FromXInput(byte type, byte level)
    {
        if (type is not (XInputTypeAlkaline or XInputTypeNimh)) return null;
        return level switch
        {
            0 => 5,
            1 => 30,
            2 => 65,
            _ => 100
        };
    }

    /// <summary>
    /// Windows.Gaming.Input BatteryReport (status: 0 NotPresent, 1 Discharging, 2 Idle, 3 Charging).
    /// Null without a battery or without the two capacities.
    /// </summary>
    public static (int Percent, bool Charging)? FromWgi(int? remainingMwh, int? fullMwh, int status)
    {
        if (status == 0 || remainingMwh is not { } remaining || fullMwh is not { } full || full <= 0) return null;
        var percent = (int)Math.Round(Math.Clamp(remaining * 100.0 / full, 0, 100));
        return (percent, status == 3);
    }

    /// <summary>
    /// The battery byte of a DS4 / DualSense input report (first byte = report ID), as the Linux
    /// hid-playstation driver reads it. Null for reports that carry no battery (the short
    /// Bluetooth report 0x01) or an error state.
    /// </summary>
    /// <param name="inputReportLength">HIDP_CAPS.InputReportByteLength, 0 when unknown (see ControllerMapping.SonyButtons).</param>
    public static (int Percent, bool Charging)? FromSonyReport(ReadOnlySpan<byte> report, bool dualSense, int inputReportLength = 0)
    {
        if (report.Length == 0) return null;
        // USB reports are 64 bytes; the basic Bluetooth 0x01 report has the same ID but no battery.
        var usb = report[0] == 0x01 && report.Length >= 64 && inputReportLength is 0 or 64;
        var offset = (report[0], dualSense) switch
        {
            (0x01, true) when usb => 53,   // DualSense USB: status byte
            (0x31, true) => 54,            // DualSense Bluetooth
            (0x01, false) when usb => 30,  // DS4 USB: status[0]
            (0x11, false) => 32,           // DS4 Bluetooth
            _ => -1
        };
        if (offset < 0 || report.Length <= offset) return null;
        return dualSense ? FromDualSenseStatus(report[offset]) : FromDs4Status(report[offset]);
    }

    /// <summary>DualSense status: low nibble level 0..10 (0 = 0-9 %), high nibble 0 discharging, 1 charging, 2 full.</summary>
    public static (int Percent, bool Charging)? FromDualSenseStatus(byte status)
    {
        var level = status & 0x0F;
        return (status >> 4) switch
        {
            0 => (Math.Min(level * 10 + 5, 100), false),
            1 => (Math.Min(level * 10 + 5, 100), true),
            2 => (100, true),
            _ => null
        };
    }

    /// <summary>DS4 status[0]: low nibble level (0..10 on battery, 0..11 on the cable, 11 = full), bit 4 = cable.</summary>
    public static (int Percent, bool Charging)? FromDs4Status(byte status)
    {
        var level = status & 0x0F;
        var cable = (status & 0x10) != 0;
        if (!cable) return (Math.Min(level * 10 + 5, 100), false);
        return level switch
        {
            < 10 => (level * 10 + 5, true),
            10 or 11 => (100, true),
            _ => null
        };
    }

    /// <summary>
    /// One list out of the three sources: the Sony HID value wins for PlayStation pads (Windows
    /// rarely knows their battery), Windows.Gaming.Input covers the rest, and XInput fills in only
    /// when Windows.Gaming.Input reported no non-Sony pad (they would be the same Xbox pads twice).
    /// </summary>
    /// <param name="wgi">Readings with whether each comes from a Sony pad.</param>
    public static IReadOnlyList<BatteryReading> Combine(
        IEnumerable<BatteryReading> sonyHid,
        IEnumerable<(BatteryReading Reading, bool Sony)> wgi,
        IEnumerable<BatteryReading> xinput)
    {
        var result = sonyHid.ToList();
        var hasSony = result.Count > 0;
        var fromWgi = wgi.Where(w => !(w.Sony && hasSony)).ToList();
        result.AddRange(fromWgi.Select(w => w.Reading));
        if (!fromWgi.Any(w => !w.Sony)) result.AddRange(xinput);
        return result;
    }

    /// <summary>What the header shows when several pads report: the emptiest one.</summary>
    public static BatteryReading? Lowest(IEnumerable<BatteryReading> readings) =>
        readings.OrderBy(r => r.Percent).ThenBy(r => r.Charging).FirstOrDefault();

    /// <summary>"65%", or "~65%" when approximate.</summary>
    public static string Format(BatteryReading reading) => $"{(reading.Approximate ? "~" : "")}{reading.Percent}%";

    /// <summary>Segoe Fluent Icons battery glyph in ten steps, with the charging variant.</summary>
    public static string Glyph(int percent, bool charging)
    {
        var step = Math.Clamp((int)Math.Round(percent / 10.0), 0, 10);
        if (charging)
            // BatteryCharging0..8 = E85A..E862, BatteryCharging9 = E83E (also used for a full pad on the cable).
            return step >= 9 ? "" : ((char)(0xE85A + step)).ToString();
        // Battery0..9 = E850..E859, Battery10 = E83F.
        return step == 10 ? "" : ((char)(0xE850 + step)).ToString();
    }
}

/// <summary>
/// When to warn about a low battery, for one pad: once at 15 % or less, once more at 5 % or less,
/// and again only after the pad charged (or reads well above the threshold, new batteries).
/// </summary>
public sealed class LowBatteryAlarm
{
    public const int Low = 15;
    public const int Critical = 5;
    private const int ResetAbove = 25;

    private int _warned; // 0, Low or Critical

    /// <summary>The threshold just crossed (15 or 5) when a warning is due, else null.</summary>
    public int? Update(int percent, bool charging)
    {
        if (charging || percent > ResetAbove)
        {
            _warned = 0;
            return null;
        }
        if (percent <= Critical && _warned != Critical)
        {
            _warned = Critical;
            return Critical;
        }
        if (percent <= Low && _warned == 0)
        {
            _warned = Low;
            return Low;
        }
        return null;
    }
}
