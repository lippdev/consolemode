using System.Runtime.InteropServices;
using ConsoleMode.Models;

namespace ConsoleMode.Native;

public static class DisplayScreens
{
    private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

    private const int CCHDEVICENAME = 32;
    private const uint MONITORINFOF_PRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string szDevice;
    }

    public static ScreenRect? GetBounds(string deviceName)
    {
        ScreenRect? found = null;
        var normalized = Normalize(deviceName);

        EnumDisplayMonitors(0, 0, (hMonitor, _, _, _) =>
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (!GetMonitorInfo(hMonitor, ref info)) return true;
            if (string.Equals(info.szDevice, deviceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Normalize(info.szDevice), normalized, StringComparison.OrdinalIgnoreCase))
            {
                found = new ScreenRect
                {
                    X = info.rcMonitor.Left,
                    Y = info.rcMonitor.Top,
                    Width = info.rcMonitor.Right - info.rcMonitor.Left,
                    Height = info.rcMonitor.Bottom - info.rcMonitor.Top
                };
                return false;
            }

            return true;
        }, 0);

        return found;
    }

    private static string Normalize(string name) =>
        name.Replace(@"\\.\", "", StringComparison.Ordinal).Trim();
}
