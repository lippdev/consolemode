using System.Text.Json.Serialization;
using ConsoleMode.Services;

namespace ConsoleMode.Models;

public sealed class AppConfig
{
    /// <summary>2 = monitor fields hold <see cref="MonitorInfo.StableId"/>; 0/1 = GDI names (\\.\DISPLAYn).</summary>
    public int Version { get; set; }
    /// <summary>Empty until the user (or the installer) picks one; the app then follows Windows.</summary>
    public string AppLanguage { get; set; } = "";
    public string FocusMonitor { get; set; } = "";
    public List<string> HideMonitors { get; set; } = [];
    public string HideStrategy { get; set; } = "disconnect";
    public string FullscreenMode { get; set; } = "bigPicture";

    /// <summary>Playnite folder or exe chosen by the user (portable installs); empty = auto-detect.</summary>
    public string PlaynitePath { get; set; } = "";

    /// <summary>"customApp" mode: the exe to launch, its optional arguments, and an optional process to
    /// watch instead of it (for launcher stubs that hand off to another process).</summary>
    public string CustomAppPath { get; set; } = "";
    public string CustomAppArguments { get; set; } = "";
    public string CustomAppWatchProcess { get; set; } = "";
    public string AudioDeviceId { get; set; } = "";
    public string AudioDeviceName { get; set; } = "";
    public bool AudioAutoSwitch { get; set; }
    public int FpsLimit { get; set; }
    /// <summary>RtssOverlay style: "external" | "off" | "compact" (FPS only) | "stats" | "detailed" | "custom". Applied over the game during a session.</summary>
    public string FpsOverlay { get; set; } = RtssOverlay.External;
    public FpsOverlayLayout FpsOverlayLayout { get; set; } = new();
    public Dictionary<string, SavedDisplayMode> MonitorModes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool HdrEnable { get; set; }
    public bool VrrEnable { get; set; }

    /// <summary>"auto" | "desktop" | "console": which interface to show (UiModeResolver).</summary>
    public string UiMode { get; set; } = "auto";

    /// <summary>"auto" | "xbox" | "playstation": which controller symbols to show in the console UI.</summary>
    public string ControllerLayout { get; set; } = "auto";

    /// <summary>The first-run tour was finished or skipped.</summary>
    public bool TourDone { get; set; }

    /// <summary>The session menu's tour was taken, finished or declined: it is no longer offered when the menu opens.</summary>
    public bool SessionMenuTourDone { get; set; }

    /// <summary>
    /// Game screen + mode the user confirmed they can see ("id|WxH@Hz"). A different setup
    /// asks for confirmation on the TV before launching, and rolls back if nobody answers.
    /// </summary>
    public string ConfirmedSetup { get; set; } = "";

    /// <summary>Look for new GitHub releases on startup.</summary>
    public bool CheckUpdates { get; set; } = true;
    /// <summary>Also offer alpha/beta releases (Settings → Receive test versions).</summary>
    public bool BetaUpdates { get; set; }

    /// <summary>
    /// Controller shortcuts the user picked, as XInput button bits (see ControllerShortcuts);
    /// 0 = not set, which is how every install starts: nothing fires until they choose.
    /// Home: from the tray, enters console mode. Menu: in a session, opens the menu over the
    /// game. Exit: in a session, goes back to the PC.
    /// </summary>
    public int HomeShortcut { get; set; }
    public int MenuShortcut { get; set; }
    public int ExitShortcut { get; set; }

    /// <summary>The shortcuts setup (first run of the version that made them configurable) was finished or skipped.</summary>
    public bool ShortcutsOnboardingDone { get; set; }

    /// <summary>A short press of the Guide button is enough (Game Bar's own shortcut is turned off).</summary>
    public bool HomeButtonShortPress { get; set; }

    /// <summary>
    /// Going back to the PC closes Steam's client window to the tray, unless a game is running.
    /// Big Picture itself is always closed.
    /// </summary>
    public bool CloseSteamOnRestore { get; set; } = true;

    /// <summary>Put the desktop icons back where they were after the session (issue #30).</summary>
    public bool RestoreDesktopIcons { get; set; } = true;

    /// <summary>Move the mouse pointer to a corner of the game screen once the launcher is up, and back afterwards (issue #172).</summary>
    public bool ParkPointer { get; set; } = true;

    /// <summary>Enter console mode when a controller connects while the app is in the tray.</summary>
    public bool AutoStartOnController { get; set; }

    /// <summary>Sounds for moving, picking and going back in the console interface.</summary>
    public bool InterfaceSounds { get; set; } = true;

    /// <summary>Minimizing the main window hides it in the tray instead of leaving it on the taskbar.</summary>
    public bool MinimizeToTray { get; set; }

    /// <summary>Console background: "auto" (the built-in wallpaper), "covers" (of the installed Steam games), "gradient" or "image".</summary>
    public string ConsoleBackground { get; set; } = "auto";

    /// <summary>The picture used when the background is "image".</summary>
    public string ConsoleBackgroundImage { get; set; } = "";

    /// <summary>A version the user chose to skip; newer ones are still announced.</summary>
    public string SkippedUpdateVersion { get; set; } = "";

    /// <summary>Turning the TV on / to the PC's input when console mode starts (issue #75).</summary>
    public TvControlConfig Tv { get; set; } = new();

