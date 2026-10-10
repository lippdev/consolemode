using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>One reading of the PC for the FPS counter (HardwareMonitor). null = not available on this machine.</summary>
public sealed record HardwareSample
{
    public double? CpuUsage { get; init; }
    public double? CpuClockMhz { get; init; }
    public double? GpuUsage { get; init; }
    public double? GpuClockMhz { get; init; }
    public double? GpuTempC { get; init; }
    public double? VramUsedMb { get; init; }
    public double? VramTotalMb { get; init; }
    public double? RamUsedMb { get; init; }
    public double? RamTotalMb { get; init; }
}

/// <summary>
/// Reads the "GPU Engine" / "GPU Adapter Memory" performance counter instances the way Task Manager
/// does: an adapter's usage is its busiest engine, each engine summed over every process.
/// </summary>
public static partial class GpuCounters
{
    /// <summary>The adapter the game runs on (the busiest one) and the engine node to read its clock from.</summary>
    public readonly record struct Adapter(string Key, uint LuidLow, int LuidHigh, uint Node, double Usage);

    /// <summary>
    /// <paramref name="engines"/>: "\GPU Engine(*)\Utilization Percentage" instances, e.g.
    /// pid_1234_luid_0x00000000_0x0000D1E4_phys_0_eng_0_engtype_3D. <paramref name="dedicated"/>:
    /// "\GPU Adapter Memory(*)\Dedicated Usage" (luid_…_phys_0), the tie-break when every GPU is idle.
    /// </summary>
    public static Adapter? Busiest(IEnumerable<(string Instance, double Value)> engines, IEnumerable<(string Instance, double Value)>? dedicated = null)
    {
        var perEngine = new Dictionary<(string Key, uint Node), (double Sum, bool Is3D)>();
        foreach (var (instance, value) in engines)
        {
            var m = EngineInstance().Match(instance);
            if (!m.Success) continue;
            var key = (AdapterKey(m), uint.Parse(m.Groups["node"].Value, CultureInfo.InvariantCulture));
            var is3D = string.Equals(m.Groups["type"].Value, "3D", StringComparison.OrdinalIgnoreCase);
            perEngine[key] = perEngine.TryGetValue(key, out var e) ? (e.Sum + value, e.Is3D || is3D) : (value, is3D);
        }
        if (perEngine.Count == 0) return null;

        var memory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (instance, value) in dedicated ?? [])
        {
            var m = AdapterInstance().Match(instance);
            if (m.Success) memory[AdapterKey(m)] = value;
        }

        return perEngine.GroupBy(e => e.Key.Key)
            .Select(g =>
            {
                // The clock is read from the 3D engine (the one games load), else the busiest one.
                var node = g.OrderByDescending(e => e.Value.Is3D).ThenBy(e => e.Key.Node).First().Key.Node;
                var m = AdapterInstance().Match(g.Key);
                return new Adapter(g.Key, Hex(m.Groups["low"].Value), (int)Hex(m.Groups["high"].Value), node,
                    Math.Clamp(g.Max(e => e.Value.Sum), 0, 100));
            })
            .OrderByDescending(a => a.Usage)
            .ThenByDescending(a => memory.GetValueOrDefault(a.Key))
            .First();
    }

    /// <summary>"\GPU Adapter Memory(*)\Dedicated Usage" of one adapter, in MB.</summary>
    public static double? DedicatedMb(IEnumerable<(string Instance, double Value)> dedicated, string adapterKey)
    {
        foreach (var (instance, value) in dedicated)
        {
            var m = AdapterInstance().Match(instance);
            if (m.Success && string.Equals(AdapterKey(m), adapterKey, StringComparison.OrdinalIgnoreCase))
                return value / (1024 * 1024);
        }
        return null;
    }

    private static string AdapterKey(Match m) =>
        $"luid_0x{m.Groups["high"].Value}_0x{m.Groups["low"].Value}_phys_{m.Groups["phys"].Value}".ToLowerInvariant();

    private static uint Hex(string value) => uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"luid_0x(?<high>[0-9A-Fa-f]{8})_0x(?<low>[0-9A-Fa-f]{8})_phys_(?<phys>\d+)_eng_(?<node>\d+)_engtype_(?<type>.*)$")]
    private static partial Regex EngineInstance();

    [GeneratedRegex(@"luid_0x(?<high>[0-9A-Fa-f]{8})_0x(?<low>[0-9A-Fa-f]{8})_phys_(?<phys>\d+)")]
    private static partial Regex AdapterInstance();
}
