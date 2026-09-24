using System.Text.Json.Serialization;
using ConsoleMode.Services;

namespace ConsoleMode.Models;

public sealed class AppConfig
{
    /// <summary>2 = monitor fields hold <see cref="MonitorInfo.StableId"/>; 0/1 = GDI names (\\.\DISPLAYn).</summary>
    public int Version { get; set; }
    public string AppLanguage { get; set; } = "pt-BR";
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

    /// <summary>The first-run tour was finished or skipped.</summary>
    public bool TourDone { get; set; }

    /// <summary>
    /// Game screen + mode the user confirmed they can see ("id|WxH@Hz"). A different setup
    /// asks for confirmation on the TV before launching, and rolls back if nobody answers.
    /// </summary>
    public string ConfirmedSetup { get; set; } = "";

    /// <summary>Look for new GitHub releases on startup.</summary>
    public bool CheckUpdates { get; set; } = true;

    /// <summary>Holding the Xbox Guide (Home) button while in the tray enters console mode.</summary>
    public bool HomeButtonLaunch { get; set; } = true;

    /// <summary>A version the user chose to skip; newer ones are still announced.</summary>
    public string SkippedUpdateVersion { get; set; } = "";

    [JsonIgnore]
    public string SetupKey =>
        $"{FocusMonitor}|{(MonitorModes.TryGetValue(FocusMonitor, out var mode) ? mode.Key : "current")}";
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
    public string MonitorId { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string LeftTop { get; set; } = "";

    /// <summary>
    /// Survives Windows renumbering \\.\DISPLAYn when a screen is disabled and re-enabled.
    /// </summary>
    public string StableId =>
        !string.IsNullOrWhiteSpace(MonitorId) ? MonitorId
        : !string.IsNullOrWhiteSpace(ShortId) ? $"{ShortId}#{SerialNumber}"
        : Name;

    public bool Matches(string idOrName) =>
        !string.IsNullOrWhiteSpace(idOrName) &&
        (string.Equals(StableId, idOrName, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(Name, idOrName, StringComparison.OrdinalIgnoreCase));

    public string FriendlyName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(MonitorName)) return MonitorName;
            if (ShortId.Length >= 3)
            {
                var vendor = PnpVendors.GetValueOrDefault(ShortId[..3]) ?? ShortId[..3];
                return $"{vendor} ({ShortId})";
            }
            return Name.Replace(@"\\.\", "");
        }
    }

    public string ResolutionText
    {
        get
        {
            var w = Width > 0 ? Width : MaxWidth;
            var h = Height > 0 ? Height : MaxHeight;
            if (w <= 0 || h <= 0) return "";
            var hz = int.TryParse(Frequency, out var f) && f > 0 ? $" @ {f} Hz" : "";
            return $"{w} × {h}{hz}";
        }
    }

    public string DisplayTitle
    {
        get
        {
            // WindowsDisplayNumber is CSV order, not the number Windows shows in "Identify".
            var status = IsActive ? "" : $"  ·  {LocalizationService.Get("MonitorOff")}";
            return $"{FriendlyName}{status}";
        }
    }

    private static readonly Dictionary<string, string> PnpVendors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACR"] = "Acer", ["AOC"] = "AOC", ["AUS"] = "ASUS", ["BNQ"] = "BenQ", ["DEL"] = "Dell",
        ["GSM"] = "LG", ["HWP"] = "HP", ["LEN"] = "Lenovo", ["MSI"] = "MSI", ["PHL"] = "Philips",
        ["SAM"] = "Samsung", ["SNY"] = "Sony", ["TCL"] = "TCL", ["VSC"] = "ViewSonic", ["GBT"] = "Gigabyte",
        ["HSD"] = "HannStar", ["IVM"] = "iiyama", ["XMI"] = "Xiaomi", ["HIS"] = "Hisense", ["PHI"] = "Philips"
    };
}

public sealed class AudioDevice
{
    public string Name { get; set; } = "";
    public string FriendlyId { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class DisplayModeOption : System.ComponentModel.INotifyPropertyChanged
{
    private string _text = "";

    public int Width { get; set; }
    public int Height { get; set; }
    public int Frequency { get; set; }
    public int BitsPerPel { get; set; }
    public string Key { get; set; } = "";
    public bool UseCurrent { get; set; }

    /// <summary>
    /// Catalog key of the whole label (the "don't change" entry) or of the suffix such as
    /// " (cache)"; lets <see cref="RefreshText"/> follow a language switch.
    /// </summary>
    public string? TextKey { get; set; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Rebuilds <see cref="Text"/> in the current app language.</summary>
    public DisplayModeOption RefreshText()
    {
        if (UseCurrent)
        {
            if (TextKey is not null) Text = LocalizationService.Get(TextKey);
            return this;
        }
        var freq = Frequency > 0 ? $" @ {Frequency} Hz" : "";
        var suffix = TextKey is null ? "" : LocalizationService.Get(TextKey);
        Text = $"{Width} x {Height}{freq}{suffix}";
        return this;
    }

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
