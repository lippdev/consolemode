using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>
/// CPU, GPU and memory for the FPS counter (RtssOverlay), from Windows' own counters and the display
/// driver: nothing to install and no admin. Sample() is meant to be called about once a second;
/// usages need two samples, so the first one has them empty.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly PerfCounters _pdh = new();
    private readonly nint _cpuUsage;
    private readonly nint _cpuPerformance;
    private readonly nint _cpuFrequency;
    private readonly nint _gpuEngines;
    private readonly nint _gpuDedicated;

    public HardwareMonitor()
    {
        // "% Processor Utility" is what Task Manager shows (it counts turbo); older systems lack it.
        _cpuUsage = _pdh.Add(@"\Processor Information(_Total)\% Processor Utility");
        if (_cpuUsage == 0) _cpuUsage = _pdh.Add(@"\Processor Information(_Total)\% Processor Time");
        // Effective clock = base frequency × % performance, Task Manager's "Speed".
        _cpuPerformance = _pdh.Add(@"\Processor Information(_Total)\% Processor Performance");
        _cpuFrequency = _pdh.Add(@"\Processor Information(_Total)\Processor Frequency");
        _gpuEngines = _pdh.Add(@"\GPU Engine(*)\Utilization Percentage");
        _gpuDedicated = _pdh.Add(@"\GPU Adapter Memory(*)\Dedicated Usage");
        _pdh.Collect();
    }

    public HardwareSample Sample()
    {
        _pdh.Collect();

        var frequency = _pdh.Read(_cpuFrequency);
        var performance = _pdh.Read(_cpuPerformance);
        var dedicated = _pdh.ReadAll(_gpuDedicated);
        var gpu = GpuCounters.Busiest(_pdh.ReadAll(_gpuEngines), dedicated);
        var kmt = gpu is { } a ? GpuKmt.Read(a.LuidLow, a.LuidHigh, a.Node) : default;
        var (ramUsed, ramTotal) = SystemMemory.Read();

        return new HardwareSample
        {
            CpuUsage = _pdh.Read(_cpuUsage) is { } cpu ? Math.Clamp(cpu, 0, 100) : null,
            CpuClockMhz = frequency is > 0 && performance is > 0 ? frequency * performance / 100 : null,
            GpuUsage = gpu?.Usage,
            GpuClockMhz = kmt.ClockMhz,
            GpuTempC = kmt.TemperatureC,
            VramUsedMb = gpu is { } g ? GpuCounters.DedicatedMb(dedicated, g.Key) : null,
            VramTotalMb = kmt.DedicatedMb,
            RamUsedMb = ramUsed,
            RamTotalMb = ramTotal
        };
    }

    public void Dispose() => _pdh.Dispose();
}
