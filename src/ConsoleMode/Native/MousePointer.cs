using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

/// <summary>Where the mouse pointer is, moving it, and whether a point is on a screen that is on.</summary>
public static class MousePointer
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    private const uint MONITOR_DEFAULTTONULL = 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT point, uint flags);

    public static (int X, int Y)? Position() => GetCursorPos(out var p) ? (p.X, p.Y) : null;

    public static bool MoveTo(int x, int y) => SetCursorPos(x, y);

    /// <summary>True when some screen that is on contains the point.</summary>
    public static bool IsOnScreen(int x, int y) => MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONULL) != 0;
}
