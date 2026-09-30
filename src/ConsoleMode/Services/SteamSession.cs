using System.Diagnostics;
using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// Talks to the Steam client about the end of a session: is it running, is a game running, and the
/// request to quit. Quitting is Steam's own "-shutdown" (what its Exit menu does): it is asked to close,
/// never killed, so it can save and leave cleanly.
/// </summary>
public static class SteamSession
{
    private const string SteamKey = @"Software\Valve\Steam";

    public static bool IsRunning()
    {
        var processes = Process.GetProcessesByName("steam");
        try { return processes.Length > 0; }
        finally { foreach (var p in processes) p.Dispose(); }
    }

    /// <summary>The id of the game Steam is running, or 0.</summary>
    public static int RunningAppId()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamKey);
            return SteamShutdown.ParseRunningAppId(key?.GetValue("RunningAppID"));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Steam: RunningAppID: {ex.Message}");
            return 0;
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
        catch (Exception ex)
        {
            AppLog.Write($"Steam: caminho: {ex.Message}");
        }
        return null;
    }

    /// <summary>Asks Steam to exit the normal way. Returns whether the request was sent; it does not wait.</summary>
    public static bool RequestShutdown()
    {
        var exe = FindSteamExe();
        if (exe is null)
        {
            AppLog.Write("Steam: não achei o steam.exe para fechar");
            return false;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = exe, Arguments = "-shutdown", UseShellExecute = false, CreateNoWindow = true })?.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Steam: pedido de fechar falhou: {ex.Message}");
            return false;
        }
    }
}
