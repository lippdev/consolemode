using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Native;

public static class BlackCurtain
{
    private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowExW(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint GetModuleHandleW(string? lpModuleName);

    [DllImport("gdi32.dll")]
    private static extern nint CreateSolidBrush(int crColor);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint hObject);

    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int SW_SHOW = 5;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_DESTROY = 0x0002;
    private const int VK_ESCAPE = 0x1B;
    private const int CS_HREDRAW = 0x0002;
    private const int CS_VREDRAW = 0x0001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    private static readonly List<nint> _windows = [];
    private static nint _brush;
    private static WndProc? _proc;
    private static bool _classRegistered;
    private static Action? _onEscape;

    public static void Show(IEnumerable<ScreenRect> rects, Action onEscape)
    {
        Close();
        _onEscape = onEscape;
        EnsureClass();

        foreach (var rect in rects)
        {
            var hwnd = CreateWindowExW(
                WS_EX_TOPMOST | WS_EX_TOOLWINDOW,
                "ConsoleModeCurtain",
                "",
                WS_POPUP | WS_VISIBLE,
                rect.X, rect.Y, rect.Width, rect.Height,
                0, 0, GetModuleHandleW(null), 0);
            if (hwnd != 0)
            {
                ShowWindow(hwnd, SW_SHOW);
                SetForegroundWindow(hwnd);
                _windows.Add(hwnd);
            }
        }
    }

    public static void Close()
    {
        foreach (var hwnd in _windows.ToArray())
        {
            try { DestroyWindow(hwnd); }
            catch { /* ignore */ }
        }
        _windows.Clear();
        _onEscape = null;
    }

    private static void EnsureClass()
    {
        if (_classRegistered) return;
        _proc = CurtainProc;
        _brush = CreateSolidBrush(0);
        var wc = new WNDCLASS
        {
            style = CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc = _proc,
            hInstance = GetModuleHandleW(null),
            hbrBackground = _brush,
            lpszClassName = "ConsoleModeCurtain"
        };
        RegisterClassW(ref wc);
        _classRegistered = true;
    }

    private static nint CurtainProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_KEYDOWN && wParam == VK_ESCAPE)
        {
            AppLog.Write("Curtain: ESC");
            _onEscape?.Invoke();
            return 0;
        }

        if (msg == WM_DESTROY)
        {
            return 0;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}
