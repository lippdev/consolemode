using System.Diagnostics;
using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Services;
using static ConsoleMode.Native.CcdHelper;

namespace ConsoleMode.Native;

/// <summary>
/// Displays through Windows' own APIs (CCD, ChangeDisplaySettingsEx, DDC/CI via dxva2) (issue #91).
/// Monitors are named by their GDI source (\\.\DISPLAYn), the same names 1.5 saved.
/// </summary>
internal static class NativeDisplays
{
    /// <summary>A connected monitor and the video source it uses (or would use when enabled).</summary>
    public sealed record Monitor(string GdiName, LUID AdapterId, uint SourceId, uint TargetId, bool IsActive,
        int PositionX, int PositionY, string FriendlyName, string DevicePath);

    private const uint QdcAllPaths = 0x1;
    private const uint QdcOnlyActivePaths = 0x2;
    private const uint PathActive = 0x1;
    private const uint ModeIdxInvalid = 0xFFFFFFFF;
    private const uint ModeInfoTypeSource = 1;
    private const uint SdcApply = 0x80, SdcUseSuppliedDisplayConfig = 0x20, SdcSaveToDatabase = 0x200, SdcAllowChanges = 0x400;
    private const uint DeviceInfoGetTargetName = 2;

    // Target (adapter + target id) → source it last had while active, so re-enabling keeps the same \\.\DISPLAYn.
    private static readonly Dictionary<(long Adapter, uint Target), uint> LastSource = [];
    private static readonly object Gate = new();

    public static List<Monitor> ListMonitors()
    {
        if (!Query(QdcAllPaths, out var paths, out var modes)) return [];

        var targets = new List<(long Adapter, uint Target, LUID AdapterId)>();
        var candidates = new Dictionary<(long, uint), List<uint>>();
        var active = new Dictionary<(long, uint), int>();
        for (var i = 0; i < paths.Length; i++)
        {
            ref var p = ref paths[i];
            if (!p.targetInfo.targetAvailable) continue;
            var key = (Key(p.targetInfo.adapterId), p.targetInfo.id);
            if (!candidates.TryGetValue(key, out var list))
            {
                candidates[key] = list = [];
                targets.Add((key.Item1, key.Item2, p.targetInfo.adapterId));
            }
            // Sources are per adapter; this path's source lives on the same adapter as the target.
            if (!list.Contains(p.sourceInfo.id)) list.Add(p.sourceInfo.id);
            if ((p.flags & PathActive) != 0 && !active.ContainsKey(key)) active[key] = i;
        }

        Dictionary<(long, uint), (long Adapter, uint Source)> assigned;
        lock (Gate)
        {
            foreach (var (key, index) in active) LastSource[key] = paths[index].sourceInfo.id;
            // A source is identified by its adapter too, so sources of different adapters never collide.
            assigned = DisplayIdentity.AssignSources<(long, uint), (long Adapter, uint Source)>(targets.Select(t =>
            {
                var key = (t.Adapter, t.Target);
                (long, uint)? activeSource = active.TryGetValue(key, out var idx) ? (t.Adapter, paths[idx].sourceInfo.id) : null;
                (long, uint)? preferred = LastSource.TryGetValue(key, out var last) ? (t.Adapter, last) : null;
                IReadOnlyList<(long, uint)> list = candidates[key].Select(s => (t.Adapter, s)).ToList();
                return (key, activeSource, preferred, list);
            }));
        }

        var result = new List<Monitor>();
        foreach (var t in targets)
        {
            var key = (t.Adapter, t.Target);
            if (!assigned.TryGetValue(key, out var source)) continue;
            var sourceId = source.Source;
            var gdi = GetSourceGdiName(t.AdapterId, sourceId);
            if (string.IsNullOrWhiteSpace(gdi)) continue;

            int x = 0, y = 0;
            var isActive = active.TryGetValue(key, out var pathIndex);
            if (isActive)
            {
                var modeIdx = paths[pathIndex].sourceInfo.modeInfoIdx;
                if (modeIdx < modes.Length && modes[modeIdx].infoType == ModeInfoTypeSource)
                {
                    x = modes[modeIdx].mode.sourceMode.position.x;
                    y = modes[modeIdx].mode.sourceMode.position.y;
                }
            }
            var (friendly, devicePath) = GetTargetName(t.AdapterId, t.Target);
            result.Add(new Monitor(gdi, t.AdapterId, sourceId, t.Target, isActive, x, y, friendly, devicePath));
        }
        return result;
    }

