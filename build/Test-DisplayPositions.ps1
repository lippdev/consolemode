#Requires -Version 5.1
$src = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class DcPosTest {
    private const uint QDC_ONLY_ACTIVE_PATHS = 0x2;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const int ERROR_SUCCESS = 0;
    private const uint DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1;

    [StructLayout(LayoutKind.Sequential)] public struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Explicit, Size = 72)]
    public struct DISPLAYCONFIG_PATH_INFO {
        [FieldOffset(0)] public LUID sourceAdapterId;
        [FieldOffset(8)] public uint sourceId;
        [FieldOffset(12)] public uint sourceModeInfoIdx;
        [FieldOffset(68)] public uint flags;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct DISPLAYCONFIG_MODE_INFO {
        [FieldOffset(0)] public uint infoType;
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public LUID adapterId;
        [FieldOffset(16)] public uint width;
        [FieldOffset(20)] public uint height;
        [FieldOffset(24)] public uint pixelFormat;
        [FieldOffset(28)] public int positionX;
        [FieldOffset(32)] public int positionY;
    }

    [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public uint type; public uint size; public LUID adapterId; public uint id; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
    }

    [DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);
    [DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint modeCount, [In, Out] DISPLAYCONFIG_MODE_INFO[] modeArray, IntPtr topologyId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME packet);

    public static void Run() {
        uint pc, mc;
        GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pc, out mc);
        var paths = new DISPLAYCONFIG_PATH_INFO[pc];
        var modes = new DISPLAYCONFIG_MODE_INFO[mc];
        QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pc, paths, ref mc, modes, IntPtr.Zero);

        for (int i = 0; i < pc; i++) {
            var req = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            req.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            req.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
            req.header.adapterId = paths[i].sourceAdapterId;
            req.header.id = paths[i].sourceId;
            DisplayConfigGetDeviceInfo(ref req);

            uint midx = paths[i].sourceModeInfoIdx;
            int px = 0, py = 0, w = 0, h = 0;
            if (midx < modes.Length) {
                px = modes[midx].positionX;
                py = modes[midx].positionY;
                w = (int)modes[midx].width;
                h = (int)modes[midx].height;
            }
            Console.WriteLine("path[{0}] {1} modeIdx={2} pos={3},{4} size={5}x{6} flags={7}", i, req.viewGdiDeviceName, midx, px, py, w, h, paths[i].flags);
        }
    }
}
"@
Add-Type -TypeDefinition $src
[DcPosTest]::Run()