    /// <summary>Settings applied while a given Steam game runs during a session (issue #69).</summary>
    public List<GameProfile> GameProfiles { get; set; } = [];

    /// <summary>
    /// A copy with other monitor fields and every other setting as it is here. A copy, not a field-by-field
    /// rebuild: a setting added later is carried along without anyone having to remember it.
    /// </summary>
    public AppConfig WithMonitors(string focusMonitor, List<string> hideMonitors, Dictionary<string, SavedDisplayMode> monitorModes)
    {
        var copy = (AppConfig)MemberwiseClone();
        copy.FocusMonitor = focusMonitor;
        copy.HideMonitors = hideMonitors;
        copy.MonitorModes = monitorModes;
        return copy;
    }

    [JsonIgnore]
    public string SetupKey =>
        $"{FocusMonitor}|{(MonitorModes.TryGetValue(FocusMonitor, out var mode) ? mode.Key : "current")}";
}

public sealed class TvControlConfig
{
    public const string None = "none";
    public const string AndroidTv = "androidTv";

    /// <summary>"none" | "androidTv".</summary>
    public string Provider { get; set; } = None;

    /// <summary>The TV's IP address or host name, optionally with ":port".</summary>
    public string Host { get; set; } = "";

    /// <summary>For Wake-on-LAN when the TV is in deep standby; empty = don't send it.</summary>
    public string MacAddress { get; set; } = "";

    /// <summary>HDMI input the PC is plugged into (1-4).</summary>
    public int HdmiInput { get; set; } = 1;

    /// <summary>Android TV: shell command that switches to the PC's input, for TVs that ignore the HDMI key codes.</summary>
    public string InputCommand { get; set; } = "";

    /// <summary>Put the TV in standby after the desk is restored. Off by default.</summary>
    public bool TurnOffOnRestore { get; set; }

    [JsonIgnore]
    public bool IsEnabled => !string.IsNullOrWhiteSpace(Provider) && Provider != None;
}

/// <summary>
/// One game's own settings, saved from the session menu while it runs. Every field left empty keeps
/// the session's value. Applied when the game starts, undone when it closes.
/// </summary>
public sealed class GameProfile
{
    public int SteamAppId { get; set; }
    public string Name { get; set; } = "";
    public SavedDisplayMode? Mode { get; set; }
    public bool? Hdr { get; set; }
    /// <summary>0 = no limit; null = leave the session's.</summary>
    public int? FpsLimit { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
}

/// <summary>What the game screen looked like when a game started, to go back to when it closes.</summary>
public sealed class GameBaseline
{
    public SavedDisplayMode? Mode { get; set; }
    public bool Hdr { get; set; }
    public int FpsLimit { get; set; }
    public string? AudioDeviceId { get; set; }
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
    /// <summary>TV control used for this session, for the optional standby on restore.</summary>
    public TvControlConfig? Tv { get; set; }
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
    public bool CloseSteamOnRestore { get; set; } = true;
    public string? AudioDeviceId { get; set; }
    public bool AudioAutoSwitch { get; set; }
    public string? AudioDeviceHint { get; set; }
    public HashSet<string> AudioBaselineActiveIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime? LastAudioPoll { get; set; }
    public string? LastAudioSwitchName { get; set; }
    public bool AudioPendingTarget { get; set; }
    public nint CachedBigPictureHandle { get; set; }
    public DateTime? PlayniteWindowMissingSince { get; set; }
    /// <summary>"customApp": the process name that keeps the session going, and when it went missing.</summary>
    public string CustomAppWatchName { get; set; } = "";
    public DateTime? CustomAppGoneSince { get; set; }
    public bool CustomAppTimeoutLogged { get; set; }
    public nint CachedXboxHandle { get; set; }
    public bool BigPictureWatchActive { get; set; }
    public bool AudioWatchComplete { get; set; }
    public int FpsLimit { get; set; }
    public RtssBackup? RtssBackup { get; set; }
    public bool RtssLimitApplied { get; set; }
    public bool HdrApplied { get; set; }
    public string? HdrMonitor { get; set; }
    public bool VrrApplied { get; set; }
    /// <summary>The session menu turned off an HDR that was on before the session; Stop() turns it back on.</summary>
    public bool HdrTurnedOffByUser { get; set; }
    public DateTime? SessionStartedAt { get; set; }
    public string? BackupAudioId { get; set; }
    /// <summary>The desktop icons read before the session; null when they aren't restored.</summary>
    public List<DesktopIcon>? DesktopIcons { get; set; }
    /// <summary>Park the pointer this session; how many times it was parked, when, and where it was before.</summary>
    public bool ParkPointer { get; set; }
    public int PointerParks { get; set; }
    public DateTime? LastPointerPark { get; set; }
    public (int X, int Y)? PointerBefore { get; set; }
    /// <summary>Per-game profiles for this session; the session menu updates them as they are saved.</summary>
    public List<GameProfile> GameProfiles { get; set; } = [];
    /// <summary>The Steam game running now (0 = none), and the screen as it was when it started.</summary>
    public int RunningGameId { get; set; }
    public GameBaseline? GameBaseline { get; set; }
    /// <summary>A profile changed the screen for the running game; it is undone when the game closes.</summary>
    public bool GameProfileApplied { get; set; }
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
