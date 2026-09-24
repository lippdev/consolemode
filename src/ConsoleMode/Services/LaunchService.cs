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

    /// <summary>
    /// Asks the fullscreen front-end to quit, so leaving console mode from outside doesn't leave
    /// Big Picture / Playnite sitting on the desk monitor. Xbox mode has no window of its own.
    /// Returns once it is gone, or after <paramref name="timeoutMs"/>.
    /// </summary>
    public bool CloseFrontEnd(string mode, ConsoleRuntimeState state, int timeoutMs = 8000)
    {
        if (mode == "xboxMode" || !IsFullscreenActive(mode, state)) return true;
        var handles = GetFullscreenHandles(mode);
        if (mode == "playnite")
        {
            foreach (var p in Process.GetProcessesByName("Playnite.FullscreenApp"))
            {
                try { p.CloseMainWindow(); } catch { /* best effort */ }
            }
        }
        else
        {
            // Steam's own URL leaves Big Picture the clean way (back to the desktop client)
            try { ProcessRunner.StartDetached("steam://close/bigpicture"); }
            catch (Exception ex) { AppLog.Write($"Fechar Big Picture: {ex.Message}"); }
        }

        var deadline = Environment.TickCount64 + timeoutMs;
        var nudged = false;
        while (Environment.TickCount64 < deadline)
        {
            Thread.Sleep(500);
            if (!IsFullscreenActive(mode, state)) return true;
            // halfway through and still there: close the window itself
            if (!nudged && Environment.TickCount64 > deadline - timeoutMs / 2)
            {
                nudged = true;
                foreach (var h in handles.Concat(GetFullscreenHandles(mode)).Distinct())
                    NativeWindows.PostMessage(h, NativeWindows.WM_CLOSE, 0, 0);
            }
        }
        return !IsFullscreenActive(mode, state);
    }

    public bool IsFullscreenActive(string mode, ConsoleRuntimeState state) => mode switch
    {
        "bigPicture" => IsBigPictureActive(state),
        "xboxMode" => IsXboxActive(state),
        "playnite" => IsPlayniteActive(state),
        _ => false
    };
}
