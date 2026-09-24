using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.Win32;

namespace ConsoleMode.Services;

public sealed class VideoFeaturesService
{
    private const string VrrKey = @"Software\Microsoft\DirectX\UserGpuPreferences";

    public bool EnableHdr(string monitorName, ConsoleRuntimeState state)
    {
        var status = CcdHelper.GetHdrStatus(monitorName);
        if (status < 1)
        {
            AppLog.Write($"HDR não disponível em {monitorName}");
            return false;
        }
        if (status == 2) return true;
        var code = CcdHelper.SetHdrState(monitorName, true);
        if (code != 0)
        {
            AppLog.Write($"Falha ao ativar HDR em {monitorName} (código {code})");
            return false;
        }
        state.HdrApplied = true;
        state.HdrMonitor = monitorName;
        return true;
    }

    public void RestoreHdr(ConsoleRuntimeState state)
    {
        // The session menu turned off an HDR that was on before: put it back.
        if (state.HdrTurnedOffByUser && !string.IsNullOrWhiteSpace(state.FocusMonitor))
        {
            CcdHelper.SetHdrState(state.FocusMonitor, true);
            state.HdrTurnedOffByUser = false;
        }
        if (!state.HdrApplied) return;
        if (!string.IsNullOrWhiteSpace(state.HdrMonitor))
            CcdHelper.SetHdrState(state.HdrMonitor, false);
        state.HdrApplied = false;
        state.HdrMonitor = null;
    }

    /// <summary>Session menu toggle. Tracks what to undo at Stop(): only what the user changed.</summary>
    public bool SetHdrFromMenu(string monitorName, bool on, ConsoleRuntimeState state)
    {
        if (CcdHelper.SetHdrState(monitorName, on) != 0) return false;
        if (on)
        {
            if (state.HdrTurnedOffByUser) state.HdrTurnedOffByUser = false;   // back to how it was
            else { state.HdrApplied = true; state.HdrMonitor = monitorName; }
        }
        else
        {
            if (state.HdrApplied) { state.HdrApplied = false; state.HdrMonitor = null; }
            else state.HdrTurnedOffByUser = true;
        }
        return true;
    }

    public bool IsHdrOn(string monitorName) => CcdHelper.GetHdrStatus(monitorName) == 2;

    public bool EnableVrr(ConsoleRuntimeState state)
    {
        if (IsVrrEnabled()) return true;
        try
        {
            SetVrr(true);
            state.VrrApplied = true;
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Falha ao ativar VRR: {ex.Message}");
            return false;
        }
    }

    public void RestoreVrr(ConsoleRuntimeState state)
    {
        if (!state.VrrApplied) return;
        try { SetVrr(false); } catch { /* ignore */ }
        state.VrrApplied = false;
    }

    private static string GetVrrSetting()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(VrrKey);
            return key?.GetValue("DirectXUserGlobalSettings") as string ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static bool IsVrrEnabled() => GetVrrSetting().Contains("VRROptimizeEnable=1", StringComparison.Ordinal);

    private static void SetVrr(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(VrrKey);
        var current = GetVrrSetting();
        var flag = enabled ? "1" : "0";
        string next;
        if (current.Contains("VRROptimizeEnable=", StringComparison.Ordinal))
            next = System.Text.RegularExpressions.Regex.Replace(current, @"VRROptimizeEnable=\d", $"VRROptimizeEnable={flag}");
        else if (string.IsNullOrWhiteSpace(current))
            next = $"VRROptimizeEnable={flag};";
        else
            next = current.TrimEnd().EndsWith(';') ? $"{current}VRROptimizeEnable={flag};" : $"{current};VRROptimizeEnable={flag};";
        key.SetValue("DirectXUserGlobalSettings", next);
    }
}
