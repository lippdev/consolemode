using System.Diagnostics;
using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.Win32;

namespace ConsoleMode.Services;

public sealed class LaunchService
{
    public void StartBigPicture() => ProcessRunner.StartDetached("steam://open/bigpicture");

    public void StartXbox() => NativeWindows.SendWinF11();

    public void StartPlaynite()
    {
        var exe = GetPlaynitePath();
        if (exe is not null)
        {
            ProcessRunner.StartDetached(exe);
            return;
        }
        ProcessRunner.StartDetached("playnite://playnite/start");
    }

    /// <summary>"customApp": starts the exe from its own folder (emulators look for their files there).</summary>
    public void StartCustomApp(string exePath, string? arguments)
    {
        var exe = CustomApp.CleanPath(exePath);
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments?.Trim() ?? "",
            WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
            UseShellExecute = true
        })?.Dispose();
    }

    /// <summary>
    /// Asks the open JoyChromium to leave full screen: a second run hands its arguments to the open
    /// instance and exits at once, so this doesn't open another browser.
    /// </summary>
    public void ReturnJoyChromiumToWindow()
    {
        if (GetJoyChromiumPath() is not { } exe) return;
        StartCustomApp(exe, JoyChromium.WindowArgument);
    }

    /// <summary>The installed JoyChromium.exe: the per-user install folder, else the uninstall key's icon path.</summary>
    public static string? GetJoyChromiumPath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? displayIcon = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(JoyChromium.UninstallKey);
            displayIcon = key?.GetValue("DisplayIcon") as string;
        }
        catch { /* registry optional */ }
        return JoyChromium.Resolve(string.IsNullOrWhiteSpace(local) ? null : JoyChromium.DefaultPath(local), displayIcon, File.Exists);
    }

    /// <summary>The exe's FileDescription ("RetroArch"), else its file name.</summary>
    public static string GetCustomAppName(string exePath)
    {
        string? description = null;
        try
        {
            var exe = CustomApp.CleanPath(exePath);
            if (File.Exists(exe)) description = FileVersionInfo.GetVersionInfo(exe).FileDescription;
        }
        catch { /* no version info: fall back to the file name */ }
        return CustomApp.DisplayName(exePath, description);
    }

    public static bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var processes = Process.GetProcessesByName(processName);
        foreach (var p in processes) p.Dispose();
        return processes.Length > 0;
    }

    /// <summary>
    /// The processes' main windows, largest first. Windows may report a tooltip as an app's "main" window
    /// (seen with JoyChromium), so the first one is the one most likely to be the app itself.
    /// </summary>
    public static nint[] GetProcessWindows(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return [];
        var handles = new List<nint>();
        foreach (var p in Process.GetProcessesByName(processName))
        {
            using (p)
            {
                if (p.MainWindowHandle != 0) handles.Add(p.MainWindowHandle);
            }
        }
        return [.. handles.OrderByDescending(NativeWindows.GetWindowArea)];
    }

    public bool IsPlayniteAvailable() => GetPlaynitePath() is not null;

    /// <summary>User-chosen Playnite folder or exe (portable installs); tried before auto-detection.</summary>
    public string CustomPlaynitePath { get; set; } = "";

    public string? GetPlaynitePath()
    {
        if (PlaynitePaths.ResolveCustom(CustomPlaynitePath) is { } custom) return custom;

        var candidates = new List<string>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(local))
            candidates.Add(Path.Combine(local, "Playnite", "Playnite.FullscreenApp.exe"));
        candidates.Add(@"C:\Program Files\Playnite\Playnite.FullscreenApp.exe");
        candidates.Add(@"C:\Program Files (x86)\Playnite\Playnite.FullscreenApp.exe");

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Playnite_is1");
            var install = key?.GetValue("InstallLocation") as string;
            if (!string.IsNullOrWhiteSpace(install))
            {
                var exe = Path.Combine(install, "Playnite.FullscreenApp.exe");
                if (File.Exists(exe)) return exe;
            }
        }
        catch { /* registry optional */ }

        return null;
    }

    public nint[] GetBigPictureHandles()
    {
        var all = NativeWindows.GetAllVisibleWindows();
        // The regular Steam window is an SDL_app too: an SDL_app only counts when it fills its
        // screen, otherwise console mode would follow that window and never see Big Picture close.
        var candidates = all
            .Where(w => w.Title.Contains("Big Picture", StringComparison.OrdinalIgnoreCase) ||
                        (w.ClassName == "SDL_app" && NativeWindows.IsFullscreen(w.Handle)))
            .ToList();

        if (candidates.Count == 0)
        {
            foreach (var p in Process.GetProcessesByName("steamwebhelper"))
            {
                if (p.MainWindowHandle == 0) continue;
                var area = NativeWindows.GetWindowArea(p.MainWindowHandle);
                if (area <= 0) continue;
                if (!p.MainWindowTitle.Contains("Big Picture", StringComparison.OrdinalIgnoreCase) &&
                    !NativeWindows.IsFullscreen(p.MainWindowHandle)) continue;
                candidates.Add(new NativeWindows.WindowMatch
                {
                    Handle = p.MainWindowHandle,
                    Title = p.MainWindowTitle,
                    Area = area
                });
            }
        }

        if (candidates.Count == 0) return [];
        var best = candidates
            .OrderByDescending(w =>
            {
                var score = w.Area;
                if (w.Title.Contains("Big Picture", StringComparison.OrdinalIgnoreCase)) score += 1_000_000_000;
                if (w.Title.Contains("Steam", StringComparison.OrdinalIgnoreCase)) score += 100_000_000;
                return score;
            })
            .First();
        return [best.Handle];
    }

    public nint[] GetPlayniteHandles()
    {
        return Process.GetProcessesByName("Playnite.FullscreenApp")
            .Where(p => p.MainWindowHandle != 0)
            .Select(p => p.MainWindowHandle)
            .ToArray();
    }

    public bool IsPlayniteRunning() => Process.GetProcessesByName("Playnite.FullscreenApp").Length > 0;

    public nint[] GetFullscreenHandles(string mode) =>
        mode == "playnite" ? GetPlayniteHandles() : GetBigPictureHandles();

    public bool IsPlayniteActive(ConsoleRuntimeState state)
    {
        if (state.CachedBigPictureHandle != 0)
        {
            if (NativeWindows.IsWindowStillVisible(state.CachedBigPictureHandle) &&
                NativeWindows.GetWindowArea(state.CachedBigPictureHandle) > 200000)
            {
                return true;
            }
            state.CachedBigPictureHandle = 0;
        }

        foreach (var h in GetPlayniteHandles())
        {
            if (NativeWindows.GetWindowArea(h) <= 200000) continue;
            state.CachedBigPictureHandle = h;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Leaves Big Picture the clean way, through Steam's own URL (back to the desktop client), so it does not
    /// stay open on the desk once the screens are back. Halfway through the wait, if it is still there, the
    /// window is asked to close (WM_CLOSE: the X button, not a kill), and only if it belongs to Steam: a game in
    /// full screen can pass for Big Picture in <see cref="GetBigPictureHandles"/>. Returns whether it is gone.
    /// </summary>
    public bool CloseBigPicture(ConsoleRuntimeState state, int timeoutMs = 6000)
    {
        if (!IsBigPictureActive(state)) return true;
        var handles = GetBigPictureHandles();
        try { ProcessRunner.StartDetached("steam://close/bigpicture"); }
        catch (Exception ex) { AppLog.Write($"Fechar Big Picture: {ex.Message}"); }

        var deadline = Environment.TickCount64 + timeoutMs;
        var nudged = false;
        while (Environment.TickCount64 < deadline)
        {
            Thread.Sleep(400);
            if (!IsBigPictureActive(state)) return true;
            if (!nudged && Environment.TickCount64 > deadline - timeoutMs / 2)
            {
                nudged = true;
                foreach (var handle in handles.Concat(GetBigPictureHandles()).Distinct())
                {
                    if (IsSteamWindow(handle)) NativeWindows.PostMessage(handle, NativeWindows.WmClose, 0, 0);
                    else AppLog.Write("Fechar Big Picture: janela em tela cheia não é da Steam, mantida aberta");
                }
            }
        }
        return !IsBigPictureActive(state);
    }

    private static bool IsSteamWindow(nint handle)
    {
        NativeWindows.GetWindowThreadProcessId(handle, out var processId);
        if (processId == 0) return false;
        try
        {
            using var process = Process.GetProcessById(unchecked((int)processId));
            return SteamShutdown.IsSteamProcess(process.ProcessName);
        }
        catch { return false; /* gone, or not ours to read: not known to be Steam, so left alone */ }
    }

    public bool IsBigPictureActive(ConsoleRuntimeState state)
    {
        if (state.CachedBigPictureHandle != 0)
        {
            if (NativeWindows.IsWindowStillVisible(state.CachedBigPictureHandle) &&
                NativeWindows.GetWindowArea(state.CachedBigPictureHandle) > 200000)
            {
                return true;
            }
            state.CachedBigPictureHandle = 0;
        }

        foreach (var h in GetBigPictureHandles())
        {
            if (NativeWindows.GetWindowArea(h) <= 200000) continue;
            state.CachedBigPictureHandle = h;
            return true;
        }
        return false;
    }

    public bool IsXboxActive(ConsoleRuntimeState state)
    {
        if (state.CachedXboxHandle != 0)
        {
            if (NativeWindows.IsWindowStillVisible(state.CachedXboxHandle) &&
                NativeWindows.GetWindowArea(state.CachedXboxHandle) > 250000)
            {
                return true;
            }
            state.CachedXboxHandle = 0;
        }

        foreach (var procName in new[] { "XboxGameCallableUI", "GamingApp", "XboxPcApp" })
        {
            foreach (var p in Process.GetProcessesByName(procName))
            {
                if (p.MainWindowHandle == 0) continue;
                var area = NativeWindows.GetWindowArea(p.MainWindowHandle);
                if (area <= 250000) continue;
                state.CachedXboxHandle = p.MainWindowHandle;
                return true;
            }
        }

        if (!state.HasAppeared)
        {
            foreach (var w in NativeWindows.GetAllVisibleWindows())
            {
                if (w.Area < 250000) continue;
                if (w.Title.Contains("Xbox", StringComparison.OrdinalIgnoreCase) ||
                    w.Title.Contains("Game Pass", StringComparison.OrdinalIgnoreCase) ||
                    w.Title.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ||
                    w.Title.Contains("Modo Xbox", StringComparison.OrdinalIgnoreCase) ||
                    (w.ClassName == "ApplicationFrameWindow" && w.Title.Contains("Xbox", StringComparison.OrdinalIgnoreCase)))
                {
                    state.CachedXboxHandle = w.Handle;
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsFullscreenActive(string mode, ConsoleRuntimeState state) => mode switch
    {
        "bigPicture" => IsBigPictureActive(state),
        "xboxMode" => IsXboxActive(state),
        "playnite" => IsPlayniteActive(state),
        _ => false
    };
}