    /// <summary>Attaches inactive monitors (by the name <see cref="ListMonitors"/> gave them) to the desktop.</summary>
    public static int Enable(IReadOnlyCollection<string> gdiNames)
    {
        var wanted = ListMonitors()
            .Where(m => !m.IsActive && gdiNames.Contains(m.GdiName, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (wanted.Count == 0) return 0;
        if (!Query(QdcAllPaths, out var paths, out var modes)) return -1;

        var selected = new List<PATH_INFO>();
        foreach (var p in paths)
            if ((p.flags & PathActive) != 0) selected.Add(p);

        foreach (var m in wanted)
        {
            var index = Array.FindIndex(paths, p =>
                p.targetInfo.targetAvailable && p.targetInfo.id == m.TargetId && p.sourceInfo.id == m.SourceId &&
                Key(p.targetInfo.adapterId) == Key(m.AdapterId));
            if (index < 0)
            {
                AppLog.Write($"Telas: caminho para {m.GdiName} não encontrado");
                continue;
            }
            var path = paths[index];
            path.flags |= PathActive;
            path.sourceInfo.modeInfoIdx = ModeIdxInvalid;
            path.targetInfo.modeInfoIdx = ModeIdxInvalid;
            selected.Add(path);
        }

        var code = SetDisplayConfig((uint)selected.Count, [.. selected], (uint)modes.Length, modes,
            SdcApply | SdcUseSuppliedDisplayConfig | SdcAllowChanges | SdcSaveToDatabase);
        AppLog.Write($"Telas: ativar {string.Join('+', wanted.Select(m => m.GdiName))} => {code}");
        return code;
    }

    /// <summary>Detaches monitors from the desktop. Refuses to detach every active one.</summary>
    public static int Disable(IReadOnlyCollection<string> gdiNames)
    {
        if (!Query(QdcOnlyActivePaths, out var paths, out var modes)) return -1;
        var remaining = 0;
        var changed = false;
        for (var i = 0; i < paths.Length; i++)
        {
            var name = GetSourceGdiName(paths[i].sourceInfo.adapterId, paths[i].sourceInfo.id);
            if (name is not null && gdiNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                paths[i].flags = 0;
                changed = true;
            }
            else remaining++;
        }
        if (!changed) return 0;
        if (remaining == 0)
        {
            AppLog.Write($"Telas: desativar {string.Join('+', gdiNames)} deixaria o PC sem tela; ignorado");
            return -2;
        }
        var code = SetDisplayConfig((uint)paths.Length, paths, (uint)modes.Length, modes,
            SdcApply | SdcUseSuppliedDisplayConfig | SdcSaveToDatabase | SdcAllowChanges);
        AppLog.Write($"Telas: desativar {string.Join('+', gdiNames)} => {code}");
        return code;
    }

    /// <summary>
    /// Queues a mode and/or position for a monitor (applied by <see cref="ApplyPending"/>);
    /// zero or null keeps the current value; a negative orientation leaves the rotation alone.
    /// </summary>
    public static int QueueMode(string gdiName, int width, int height, int frequency, int bitsPerPixel, int? x, int? y, int orientation = -1)
    {
        var dm = new NativeWindows.DEVMODE { dmSize = (short)Marshal.SizeOf<NativeWindows.DEVMODE>() };
        if (!NativeWindows.EnumDisplaySettings(gdiName, NativeWindows.ENUM_CURRENT_SETTINGS, ref dm)) return -1;
        dm.dmFields = 0;
        if (width > 0 && height > 0)
        {
            dm.dmPelsWidth = width;
            dm.dmPelsHeight = height;
            dm.dmFields |= DmPelsWidth | DmPelsHeight;
        }
        if (frequency > 0) { dm.dmDisplayFrequency = frequency; dm.dmFields |= DmDisplayFrequency; }
        if (bitsPerPixel > 0) { dm.dmBitsPerPel = bitsPerPixel; dm.dmFields |= DmBitsPerPel; }
        if (x is not null && y is not null)
        {
            dm.dmPositionX = x.Value;
            dm.dmPositionY = y.Value;
            dm.dmFields |= DmPosition;
        }
        // Only when it differs from the current one: setups that never rotate keep the exact same call.
        if (orientation >= 0 && orientation != dm.dmDisplayOrientation)
        {
            dm.dmDisplayOrientation = orientation;
            dm.dmFields |= DmDisplayOrientation;
        }
        if (dm.dmFields == 0) return 0;
        var code = ChangeDisplaySettingsEx(gdiName, ref dm, 0, CdsUpdateRegistry | CdsNoReset, 0);
        if (code != 0) AppLog.Write($"Telas: modo {width}x{height}@{frequency} em {gdiName} => {code}");
        return code;
    }

    /// <summary>Applies every change queued by <see cref="QueueMode"/> at once.</summary>
    public static int ApplyPending() => ChangeDisplaySettingsEx(null, 0, 0, 0, 0);

    /// <summary>DDC/CI power (VCP 0xD6): on, or off (DPM) so it can still be woken over DDC.</summary>
    public static bool SetPower(string gdiName, bool on)
    {
        var hMonitor = FindHMonitor(gdiName);
        if (hMonitor == 0 || !GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0) return false;
        var physical = new PHYSICAL_MONITOR[count];
        if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physical)) return false;
        try
        {
            var ok = true;
            foreach (var p in physical) ok &= SetVCPFeature(p.hPhysicalMonitor, 0xD6, on ? 1u : 4u);
            if (!ok) AppLog.Write($"Telas: DDC/CI {(on ? "ligar" : "desligar")} {gdiName} falhou");
            return ok;
        }
        finally { DestroyPhysicalMonitors(count, physical); }
    }

