using System.Runtime.InteropServices;
using ConsoleMode.Models;
using Microsoft.UI.Windowing;

namespace ConsoleMode.Native;

/// <summary>Puts a window in the middle of a screen, sized in that screen's DPI (a 4K TV is usually scaled 150-300%).</summary>
public static class WindowPlacement
{
    public static void CenterOn(AppWindow appWindow, ScreenRect? target, double widthDip, double heightDip)
    {
        if (target is null || target.Width <= 0 || target.Height <= 0)
        {
            // No target screen: keep the position, but still size in that screen's DPI.
            var here = DpiAt(appWindow.Position.X + 1, appWindow.Position.Y + 1);
            appWindow.Resize(new Windows.Graphics.SizeInt32((int)(widthDip * here), (int)(heightDip * here)));
            return;
        }

        var cx = target.X + target.Width / 2;
        var cy = target.Y + target.Height / 2;
        var scale = DpiAt(cx, cy);
        var w = (int)(widthDip * scale);
        var h = (int)(heightDip * scale);
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(cx - w / 2, cy - h / 2, w, h));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    public static double DpiAt(int x, int y)
    {
        try
        {
            var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, 2 /* MONITOR_DEFAULTTONEAREST */);
            return GetDpiForMonitor(monitor, 0, out var dpi, out _) == 0 && dpi > 0 ? dpi / 96.0 : 1.0;
        }
        catch
        {
            return 1.0;
        }
    }
}
