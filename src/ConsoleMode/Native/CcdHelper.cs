using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

public static class CcdHelper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct REGION2D
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public RATIONAL refreshRate;
        public uint scanLineOrdering;
        [MarshalAs(UnmanagedType.Bool)] public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_INFO
    {
        public PATH_SOURCE_INFO sourceInfo;
        public PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public RATIONAL hSyncFreq;
        public RATIONAL vSyncFreq;
        public REGION2D activeSize;
        public REGION2D totalSize;
        public uint videoStandard;
        public uint scanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TARGET_MODE
    {
        public VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SOURCE_MODE
    {
        public uint width;
        public uint height;
        public uint pixelFormat;
        public POINTL position;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MODE_UNION
    {
        [FieldOffset(0)] public TARGET_MODE targetMode;
        [FieldOffset(0)] public SOURCE_MODE sourceMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        public MODE_UNION mode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SOURCE_DEVICE_NAME
    {
        public DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GET_ADVANCED_COLOR_INFO
    {
        public DEVICE_INFO_HEADER header;
        public uint value;
        public uint colorEncoding;
        public uint bitsPerColorChannel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SET_ADVANCED_COLOR_STATE
    {
        public DEVICE_INFO_HEADER header;
        public uint enableAdvancedColor;
    }

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPaths, out uint numModes);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(uint flags, ref uint numPaths, [Out] PATH_INFO[] paths, ref uint numModes, [Out] MODE_INFO[] modes, nint topologyId);

    [DllImport("user32.dll")]
    public static extern int SetDisplayConfig(uint numPaths, PATH_INFO[]? paths, uint numModes, MODE_INFO[]? modes, uint flags);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref SOURCE_DEVICE_NAME deviceName);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref GET_ADVANCED_COLOR_INFO info);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigSetDeviceInfo(ref SET_ADVANCED_COLOR_STATE state);

    private const uint QDC_ONLY_ACTIVE_PATHS = 2;
    private const uint MODE_INFO_TYPE_SOURCE = 1;
    private const uint SDC_TOPOLOGY_EXTEND = 0x4;
    private const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x20;
    private const uint SDC_APPLY = 0x80;
    private const uint SDC_SAVE_TO_DATABASE = 0x200;
    private const uint SDC_ALLOW_CHANGES = 0x400;
    private const uint DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
    private const uint DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;

    public static int ExtendAll()
    {
        return SetDisplayConfig(0, null, 0, null, SDC_APPLY | SDC_TOPOLOGY_EXTEND);
    }

    public static int DetachDisplay(string gdiDeviceName)
    {
        var err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var numPaths, out var numModes);
        if (err != 0) return err;
        var paths = new PATH_INFO[numPaths];
        var modes = new MODE_INFO[numModes];
        err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref numPaths, paths, ref numModes, modes, 0);
        if (err != 0) return err;

        var found = false;
        for (var i = 0; i < numPaths; i++)
        {
            var name = GetSourceGdiName(paths[i].sourceInfo.adapterId, paths[i].sourceInfo.id);
            if (string.Equals(name, gdiDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                paths[i].flags = 0;
                found = true;
            }
        }

        if (!found) return -100;
        return SetDisplayConfig(numPaths, paths, numModes, modes,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES);
    }

    private static bool FindTargetForGdiName(string gdiDeviceName, out LUID adapterId, out uint targetId)
    {
        adapterId = new LUID();
        targetId = 0;
        if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var numPaths, out var numModes) != 0) return false;
        var paths = new PATH_INFO[numPaths];
        var modes = new MODE_INFO[numModes];
        if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref numPaths, paths, ref numModes, modes, 0) != 0) return false;

        for (var i = 0; i < numPaths; i++)
        {
            var name = GetSourceGdiName(paths[i].sourceInfo.adapterId, paths[i].sourceInfo.id);
            if (string.Equals(name, gdiDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                adapterId = paths[i].targetInfo.adapterId;
                targetId = paths[i].targetInfo.id;
                return true;
            }
        }

        return false;
    }

    public static int GetHdrStatus(string gdiDeviceName)
    {
        if (!FindTargetForGdiName(gdiDeviceName, out var adapterId, out var targetId)) return -1;

        var info = new GET_ADVANCED_COLOR_INFO
        {
            header =
            {
                type = DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                size = (uint)Marshal.SizeOf<GET_ADVANCED_COLOR_INFO>(),
                adapterId = adapterId,
                id = targetId
            }
        };
        if (DisplayConfigGetDeviceInfo(ref info) != 0) return -1;

        var supported = (info.value & 0x1) != 0;
        var enabled = (info.value & 0x2) != 0;
        if (!supported) return 0;
        return enabled ? 2 : 1;
    }

    public static int SetHdrState(string gdiDeviceName, bool enable)
    {
        if (!FindTargetForGdiName(gdiDeviceName, out var adapterId, out var targetId)) return -1;

        var state = new SET_ADVANCED_COLOR_STATE
        {
            header =
            {
                type = DEVICE_INFO_SET_ADVANCED_COLOR_STATE,
                size = (uint)Marshal.SizeOf<SET_ADVANCED_COLOR_STATE>(),
                adapterId = adapterId,
                id = targetId
            },
            enableAdvancedColor = enable ? 1u : 0u
        };
        return DisplayConfigSetDeviceInfo(ref state);
    }

    private static string? GetSourceGdiName(LUID adapterId, uint sourceId)
    {
        var req = new SOURCE_DEVICE_NAME
        {
            header =
            {
                type = 1,
                size = (uint)Marshal.SizeOf<SOURCE_DEVICE_NAME>(),
                adapterId = adapterId,
                id = sourceId
            }
        };
        return DisplayConfigGetDeviceInfo(ref req) != 0 ? null : req.viewGdiDeviceName;
    }

    public static int SetPrimary(string gdiDeviceName)
    {
        var err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var numPaths, out var numModes);
        if (err != 0) return err;
        var paths = new PATH_INFO[numPaths];
        var modes = new MODE_INFO[numModes];
        err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref numPaths, paths, ref numModes, modes, 0);
        if (err != 0) return err;

        var dx = 0;
        var dy = 0;
        var found = false;
        for (var i = 0; i < numModes; i++)
        {
            if (modes[i].infoType != MODE_INFO_TYPE_SOURCE) continue;
            var name = GetSourceGdiName(modes[i].adapterId, modes[i].id);
            if (!string.Equals(name, gdiDeviceName, StringComparison.OrdinalIgnoreCase)) continue;
            dx = modes[i].mode.sourceMode.position.x;
            dy = modes[i].mode.sourceMode.position.y;
            found = true;
            break;
        }

        if (!found) return -100;
        if (dx == 0 && dy == 0) return 0;

        for (var i = 0; i < numModes; i++)
        {
            if (modes[i].infoType != MODE_INFO_TYPE_SOURCE) continue;
            modes[i].mode.sourceMode.position.x -= dx;
            modes[i].mode.sourceMode.position.y -= dy;
        }

        return SetDisplayConfig(numPaths, paths, numModes, modes,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES);
    }
}
