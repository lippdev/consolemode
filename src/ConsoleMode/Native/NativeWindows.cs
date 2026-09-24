using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMode.Native;

public static class NativeWindows
{
    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);
    public delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    public const int ENUM_CURRENT_SETTINGS = -1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_LWIN = 0x5B;
    private const byte VK_F11 = 0x7A;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO info);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public sealed class WindowMatch
    {
        public nint Handle { get; set; }
        public string Title { get; set; } = "";
        public string ClassName { get; set; } = "";
        public long Area { get; set; }
    }

    public sealed class DisplayModeInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Frequency { get; set; }
        public int BitsPerPel { get; set; }
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public static long GetWindowArea(nint hWnd)
    {
        if (!GetWindowRect(hWnd, out var r)) return 0;
        long w = r.Right - r.Left;
        long h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return 0;
        return w * h;
    }

    /// <summary>True when the window covers the whole screen it is on (borderless fullscreen).</summary>
    public static bool IsFullscreen(nint hWnd)
    {
        if (hWnd == 0 || !GetWindowRect(hWnd, out var r)) return false;
        var monitor = MonitorFromWindow(hWnd, 2 /* MONITOR_DEFAULTTONEAREST */);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (monitor == 0 || !GetMonitorInfo(monitor, ref info)) return false;
        var m = info.rcMonitor;
        return r.Left <= m.Left && r.Top <= m.Top && r.Right >= m.Right && r.Bottom >= m.Bottom;
    }

    public static bool IsWindowCenterOnRect(nint hWnd, int left, int top, int width, int height)
    {
        if (hWnd == 0 || width <= 0 || height <= 0) return false;
        if (!GetWindowRect(hWnd, out var r)) return false;
        var cx = (r.Left + r.Right) / 2;
        var cy = (r.Top + r.Bottom) / 2;
        return cx >= left && cx < left + width && cy >= top && cy < top + height;
    }

    public static bool MoveWindowToRect(nint hWnd, int left, int top, int width, int height)
    {
        if (hWnd == 0 || width <= 0 || height <= 0) return false;
        return MoveWindow(hWnd, left, top, width, height, true);
    }

    public static WindowMatch[] GetAllVisibleWindows()
    {
        var list = new List<WindowMatch>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            var t = new StringBuilder(512);
            GetWindowText(hWnd, t, t.Capacity);
            var c = new StringBuilder(256);
            GetClassName(hWnd, c, c.Capacity);
            var area = GetWindowArea(hWnd);
            if (area <= 0) return true;
            list.Add(new WindowMatch
            {
                Handle = hWnd,
                Title = t.ToString(),
                ClassName = c.ToString(),
                Area = area
            });
            return true;
        }, 0);
        return [.. list];
    }

    public static void SendWinF11()
    {
        keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_F11, 0, 0, UIntPtr.Zero);
        keybd_event(VK_F11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static bool IsWindowStillVisible(nint hWnd)
    {
        if (hWnd == 0) return false;
        if (!IsWindow(hWnd)) return false;
        if (!IsWindowVisible(hWnd)) return false;
        return GetWindowArea(hWnd) > 0;
    }

    private static nint _bpHook;
    private static nint _bpWatched;
    private static WinEventDelegate? _bpCallback;

    public static bool BigPictureExitRequested { get; set; }
    public static bool BigPictureWatchActive { get; set; }

    public static bool StartBigPictureExitWatch(nint hwnd)
    {
        StopBigPictureExitWatch();
        if (hwnd == 0 || !IsWindow(hwnd)) return false;

        _bpWatched = hwnd;
        BigPictureExitRequested = false;
        _bpCallback = BigPictureWinEventProc;
        _bpHook = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY, 0, _bpCallback, 0, 0, WINEVENT_OUTOFCONTEXT);
        BigPictureWatchActive = _bpHook != 0;
        return BigPictureWatchActive;
    }

    public static void StopBigPictureExitWatch()
    {
        if (_bpHook != 0)
        {
            UnhookWinEvent(_bpHook);
            _bpHook = 0;
        }
        _bpWatched = 0;
        _bpCallback = null;
        BigPictureExitRequested = false;
        BigPictureWatchActive = false;
    }

    private static void BigPictureWinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd != _bpWatched) return;
        if (eventType == EVENT_OBJECT_DESTROY)
        {
            BigPictureExitRequested = true;
        }
    }

    public static bool ConsumeBigPictureExitRequest()
    {
        if (!BigPictureExitRequested) return false;
        BigPictureExitRequested = false;
        return true;
    }

    private static DisplayModeInfo BuildDisplayModeInfo(int width, int height, int frequency, int bitsPerPel, string suffix)
    {
        var freqPart = frequency > 0 ? $" @ {frequency} Hz" : "";
        return new DisplayModeInfo
        {
            Width = width,
            Height = height,
            Frequency = frequency,
            BitsPerPel = bitsPerPel,
            Key = $"{width}x{height}@{frequency}",
            Text = $"{width} x {height}{freqPart}{suffix}"
        };
    }

    public static DisplayModeInfo[] EnumerateDisplayModes(string deviceName)
    {
        var list = new List<DisplayModeInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        var dm = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        while (EnumDisplaySettings(deviceName, i, ref dm))
        {
            if (dm.dmPelsWidth >= 800 && dm.dmPelsHeight >= 600 &&
                (dm.dmBitsPerPel is 32 or 24 or 16))
            {
                var key = $"{dm.dmPelsWidth}x{dm.dmPelsHeight}@{dm.dmDisplayFrequency}";
                if (seen.Add(key))
                {
                    list.Add(BuildDisplayModeInfo(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel, ""));
                }
            }
            i++;
            dm = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        }

        list.Sort((a, b) =>
        {
            var cmp = b.Width.CompareTo(a.Width);
            if (cmp != 0) return cmp;
            cmp = b.Height.CompareTo(a.Height);
            return cmp != 0 ? cmp : b.Frequency.CompareTo(a.Frequency);
        });
        return [.. list];
    }

    public static DisplayModeInfo? GetCurrentDisplayMode(string deviceName)
    {
        var dm = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm)) return null;
        return BuildDisplayModeInfo(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel,
            ConsoleMode.Services.LocalizationService.Get("CurrentModeSuffix"));
    }
}
