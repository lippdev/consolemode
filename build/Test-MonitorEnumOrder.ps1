#Requires -Version 5.1
Add-Type -AssemblyName System.Windows.Forms

$src = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class MonitorEnumTest {
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private static List<string> _devices = new List<string>();

    private static bool Callback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) {
        MONITORINFOEX mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        if (GetMonitorInfo(hMonitor, ref mi)) {
            _devices.Add(mi.szDevice);
            bool primary = (mi.dwFlags & 1) != 0;
            Console.WriteLine("Enum #{0}: {1} primary={2} pos={3},{4}", _devices.Count, mi.szDevice, primary, mi.rcMonitor.left, mi.rcMonitor.top);
        }
        return true;
    }

    public static void Run() {
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
    }
}
"@
Add-Type -TypeDefinition $src
[MonitorEnumTest]::Run()

Write-Host "`nScreen.AllScreens:"
$n = 1
[System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
    Write-Host "  $n : $($_.DeviceName) primary=$($_.Primary) bounds=$($_.Bounds)"
    $n++
}

$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
. (Join-Path $root 'lib\Engine.ps1')

Write-Host "`nMMT monitors:"
Get-ConsoleMonitors | ForEach-Object {
    Write-Host "  Win#$($_.WindowsDisplayNumber) GDI=$($_.Name) $($_.MonitorName) $($_.Frequency)Hz primary=$($_.IsPrimary)"
}
