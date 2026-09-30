using System.Runtime.InteropServices;
using System.Text;
using ConsoleMode.Services;

namespace ConsoleMode.Native;

/// <summary>One entry of the in-menu window switcher.</summary>
public sealed record SwitchWindow(nint Handle, string Title, string ProcessName, string? ExePath);

/// <summary>
/// The window switcher behind the session menu: lists the windows Alt + Tab would, in front-to-back
/// order, brings one to the front and asks one to close. Only windows are touched: the session
/// engine decides a session is over by whether Big Picture / Playnite is still visible, not by
/// focus, so switching away from it never ends the session.
/// </summary>
public static class WindowSwitcher
{
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExAppWindow = 0x00040000;
    private const uint GwOwner = 4;
    private const int DwmaCloaked = 14;
    private const int SwRestore = 9;
    private const uint WmClose = 0x0010;

    /// <summary>Windows Alt + Tab would list, front to back. Never throws: a window that vanishes mid-way is skipped.</summary>
    public static List<SwitchWindow> List()
    {
        var result = new List<SwitchWindow>();
        var own = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            try
            {
                var visible = IsWindowVisible(hwnd);
                if (!visible) return true;
                GetWindowThreadProcessId(hwnd, out var pid);
                var title = new StringBuilder(256);
                GetWindowText(hwnd, title, title.Capacity);
                var className = new StringBuilder(128);
                GetClassName(hwnd, className, className.Capacity);
                var style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
                DwmGetWindowAttribute(hwnd, DwmaCloaked, out var cloaked, sizeof(int));

                var facts = new WindowFacts(title.ToString(), className.ToString(), visible, cloaked != 0,
                    GetWindow(hwnd, GwOwner) != 0, (style & WsExToolWindow) != 0, (style & WsExAppWindow) != 0, pid == own);
                if (!AltTabRules.ShouldList(facts)) return true;

                var exe = ExePathOf(pid);
                var name = exe is null ? "" : Path.GetFileName(exe);
                result.Add(new SwitchWindow(hwnd, facts.Title, AltTabRules.ProcessDisplayName(name), exe));
            }
            catch (Exception ex)
            {
                AppLog.Write($"Janelas: {ex.Message}");
            }
            return true;
        }, 0);
        return result;
    }

    /// <summary>Brings a window to the front (restoring it if minimized), without leaving it topmost.</summary>
    public static bool Activate(nint hwnd)
    {
        if (hwnd == 0 || !IsWindow(hwnd)) return false;
        if (IsIconic(hwnd)) ShowWindow(hwnd, SwRestore);
        var ourThread = GetCurrentThreadId();
        var current = GetForegroundWindow();
        var theirThread = current == 0 ? 0 : GetWindowThreadProcessId(current, out _);
        var attached = theirThread != 0 && theirThread != ourThread && AttachThreadInput(ourThread, theirThread, true);
        try
        {
            BringWindowToTop(hwnd);
            return SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached) AttachThreadInput(ourThread, theirThread, false);
        }
    }

    /// <summary>Asks the window to close (the same as its X button: it may still ask to save).</summary>
    public static void RequestClose(nint hwnd)
    {
        if (hwnd != 0 && IsWindow(hwnd)) PostMessage(hwnd, WmClose, 0, 0);
    }

    public static bool Exists(nint hwnd) => hwnd != 0 && IsWindow(hwnd);

    private static string? ExePathOf(uint pid)
    {
        var process = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, pid);
        if (process == 0) return null;
        try
        {
            var path = new StringBuilder(1024);
            var size = path.Capacity;
            return QueryFullProcessImageName(process, 0, path, ref size) ? path.ToString() : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    private delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc proc, nint lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint hwnd, int command);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(nint hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetWindow(nint hwnd, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint hwnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint hwnd, StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint attach, uint to, bool on);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll")] private static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern bool QueryFullProcessImageName(nint process, uint flags, StringBuilder name, ref int size);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
}
