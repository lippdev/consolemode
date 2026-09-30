using System.Runtime.InteropServices;

namespace ConsoleMode.Native;

/// <summary>
/// Strips the classic Windows frame from a borderless overlay window. WinUI's
/// <c>SetBorderAndTitleBar(false, false)</c> removes the title bar and the resize border but leaves
/// <c>WS_DLGFRAME</c> and <c>WS_EX_WINDOWEDGE</c> on, which Windows draws as a light 3 px frame and
/// which also makes the client area smaller than the window (1914x1074 inside 1920x1080).
/// </summary>
public static class WindowChrome
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsBorder = 0x00800000;
    private const long WsDlgFrame = 0x00400000;
    private const long WsThickFrame = 0x00040000;
    private const long WsSysMenu = 0x00080000;
    private const long WsExWindowEdge = 0x00000100;
    private const long WsExClientEdge = 0x00000200;
    private const long WsExDlgModalFrame = 0x00000001;
    private const long WsExStaticEdge = 0x00020000;
    private const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010, SwpFrameChanged = 0x0020;

    /// <summary>Removes every frame style; call before sizing the window so the client area is the whole window.</summary>
    public static void Strip(nint hwnd)
    {
        var style = GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
        var exStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        style &= ~(WsBorder | WsDlgFrame | WsThickFrame | WsSysMenu);
        exStyle &= ~(WsExWindowEdge | WsExClientEdge | WsExDlgModalFrame | WsExStaticEdge);
        SetWindowLongPtr(hwnd, GwlStyle, (nint)style);
        SetWindowLongPtr(hwnd, GwlExStyle, (nint)exStyle);
        // Tell Windows the frame changed, or the old one stays until the next resize.
        SetWindowPos(hwnd, 0, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);
}
