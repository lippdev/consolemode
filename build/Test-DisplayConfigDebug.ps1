#Requires -Version 5.1
$src = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class DcTest {
    private const uint QDC_DATABASE_CURRENT = 0x4;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const int ERROR_SUCCESS = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Explicit, Size = 72)]
    public struct DISPLAYCONFIG_PATH_INFO {
        [FieldOffset(0)] public LUID sourceAdapterId;
        [FieldOffset(8)] public uint sourceId;
        [FieldOffset(68)] public uint flags;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct DISPLAYCONFIG_MODE_INFO {
        [FieldOffset(0)] public uint infoType;
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public LUID adapterId;
    }

    public enum DISPLAYCONFIG_TOPOLOGY_ID : uint { }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER {
        public uint type; public uint size; public LUID adapterId; public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
    }

    [DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);
    [DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint modeCount, [In, Out] DISPLAYCONFIG_MODE_INFO[] modeArray, IntPtr currentTopologyId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME packet);

    public static void Run() {
        Console.WriteLine("PathInfo size={0} ModeInfo size={1}", Marshal.SizeOf(typeof(DISPLAYCONFIG_PATH_INFO)), Marshal.SizeOf(typeof(DISPLAYCONFIG_MODE_INFO)));
        uint pc, mc;
        int e1 = GetDisplayConfigBufferSizes(QDC_DATABASE_CURRENT, out pc, out mc);
        Console.WriteLine("GetDisplayConfigBufferSizes err={0} paths={1} modes={2}", e1, pc, mc);
        if (e1 != ERROR_SUCCESS) return;

        var paths = new DISPLAYCONFIG_PATH_INFO[pc];
        var modes = new DISPLAYCONFIG_MODE_INFO[mc];
        int e2 = QueryDisplayConfig(QDC_DATABASE_CURRENT, ref pc, paths, ref mc, modes, IntPtr.Zero);
        Console.WriteLine("QueryDisplayConfig err={0} paths={1} modes={2}", e2, pc, mc);
        if (e2 != ERROR_SUCCESS) return;

        int n = 1;
        for (int i = 0; i < pc; i++) {
            var req = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            req.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            req.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
            req.header.adapterId = paths[i].sourceAdapterId;
            req.header.id = paths[i].sourceId;
            int e3 = DisplayConfigGetDeviceInfo(ref req);
            if (e3 == ERROR_SUCCESS && !string.IsNullOrEmpty(req.viewGdiDeviceName))
                Console.WriteLine("Monitor {0} = {1} (flags={2})", n++, req.viewGdiDeviceName, paths[i].flags);
        }
    }
}
"@
Add-Type -TypeDefinition $src
[DcTest]::Run()
