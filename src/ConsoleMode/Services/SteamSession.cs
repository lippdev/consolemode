using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>Checks Steam state and closes only its client window without shutting down its process.</summary>
public static class SteamSession
{
    private const string SteamKey = @"Software\Valve\Steam";

    public static bool IsRunning()
    {
        var processes = Process.GetProcessesByName("steam").Concat(Process.GetProcessesByName("steamwebhelper")).ToArray();
        try { return processes.Length > 0; }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    /// <summary>The id of the game Steam is running, 0 if none, or null if the state is unknown.</summary>
    public static int? RunningAppId()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamKey);
            return SteamShutdown.ParseRunningAppId(key?.GetValue("RunningAppID"));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Steam: RunningAppID: {ex.Message}");
            return null;
        }
    }

    public static string? FindSteamExe()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamKey);
            var exe = key?.GetValue("SteamExe") as string;
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe)) return Path.GetFullPath(exe);
            var folder = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                var candidate = Path.Combine(folder, "steam.exe");
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
        }
        catch (Exception ex) { AppLog.Write($"Steam: caminho: {ex.Message}"); }
        return null;
    }

    /// <summary>Closes the Steam client window so Steam can move itself to the tray. Never shuts down the process.</summary>
    public static bool CloseWindowToTray()
    {
        var processes = Process.GetProcessesByName("steam").Concat(Process.GetProcessesByName("steamwebhelper")).ToArray();
        try
        {
            var processNames = processes.ToDictionary(process => process.Id, process => process.ProcessName);
            nint target = 0;
            NativeWindows.EnumWindows((hwnd, _) =>
            {
                NativeWindows.GetWindowThreadProcessId(hwnd, out var processId);
                var title = new StringBuilder(256);
                var className = new StringBuilder(256);
                NativeWindows.GetWindowText(hwnd, title, title.Capacity);
                NativeWindows.GetClassName(hwnd, className, className.Capacity);
                var hasOwner = NativeWindows.GetWindow(hwnd, GwOwner) != 0;
                if (processNames.TryGetValue(unchecked((int)processId), out var processName) &&
                    SteamShutdown.IsClientWindowCandidate(processName, title.ToString(), className.ToString(), NativeWindows.IsWindowVisible(hwnd), hasOwner))
                {
                    target = hwnd;
                    return false;
                }
                return true;
            }, 0);
            return target != 0 && NativeWindows.PostMessage(target, NativeWindows.WmClose, 0, 0);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Steam: fechar janela para a bandeja falhou: {ex.Message}");
            return false;
        }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    private const uint GwOwner = 4;
}
