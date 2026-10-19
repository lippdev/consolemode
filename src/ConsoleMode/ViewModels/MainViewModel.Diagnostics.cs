using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Windows.ApplicationModel.DataTransfer;
using Windows.Gaming.Input;

namespace ConsoleMode.ViewModels;

// Settings → Diagnostics: what the app sees on this PC, as a report to paste into an issue.
// Gathered off the UI thread; each section that throws reads "unavailable" (DiagnosticsReport).
public partial class MainViewModel
{
    private const ushort MicrosoftVendorId = 0x045E;

    [ObservableProperty] private bool _isDiagnosticsOpen;
    [ObservableProperty] private bool _isLoadingDiagnostics;
    [ObservableProperty] private string _diagnosticsText = "";

    partial void OnIsDiagnosticsOpenChanged(bool value)
    {
        if (value && DiagnosticsText.Length == 0) _ = RefreshDiagnosticsAsync();
    }

    [RelayCommand]
    private async Task RefreshDiagnosticsAsync()
    {
        if (IsLoadingDiagnostics) return;
        IsLoadingDiagnostics = true;
        try
        {
            var config = BuildConfig();
            var beta = BetaUpdates;
            DiagnosticsText = await Task.Run(() => DiagnosticsReport.Format(CollectDiagnostics(config, beta)));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Diagnóstico: {ex}");
            DiagnosticsText = DiagnosticsReport.Unavailable;
        }
        finally
        {
            IsLoadingDiagnostics = false;
        }
    }

    [RelayCommand]
    private async Task CopyDiagnosticsReportAsync()
    {
        if (DiagnosticsText.Length == 0) await RefreshDiagnosticsAsync();
        try
        {
            var package = new DataPackage();
            package.SetText(DiagnosticsText);
            Clipboard.SetContent(package);
            SetStatus(LocalizationService.Get("DiagnosticsCopied"), InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Diagnóstico: copiar: {ex.Message}");
        }
    }

    private DiagnosticsData CollectDiagnostics(AppConfig config, bool betaUpdates) => new()
    {
        System = Try("sistema", () => new DiagnosticsSystem(
            UpdateService.CurrentVersion,
            DescribeChannel(betaUpdates),
            DescribeWindows(),
            $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}")),
        Displays = Try("telas", () => Engine.Monitors.GetMonitors(forceRefresh: true).Select(m => new DiagnosticsDisplay(
            $"{m.FriendlyName} ({m.Name.Replace(@"\\.\", "")})",
            m.ResolutionText,
            m.IsActive ? DescribeHdr(m.Name) : "",
            m.IsActive,
            m.IsPrimary,
            m.Matches(config.FocusMonitor),
            config.HideMonitors.Any(m.Matches))).ToList()),
        // A fresh AudioService: the engine's cache belongs to the UI and the console loop.
        Audio = Try("áudio", () => new AudioService().GetDevices().Select(d => new DiagnosticsAudio(
            d.Name,
            d.IsDefault,
            string.Equals(d.FriendlyId, config.AudioDeviceId, StringComparison.OrdinalIgnoreCase))).ToList()),
        Controllers = Try("controles", () => CollectControllers()),
        Apps = Try("programas", () => new List<DiagnosticsApp>
        {
            FoundApp("Steam", SteamSession.FindSteamExe()),
            FoundApp("Playnite", Engine.Launch.GetPlaynitePath()),
            FoundApp("RTSS", Engine.Rtss.GetInstallPath()),
            FoundApp("ControlFS", ControlFsService.FindExe())
        }),
        FullscreenMode = config.FullscreenMode,
        Log = Try("log", () => ReadLogLines())
    };

    /// <summary>
    /// One entry per controller Windows lists, with its battery from the last
    /// ControllerBatteryMonitor reading in Detail (the Sony HID or XInput value for a pad
    /// Windows itself has no battery for).
    /// </summary>
    private static List<DiagnosticsController> CollectControllers()
    {
        var list = new List<DiagnosticsController>();
        var batteries = ControllerBatteryMonitor.Snapshot.ToList();
        foreach (var raw in RawGameController.RawGameControllers)
        {
            var type = raw.HardwareVendorId switch
            {
                ControllerMapping.SonyVendorId => "PlayStation",
                ControllerMapping.NintendoVendorId => "Nintendo",
                MicrosoftVendorId => "Xbox",
                _ => "Other"
            };
            if (Gamepad.FromGameController(raw) is not null) type += " (gamepad)";
            var battery = batteries.FirstOrDefault(b => b.Id == raw.NonRoamableId)
                ?? batteries.FirstOrDefault(b => b.Source == (raw.HardwareVendorId == ControllerMapping.SonyVendorId
                    ? ControllerBattery.SourceSony
                    : ControllerBattery.SourceXInput));
            if (battery is not null) batteries.Remove(battery);
            list.Add(new DiagnosticsController($"{raw.DisplayName} ({raw.HardwareVendorId:X4}:{raw.HardwareProductId:X4})", type,
                battery is null ? null : ControllerBattery.Describe(battery)));
        }
        return list;
    }

    private static T? Try<T>(string section, Func<T> read) where T : class
    {
        try { return read(); }
        catch (Exception ex)
        {
            AppLog.Write($"Diagnóstico: {section}: {ex.Message}");
            return null;
        }
    }

    private static DiagnosticsApp FoundApp(string name, string? path)
    {
        if (path is null) return new DiagnosticsApp(name, null);
        string? version = null;
        try { version = FileVersionInfo.GetVersionInfo(path).ProductVersion?.Trim(); }
        catch { /* the path is enough */ }
        return new DiagnosticsApp(name, path, version);
    }

    private static string DescribeChannel(bool betaUpdates)
    {
        var kind = AppPaths.IsInstalled ? "installed" : "portable";
        var channel = UpdateService.SemVer.Parse(UpdateService.CurrentVersion).IsPrerelease ? "test build"
            : betaUpdates ? "stable, receives test versions"
            : "stable";
        return $"{kind}, {channel}";
    }

    private static string DescribeHdr(string monitorName) => CcdHelper.GetHdrStatus(monitorName) switch
    {
        2 => "on",
        1 => "off",
        0 => "unsupported",
        _ => ""
    };

    private static string DescribeWindows()
    {
        var build = Environment.OSVersion.Version.Build;
        var name = build >= 22000 ? "Windows 11" : "Windows 10";
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var edition = key?.GetValue("EditionID") as string;
        var release = key?.GetValue("DisplayVersion") as string;
        var ubr = key?.GetValue("UBR") is int u ? $".{u}" : "";
        var parts = new[] { name, edition, release }.Where(p => !string.IsNullOrWhiteSpace(p));
        return $"{string.Join(" ", parts)} (build {build}{ubr}, {RuntimeInformation.OSArchitecture})";
    }

    private static List<string> ReadLogLines()
    {
        var path = Path.Combine(AppPaths.DataDir, "consolemode.log");
        if (!File.Exists(path)) return [];
        // AppLog may be writing at the same time.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return lines.Count > DiagnosticsReport.LogLines ? lines.GetRange(lines.Count - DiagnosticsReport.LogLines, DiagnosticsReport.LogLines) : lines;
    }
}
