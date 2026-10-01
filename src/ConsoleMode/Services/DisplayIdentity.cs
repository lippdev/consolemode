using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>
/// One monitor in the layout backup (the .cfg format 1.5 wrote, kept so its backups can be restored).
/// <c>Orientation</c> is DEVMODE.dmDisplayOrientation: 0 landscape, 1 = 90°, 2 = 180°, 3 = 270°.
/// </summary>
public sealed record LayoutEntry(string Name, string MonitorId, string Serial, int BitsPerPixel,
    int Width, int Height, int Frequency, int PositionX, int PositionY, int Orientation = 0);

/// <summary>What the monitor's EDID says about itself.</summary>
public sealed record EdidInfo(string Name, string Serial, int PreferredWidth, int PreferredHeight);

/// <summary>
/// Monitor identity and layout-file helpers for the native display backend (issue #91). They
/// produce the same values 1.5 saved, so saved settings and backups keep working.
/// Pure, so it's tested.
/// </summary>
public static class DisplayIdentity
{
    /// <summary>
    /// <c>\\?\DISPLAY#GSM5B7F#5&amp;2d8e1c7&amp;0&amp;UID4352#{e6f07b5f-…}</c> →
    /// hardware ID <c>GSM5B7F</c>, device instance <c>DISPLAY\GSM5B7F\5&amp;2d8e1c7&amp;0&amp;UID4352</c>.
    /// </summary>
    public static bool TryParseDevicePath(string? devicePath, out string hardwareId, out string instanceId)
    {
        hardwareId = instanceId = "";
        if (string.IsNullOrWhiteSpace(devicePath)) return false;
        var path = devicePath.StartsWith(@"\\?\", StringComparison.Ordinal) ? devicePath[4..] : devicePath;
        var parts = path.Split('#');
        if (parts.Length < 3 || parts[0].Length == 0 || parts[1].Length == 0 || parts[2].Length == 0) return false;
        hardwareId = parts[1];
        instanceId = $@"{parts[0]}\{parts[1]}\{parts[2]}";
        return true;
    }

    /// <summary>
    /// The monitor ID saved since 1.5 (the device ID Windows gives the monitor), e.g.
    /// <c>MONITOR\GSM5B7F\{4d36e96e-e325-11ce-bfc1-08002be10318}\0004</c>; the last part is the
    /// monitor's driver key. It's what <c>MonitorInfo.StableId</c> saves in config.json.
    /// </summary>
    public static string MonitorId(string hardwareId, string? driverKey) =>
        string.IsNullOrWhiteSpace(hardwareId) || string.IsNullOrWhiteSpace(driverKey) ? "" : $@"MONITOR\{hardwareId}\{driverKey}";

    /// <summary>Name (0xFC) and serial (0xFF) descriptors, and the preferred (native) resolution.</summary>
    public static EdidInfo ParseEdid(byte[]? edid)
    {
        if (edid is null || edid.Length < 128) return new EdidInfo("", "", 0, 0);
        string name = "", serial = "";
        int width = 0, height = 0;
        for (var offset = 54; offset <= 108; offset += 18)
        {
            if (edid[offset] != 0 || edid[offset + 1] != 0)
            {
                // Detailed timing; the first one is the preferred mode.
                if (width == 0)
                {
                    width = edid[offset + 2] | ((edid[offset + 4] & 0xF0) << 4);
                    height = edid[offset + 5] | ((edid[offset + 7] & 0xF0) << 4);
                }
                continue;
            }
            var text = DescriptorText(edid, offset);
            switch (edid[offset + 3])
            {
                case 0xFC when name.Length == 0: name = text; break;
                case 0xFF when serial.Length == 0: serial = text; break;
            }
        }
        return new EdidInfo(name, serial, width, height);
    }

    private static string DescriptorText(byte[] edid, int offset)
    {
        var sb = new StringBuilder();
        for (var i = offset + 5; i < offset + 18; i++)
        {
            if (edid[i] == 0x0A || edid[i] == 0) break;
            sb.Append((char)edid[i]);
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Picks a video source for every monitor. Active ones keep theirs; each inactive one gets its
    /// preferred source (the one it last used) when free, else the first free candidate, so no two
    /// monitors share a name (\\.\DISPLAYn) and a re-enabled monitor keeps its old one.
    /// </summary>
    public static Dictionary<TTarget, TSource> AssignSources<TTarget, TSource>(
        IEnumerable<(TTarget Target, TSource? Active, TSource? Preferred, IReadOnlyList<TSource> Candidates)> targets)
        where TTarget : notnull where TSource : struct
    {
        var list = targets.ToList();
        var result = new Dictionary<TTarget, TSource>();
        var used = new HashSet<TSource>();
        foreach (var t in list.Where(t => t.Active is not null))
        {
            result[t.Target] = t.Active!.Value;
            used.Add(t.Active.Value);
        }
        foreach (var t in list.Where(t => t.Active is null))
        {
            TSource? pick = t.Preferred is { } p && t.Candidates.Contains(p) && !used.Contains(p) ? p : null;
            pick ??= t.Candidates.Where(c => !used.Contains(c)).Select(c => (TSource?)c).FirstOrDefault();
            if (pick is not { } chosen) continue;
            result[t.Target] = chosen;
            used.Add(chosen);
        }
        return result;
    }

    public static string FormatLayout(IEnumerable<LayoutEntry> entries)
    {
        var sb = new StringBuilder();
        var i = 0;
        foreach (var e in entries)
        {
            sb.Append($"[Monitor{i++}]\r\n");
            sb.Append($"Name={e.Name}\r\n");
            sb.Append($"MonitorID={e.MonitorId}\r\n");
            sb.Append($"SerialNumber={e.Serial}\r\n");
            sb.Append($"BitsPerPixel={e.BitsPerPixel}\r\n");
            sb.Append($"Width={e.Width}\r\n");
            sb.Append($"Height={e.Height}\r\n");
            sb.Append("DisplayFlags=0\r\n");
            sb.Append($"DisplayFrequency={e.Frequency}\r\n");
            sb.Append($"DisplayOrientation={e.Orientation}\r\n");
            sb.Append($"PositionX={e.PositionX}\r\n");
            sb.Append($"PositionY={e.PositionY}\r\n");
        }
        return sb.ToString();
    }

    /// <summary>Monitor sections of a layout file, keyed by <c>Name</c> (last one wins).</summary>
    public static Dictionary<string, Dictionary<string, string>> ParseLayout(IEnumerable<string> lines)
    {
        var specs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;
        string? currentName = null;
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^\[Monitor\d+\]"))
            {
                if (currentName is not null && current is not null) specs[currentName] = current;
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                currentName = null;
                continue;
            }

            var m = Regex.Match(line, @"^Name=(.+)$");
            if (m.Success && current is not null)
            {
                currentName = m.Groups[1].Value.Trim();
                current["Name"] = currentName;
                continue;
            }

            var kv = Regex.Match(line, @"^(\w+)=(.+)$");
            if (kv.Success && current is not null)
                current[kv.Groups[1].Value] = kv.Groups[2].Value.Trim();
        }

        if (currentName is not null && current is not null) specs[currentName] = current;
        return specs;
    }

    /// <summary>
    /// Monitors that were on in the backup (they have a size) whose current state is wrong: not active, or not
    /// at the saved position. Used to check a restore and to retry it. Specs without a saved position are skipped.
    /// </summary>
    public static List<string> LayoutMismatches(
        IReadOnlyDictionary<string, Dictionary<string, string>> specs,
        IEnumerable<(string Name, bool IsActive, int X, int Y)> current)
    {
        var now = current.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var wrong = new List<string>();
        foreach (var (name, spec) in specs)
        {
            int.TryParse(spec.GetValueOrDefault("Width"), out var width);
            int.TryParse(spec.GetValueOrDefault("Height"), out var height);
            if (width <= 0 || height <= 0) continue; // was off
            if (!int.TryParse(spec.GetValueOrDefault("PositionX"), out var x) ||
                !int.TryParse(spec.GetValueOrDefault("PositionY"), out var y)) continue;
            if (!now.TryGetValue(name, out var monitor) || !monitor.IsActive || monitor.X != x || monitor.Y != y)
                wrong.Add(name);
        }
        return wrong;
    }

    /// <summary>
    /// Re-keys layout sections by the name each monitor has now, matched by <c>MonitorID</c>: a
    /// monitor that was disabled can come back as another \.\DISPLAYn. Sections whose monitor isn't
    /// found keep their saved name.
    /// </summary>
    public static Dictionary<string, Dictionary<string, string>> RemapLayoutNames(
        Dictionary<string, Dictionary<string, string>> specs, IEnumerable<(string MonitorId, string Name)> current)
    {
        var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, name) in current)
            if (!string.IsNullOrWhiteSpace(id)) byId.TryAdd(id, name);

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (savedName, spec) in specs)
        {
            var name = spec.TryGetValue("MonitorID", out var id) && byId.TryGetValue(id, out var now) ? now : savedName;
            var copy = new Dictionary<string, string>(spec, StringComparer.OrdinalIgnoreCase) { ["Name"] = name };
            result[name] = copy;
        }
        return result;
    }
}
