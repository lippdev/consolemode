using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

/// <summary>
/// Windows performance counters (PDH), the same source Task Manager reads. Counter paths are the
/// English names (PdhAddEnglishCounter), so they work on a Windows in any language.
/// </summary>
public sealed class PerfCounters : IDisposable
{
    private const uint PdhFmtDouble = 0x00000200;
    private const uint PdhFmtNoCap100 = 0x00008000;
    private const int PdhMoreData = unchecked((int)0x800007D2);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern int PdhOpenQueryW(string? dataSource, nint userData, out nint query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern int PdhAddEnglishCounterW(nint query, string path, nint userData, out nint counter);

    [DllImport("pdh.dll")]
    private static extern int PdhCollectQueryData(nint query);

    [DllImport("pdh.dll")]
    private static extern int PdhGetFormattedCounterValue(nint counter, uint format, out uint type, out FmtCounterValue value);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern int PdhGetFormattedCounterArrayW(nint counter, uint format, ref uint bufferSize, out uint itemCount, nint buffer);

    [DllImport("pdh.dll")]
    private static extern int PdhCloseQuery(nint query);

    [StructLayout(LayoutKind.Sequential)]
    private struct FmtCounterValue
    {
        public uint Status;
        public double Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FmtCounterValueItem
    {
        public nint Name;
        public FmtCounterValue Value;
    }

    private nint _query;

    public PerfCounters()
    {
        if (PdhOpenQueryW(null, 0, out _query) != 0) _query = 0;
    }

    /// <summary>0 when the counter doesn't exist on this machine (old Windows, no GPU driver…).</summary>
    public nint Add(string path) =>
        _query != 0 && PdhAddEnglishCounterW(_query, path, 0, out var counter) == 0 ? counter : 0;

    /// <summary>Takes a sample; rates (usage %) need two, so the first read after this returns null.</summary>
    public void Collect()
    {
        if (_query != 0) PdhCollectQueryData(_query);
    }

    public double? Read(nint counter)
    {
        if (counter == 0) return null;
        return PdhGetFormattedCounterValue(counter, PdhFmtDouble | PdhFmtNoCap100, out _, out var value) == 0 && value.Status == 0
            ? value.Value : null;
    }

    /// <summary>Every instance of a wildcard counter (…(*)\…) with its value.</summary>
    public List<(string Instance, double Value)> ReadAll(nint counter)
    {
        var result = new List<(string, double)>();
        if (counter == 0) return result;
        uint size = 0;
        if (PdhGetFormattedCounterArrayW(counter, PdhFmtDouble | PdhFmtNoCap100, ref size, out _, 0) != PdhMoreData || size == 0)
            return result;
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (PdhGetFormattedCounterArrayW(counter, PdhFmtDouble | PdhFmtNoCap100, ref size, out var count, buffer) != 0)
                return result;
            var itemSize = Marshal.SizeOf<FmtCounterValueItem>();
            for (var i = 0; i < count; i++)
            {
                var item = Marshal.PtrToStructure<FmtCounterValueItem>(buffer + i * itemSize);
                if (item.Value.Status == 0 && Marshal.PtrToStringUni(item.Name) is { } name)
                    result.Add((name, item.Value.Value));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return result;
    }

    public void Dispose()
    {
        if (_query == 0) return;
        PdhCloseQuery(_query);
        _query = 0;
    }
}