    /// <summary>Moves the main windows of a process onto a rectangle.</summary>
    public static void MoveProcessWindows(string processName, ScreenRect rect)
    {
        foreach (var p in Process.GetProcessesByName(processName))
        {
            using (p)
            {
                if (p.MainWindowHandle != 0)
                    NativeWindows.MoveWindowToRect(p.MainWindowHandle, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }

    private static bool Query(uint flags, out PATH_INFO[] paths, out MODE_INFO[] modes)
    {
        paths = [];
        modes = [];
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (GetDisplayConfigBufferSizes(flags, out var numPaths, out var numModes) != 0) return false;
            paths = new PATH_INFO[numPaths];
            modes = new MODE_INFO[numModes];
            var err = QueryDisplayConfig(flags, ref numPaths, paths, ref numModes, modes, 0);
            if (err == ErrorInsufficientBuffer) continue; // the setup changed between the two calls
            if (err != 0) return false;
            Array.Resize(ref paths, (int)numPaths);
            Array.Resize(ref modes, (int)numModes);
            return true;
        }
        return false;
    }

    private static (string Friendly, string DevicePath) GetTargetName(LUID adapterId, uint targetId)
    {
        var req = new TARGET_DEVICE_NAME
        {
            header = new DEVICE_INFO_HEADER
            {
                type = DeviceInfoGetTargetName,
                size = (uint)Marshal.SizeOf<TARGET_DEVICE_NAME>(),
                adapterId = adapterId,
                id = targetId
            }
        };
        return DisplayConfigGetDeviceInfo(ref req) == 0
            ? (req.monitorFriendlyDeviceName ?? "", req.monitorDevicePath ?? "")
            : ("", "");
    }

    /// <summary>Windows' own primary flag (MONITORINFOF_PRIMARY); position (0,0) is ambiguous when screens are cloned.</summary>
    public static bool IsPrimary(string gdiName)
    {
        var hMonitor = FindHMonitor(gdiName);
        if (hMonitor == 0) return false;
        var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        return GetMonitorInfo(hMonitor, ref info) && (info.dwFlags & 1) != 0;
    }

    private static nint FindHMonitor(string gdiName)
    {
        nint found = 0;
        EnumDisplayMonitors(0, 0, (hMonitor, _, _, _) =>
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (GetMonitorInfo(hMonitor, ref info) && string.Equals(info.szDevice, gdiName, StringComparison.OrdinalIgnoreCase))
            {
                found = hMonitor;
                return false;
            }
            return true;
        }, 0);
        return found;
    }

    private static long Key(LUID id) => ((long)id.HighPart << 32) | id.LowPart;

    private const int ErrorInsufficientBuffer = 122;
    private const int DmPosition = 0x20, DmDisplayOrientation = 0x80, DmBitsPerPel = 0x40000, DmPelsWidth = 0x80000, DmPelsHeight = 0x100000, DmDisplayFrequency = 0x400000;
    private const uint CdsUpdateRegistry = 0x1, CdsNoReset = 0x10000000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct TARGET_DEVICE_NAME
    {
        public DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PHYSICAL_MONITOR
    {
        public nint hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    private delegate bool MonitorEnumProc(nint hMonitor, nint hdc, nint rect, nint data);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref TARGET_DEVICE_NAME request);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, ref NativeWindows.DEVMODE devMode, nint hwnd, uint flags, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettingsEx(string? deviceName, nint devMode, nint hwnd, uint flags, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX info);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint hMonitor, out uint count);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(nint hMonitor, uint count, [Out] PHYSICAL_MONITOR[] monitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetVCPFeature(nint hPhysicalMonitor, byte code, uint value);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint count, PHYSICAL_MONITOR[] monitors);
}
