using System.Text.Json.Serialization;

namespace ConsoleMode.Models;

public sealed class AppConfig
{
    public string FocusMonitor { get; set; } = "";
    public List<string> HideMonitors { get; set; } = [];
    public string HideStrategy { get; set; } = "disconnect";
    public string FullscreenMode { get; set; } = "bigPicture";
    public string AudioDeviceId { get; set; } = "";
    public string AudioDeviceName { get; set; } = "";
    public bool AudioAutoSwitch { get; set; }
    public int FpsLimit { get; set; }
    public Dictionary<string, SavedDisplayMode> MonitorModes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool HdrEnable { get; set; }
    public bool VrrEnable { get; set; }
}

public sealed class SavedDisplayMode
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Frequency { get; set; }

    [JsonIgnore]
    public string Key => $"{Width}x{Height}@{Frequency}";
}

public sealed class MonitorInfo
{
    public string Name { get; set; } = "";
    public int WindowsDisplayNumber { get; set; }
    public string Resolution { get; set; } = "N/A";
    public string MaximumResolution { get; set; } = "";
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Frequency { get; set; } = "";
    public string Colors { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public bool IsDisconnected { get; set; }
    public string MonitorName { get; set; } = "";
    public string ShortId { get; set; } = "";
    public string LeftTop { get; set; } = "";

    public string DisplayTitle
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(MonitorName) ? Name : MonitorName;
            var n = WindowsDisplayNumber > 0 ? WindowsDisplayNumber.ToString() : "?";
            var status = IsActive ? "" : "  ·  desconectado";
            return $"{n}. {label}{status}";
        }
    }
}

public sealed class AudioDevice
{
    public string Name { get; set; } = "";
    public string FriendlyId { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class DisplayModeOption
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Frequency { get; set; }
    public int BitsPerPel { get; set; }
    public string Key { get; set; } = "";
    public string Text { get; set; } = "";
    public bool UseCurrent { get; set; }

    public override string ToString() => Text;
}

public sealed class ComboOption
{
    public string Text { get; set; } = "";
    public string Value { get; set; } = "";

    public override string ToString() => Text;
}

public sealed class RestoreResult
{
    public bool Success { get; set; } = true;
    public List<string> Issues { get; } = [];
}

public sealed class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public sealed class ScreenRect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class ConsoleRuntimeState
{
    public bool IsActive { get; set; }
    public bool ShouldExit { get; set; }
    public bool RestoreInProgress { get; set; }
    public bool SteamMoved { get; set; }
    public int MoveCount { get; set; }
    public bool HasAppeared { get; set; }
    public bool ModeLaunched { get; set; }
    public DateTime? LaunchTime { get; set; }
    public int AbsenceCount { get; set; }
    public string? FocusMonitor { get; set; }
    public ScreenRect? FocusMonitorRect { get; set; }
    public string? OriginalPrimary { get; set; }
    public bool FocusWasInactive { get; set; }
    public List<string> HideMonitors { get; set; } = [];
    public string HideStrategy { get; set; } = "disconnect";
    public string FullscreenMode { get; set; } = "bigPicture";
    public string? AudioDeviceId { get; set; }
    public bool AudioAutoSwitch { get; set; }
    public string? AudioDeviceHint { get; set; }
    public HashSet<string> AudioBaselineActiveIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime? LastAudioPoll { get; set; }
    public string? LastAudioSwitchName { get; set; }
    public bool AudioPendingTarget { get; set; }
    public nint CachedBigPictureHandle { get; set; }
    public nint CachedXboxHandle { get; set; }
    public bool BigPictureWatchActive { get; set; }
    public bool AudioWatchComplete { get; set; }
    public int FpsLimit { get; set; }
    public RtssBackup? RtssBackup { get; set; }
    public bool RtssLimitApplied { get; set; }
    public bool HdrApplied { get; set; }
    public string? HdrMonitor { get; set; }
    public bool VrrApplied { get; set; }
    public string? BackupAudioId { get; set; }
}

public sealed class RtssBackup
{
    public int FramerateLimit { get; set; }
    public bool LimiterEnabled { get; set; }
    public string SavedAt { get; set; } = "";
}

public sealed class MonitorBackupMeta
{
    public string? OriginalPrimary { get; set; }
    public bool FocusWasInactive { get; set; }
    public string? FocusMonitor { get; set; }
    public List<string> HideMonitors { get; set; } = [];
    public string? HideStrategy { get; set; }
}

public sealed class MonitorRestoreContext
{
    public string? OriginalPrimary { get; set; }
    public bool FocusWasInactive { get; set; }
    public string? FocusMonitor { get; set; }
    public List<string> HideMonitors { get; set; } = [];
    public string HideStrategy { get; set; } = "disconnect";
}
