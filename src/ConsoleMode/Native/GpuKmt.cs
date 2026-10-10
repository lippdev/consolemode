using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

/// <summary>
/// GPU temperature, engine clock and VRAM size straight from the display driver (D3DKMT, WDDM 2.4+),
/// the source of Task Manager's GPU temperature: no vendor library, driver or admin needed.
/// Drivers that don't report a value leave it 0, read here as null.
/// </summary>
public static class GpuKmt
{
    private const int KmtqaiGetSegmentSize = 3;
    private const int KmtqaiNodePerfData = 61;
    private const int KmtqaiAdapterPerfData = 62;

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenAdapterFromLuid
    {
        public uint LuidLow;
        public int LuidHigh;
        public uint Adapter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CloseAdapter
    {
        public uint Adapter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QueryAdapterInfo
    {
        public uint Adapter;
        public int Type;
        public nint Data;
        public uint DataSize;
    }

    // d3dkmthk.h: D3DKMT_SEGMENTSIZEINFO, D3DKMT_NODE_PERFDATA, D3DKMT_ADAPTER_PERFDATA (ULONGLONGs are 8-aligned).
    [StructLayout(LayoutKind.Sequential)]
    private struct SegmentSizeInfo
    {
        public ulong DedicatedVideoMemorySize;
        public ulong DedicatedSystemMemorySize;
        public ulong SharedSystemMemorySize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NodePerfData
    {
        public uint NodeOrdinal;
        public uint PhysicalAdapterIndex;
        public ulong Frequency;
        public ulong MaxFrequency;
        public ulong MaxFrequencyOc;
        public uint Voltage;
        public uint VoltageMax;
        public uint VoltageMaxOc;
        public ulong MaxTransitionLatency;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterPerfData
    {
        public uint PhysicalAdapterIndex;
        public ulong MemoryFrequency;
        public ulong MaxMemoryFrequency;
        public ulong MaxMemoryFrequencyOc;
        public ulong MemoryBandwidth;
        public ulong PcieBandwidth;
        public uint FanRpm;
        public uint Power;
        public uint Temperature;
        public byte PowerStateOverride;
    }

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTOpenAdapterFromLuid(ref OpenAdapterFromLuid open);

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTCloseAdapter(ref CloseAdapter close);

    [DllImport("gdi32.dll")]
    private static extern int D3DKMTQueryAdapterInfo(ref QueryAdapterInfo query);

    public readonly record struct Reading(double? TemperatureC, double? ClockMhz, double? DedicatedMb);

    /// <summary>One read of the adapter (LUID as in the GPU performance counters) and its engine <paramref name="node"/>.</summary>
    public static Reading Read(uint luidLow, int luidHigh, uint node)
    {
        var open = new OpenAdapterFromLuid { LuidLow = luidLow, LuidHigh = luidHigh };
        try
        {
            if (D3DKMTOpenAdapterFromLuid(ref open) != 0) return default;
        }
        catch (EntryPointNotFoundException)
        {
            return default;
        }
        try
        {
            var adapter = new AdapterPerfData();
            var engine = new NodePerfData { NodeOrdinal = node };
            var segments = new SegmentSizeInfo();
            double? temperature = Query(open.Adapter, KmtqaiAdapterPerfData, ref adapter) && adapter.Temperature > 0
                ? adapter.Temperature / 10.0 : null;
            double? clock = Query(open.Adapter, KmtqaiNodePerfData, ref engine) && engine.Frequency > 0
                ? engine.Frequency / 1_000_000.0 : null;
            double? dedicated = Query(open.Adapter, KmtqaiGetSegmentSize, ref segments) && segments.DedicatedVideoMemorySize > 0
                ? segments.DedicatedVideoMemorySize / (1024.0 * 1024.0) : null;
            return new Reading(temperature, clock, dedicated);
        }
        finally
        {
            var close = new CloseAdapter { Adapter = open.Adapter };
            D3DKMTCloseAdapter(ref close);
        }
    }

    private static unsafe bool Query<T>(uint adapter, int type, ref T data) where T : unmanaged
    {
        fixed (T* p = &data)
        {
            var query = new QueryAdapterInfo { Adapter = adapter, Type = type, Data = (nint)p, DataSize = (uint)sizeof(T) };
            return D3DKMTQueryAdapterInfo(ref query) == 0;
        }
    }
}
