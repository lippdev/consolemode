using System.Runtime.InteropServices;
using System.Text;
using ConsoleMode.Services;

namespace ConsoleMode.Native;

/// <summary>An icon as premultiplied BGRA pixels, top row first.</summary>
public sealed record IconBitmap(int Width, int Height, byte[] Pixels);

/// <summary>
/// The program icon of a window for the session menu's switcher, always at the size asked for.
/// Store apps (hosted by ApplicationFrameHost, or installed under WindowsApps) give their tile logo
/// through the shell; other programs the icon of their exe, scaled by Windows; and a window whose
/// exe has none (or can't be read) the icon it shows on the taskbar.
/// </summary>
public static class WindowIcons
{
    /// <summary>Never throws: null means the card keeps its generic icon.</summary>
    public static IconBitmap? Load(SwitchWindow window, int size)
    {
        try
        {
            return FromPackage(window, size) ?? FromExe(window.ExePath, size) ?? FromWindow(window.Handle);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Janelas: ícone de \"{window.ProcessName}\": {ex.Message}");
            return null;
        }
    }

    private static IconBitmap? FromPackage(SwitchWindow window, int size)
    {
        var id = AppUserModelIdOf(window);
        if (id is null) return null;
        var factoryId = typeof(IShellItemImageFactory).GUID;
        if (SHCreateItemFromParsingName($@"shell:AppsFolder\{id}", 0, ref factoryId, out var factory) != 0 || factory is null) return null;
        try
        {
            if (factory.GetImage(new SIZE { cx = size, cy = size }, 0, out var bitmap) != 0 || bitmap == 0) return null;
            try
            {
                // The shell's bitmaps are already premultiplied.
                return ReadBitmap(bitmap) is { } read && !IconPixels.IsBlank(read.Pixels) ? read : null;
            }
            finally { DeleteObject(bitmap); }
        }
        finally { Marshal.ReleaseComObject(factory); }
    }

    /// <summary>The package app behind a window, or null for a classic program.</summary>
    private static string? AppUserModelIdOf(SwitchWindow window)
    {
        GetWindowThreadProcessId(window.Handle, out var pid);
        var isFrameHost = string.Equals(Path.GetFileName(window.ExePath), "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase);
        if (isFrameHost)
        {
            // The frame belongs to the host; the app is the process of the window inside it.
            uint inner = 0;
            EnumChildWindows(window.Handle, (child, _) =>
            {
                GetWindowThreadProcessId(child, out var childPid);
                if (childPid == pid) return true;
                inner = childPid;
                return false;
            }, 0);
            if (inner == 0) return null;
            pid = inner;
        }

        var process = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, pid);
        if (process == 0) return null;
        try
        {
            var length = 512;
            var id = new StringBuilder(length);
            return GetApplicationUserModelId(process, ref length, id) == 0 && id.Length > 0 ? id.ToString() : null;
        }
        finally { CloseHandle(process); }
    }

    private static IconBitmap? FromExe(string? path, int size)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var icons = new nint[1];
        // Picks the closest size stored in the exe and scales it, so a small icon doesn't come back lost in a big frame.
        var count = PrivateExtractIcons(path, 0, size, size, icons, null, 1, 0);
        if (count == 0 || count == uint.MaxValue || icons[0] == 0) return null;
        try { return ReadIcon(icons[0]); }
        finally { DestroyIcon(icons[0]); }
    }

    private static IconBitmap? FromWindow(nint hwnd)
    {
        foreach (var (message, classIndex) in new[] { (IconBig, GclpHIcon), (IconSmall2, GclpHIconSm) })
        {
            var icon = SendMessageTimeout(hwnd, WmGetIcon, message, 0, SmtoAbortIfHung, 150, out var result) != 0 ? result : 0;
            if (icon == 0) icon = GetClassLongPtr(hwnd, classIndex);
            // Owned by the window: read, never destroyed.
            if (icon != 0 && ReadIcon(icon) is { } read) return read;
        }
        return null;
    }

    private static IconBitmap? ReadIcon(nint icon)
    {
        if (!GetIconInfo(icon, out var info)) return null;
        try
        {
            if (info.hbmColor == 0) return null;   // monochrome: not worth showing
            var color = ReadBitmap(info.hbmColor);
            if (color is null) return null;
            var mask = info.hbmMask == 0 ? null : ReadBitmap(info.hbmMask);
            var sameSize = mask is not null && mask.Width == color.Width && mask.Height == color.Height;
            IconPixels.Premultiply(color.Pixels, sameSize ? mask!.Pixels : null);
            return IconPixels.IsBlank(color.Pixels) ? null : color;
        }
        finally
        {
            if (info.hbmColor != 0) DeleteObject(info.hbmColor);
            if (info.hbmMask != 0) DeleteObject(info.hbmMask);
        }
    }

    private static IconBitmap? ReadBitmap(nint bitmap)
    {
        if (GetObject(bitmap, Marshal.SizeOf<BITMAP>(), out var facts) == 0) return null;
        int width = facts.bmWidth, height = Math.Abs(facts.bmHeight);
        if (width <= 0 || height <= 0 || width > 1024 || height > 1024) return null;

        // Negative height: rows come top first.
        var header = new BITMAPINFOHEADER { biSize = Marshal.SizeOf<BITMAPINFOHEADER>(), biWidth = width, biHeight = -height, biPlanes = 1, biBitCount = 32 };
        var pixels = new byte[width * height * 4];
        var screen = GetDC(0);
        try
        {
            return GetDIBits(screen, bitmap, 0, (uint)height, pixels, ref header, 0) == height ? new IconBitmap(width, height, pixels) : null;
        }
        finally { ReleaseDC(0, screen); }
    }

    private const uint WmGetIcon = 0x007F;
    private const nint IconBig = 1;
    private const nint IconSmall2 = 2;
    private const int GclpHIcon = -14;
    private const int GclpHIconSm = -34;
    private const uint SmtoAbortIfHung = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        [MarshalAs(UnmanagedType.Bool)] public bool fIcon;
        public int xHotspot, yHotspot;
        public nint hbmMask, hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public nint bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(SIZE size, int flags, out nint bitmap);
    }

    private delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(string path, nint bindContext, ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory? item);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIcons(string file, int index, int width, int height, nint[] icons, uint[]? ids, uint count, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] private static extern bool GetIconInfo(nint icon, out ICONINFO info);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, EnumProc proc, nint lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    private static extern nint SendMessageTimeout(nint hwnd, uint message, nint wParam, nint lParam, uint flags, uint timeoutMs, out nint result);
    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")] private static extern nint GetClassLongPtr(nint hwnd, int index);
    [DllImport("user32.dll")] private static extern nint GetDC(nint hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint hwnd, nint dc);
    [DllImport("gdi32.dll", EntryPoint = "GetObjectW")] private static extern int GetObject(nint handle, int size, out BITMAP bitmap);
    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(nint dc, nint bitmap, uint start, uint lines, [Out] byte[] bits, ref BITMAPINFOHEADER info, uint usage);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint handle);
    [DllImport("kernel32.dll")] private static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(nint process, ref int length, StringBuilder id);
}
