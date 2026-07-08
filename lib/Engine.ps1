#Requires -Version 5.1
# Console Mode - Motor de negocio

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$nativeSource = @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class NativeHelpers {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static long GetWindowArea(IntPtr hWnd) {
        RECT r;
        if (!GetWindowRect(hWnd, out r)) return 0;
        long w = r.Right - r.Left;
        long h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return 0;
        return w * h;
    }

    public static System.Collections.Generic.List<WindowMatch> AllMatches = new System.Collections.Generic.List<WindowMatch>();

    public class WindowMatch {
        public IntPtr Handle;
        public string Title;
        public string ClassName;
        public long Area;
    }

    private static bool CollectAllCallback(IntPtr hWnd, IntPtr lParam) {
        if (!IsWindowVisible(hWnd)) return true;
        StringBuilder t = new StringBuilder(512);
        GetWindowText(hWnd, t, t.Capacity);
        StringBuilder c = new StringBuilder(256);
        GetClassName(hWnd, c, c.Capacity);
        long area = GetWindowArea(hWnd);
        if (area <= 0) return true;
        AllMatches.Add(new WindowMatch {
            Handle = hWnd,
            Title = t.ToString(),
            ClassName = c.ToString(),
            Area = area
        });
        return true;
    }

    public static WindowMatch[] GetAllVisibleWindows() {
        AllMatches = new System.Collections.Generic.List<WindowMatch>();
        EnumWindows(CollectAllCallback, IntPtr.Zero);
        return AllMatches.ToArray();
    }

    public static System.Collections.Generic.List<IntPtr> Matched = new System.Collections.Generic.List<IntPtr>();
    private static string _titleSub;
    private static string _classSub;

    private static bool CollectCallback(IntPtr hWnd, IntPtr lParam) {
        if (!IsWindowVisible(hWnd)) return true;
        StringBuilder t = new StringBuilder(512);
        GetWindowText(hWnd, t, t.Capacity);
        StringBuilder c = new StringBuilder(256);
        GetClassName(hWnd, c, c.Capacity);
        string title = t.ToString();
        string cls = c.ToString();
        bool match = false;
        if (!string.IsNullOrEmpty(_titleSub) && title.IndexOf(_titleSub, StringComparison.OrdinalIgnoreCase) >= 0) match = true;
        if (!string.IsNullOrEmpty(_classSub) && cls.Equals(_classSub, StringComparison.OrdinalIgnoreCase)) match = true;
        if (match) Matched.Add(hWnd);
        return true;
    }

    public static IntPtr[] FindWindows(string titleSub, string classExact) {
        Matched = new System.Collections.Generic.List<IntPtr>();
        _titleSub = titleSub;
        _classSub = classExact;
        EnumWindows(CollectCallback, IntPtr.Zero);
        return Matched.ToArray();
    }

    public static bool FoundXboxWindow = false;

    public static bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam) {
        if (!IsWindowVisible(hWnd)) return true;
        StringBuilder sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, sb.Capacity);
        string title = sb.ToString();
        if (title.Contains("Xbox") || title.Contains("Gaming") ||
            title.IndexOf("Full screen", StringComparison.OrdinalIgnoreCase) >= 0 ||
            title.IndexOf("Fullscreen", StringComparison.OrdinalIgnoreCase) >= 0) {
            FoundXboxWindow = true;
            return false;
        }
        return true;
    }

    public static bool SearchXboxWindows() {
        FoundXboxWindow = false;
        EnumWindows(EnumWindowCallback, IntPtr.Zero);
        return FoundXboxWindow;
    }

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_LWIN = 0x5B;
    private const byte VK_F11 = 0x7A;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static void SendWinF11() {
        keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_F11, 0, 0, UIntPtr.Zero);
        keybd_event(VK_F11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static bool IsWindowStillVisible(IntPtr hWnd) {
        if (hWnd == IntPtr.Zero) return false;
        if (!IsWindow(hWnd)) return false;
        if (!IsWindowVisible(hWnd)) return false;
        return GetWindowArea(hWnd) > 0;
    }

    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);

    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    private static IntPtr _bpHook = IntPtr.Zero;
    private static IntPtr _bpWatched = IntPtr.Zero;
    private static WinEventDelegate _bpCallback;

    public static bool BigPictureExitRequested = false;
    public static bool BigPictureWatchActive = false;

    public static bool StartBigPictureExitWatch(IntPtr hwnd) {
        StopBigPictureExitWatch();
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;

        _bpWatched = hwnd;
        BigPictureExitRequested = false;
        _bpCallback = new WinEventDelegate(BigPictureWinEventProc);
        _bpHook = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY, IntPtr.Zero, _bpCallback, 0, 0, WINEVENT_OUTOFCONTEXT);
        BigPictureWatchActive = (_bpHook != IntPtr.Zero);
        return BigPictureWatchActive;
    }

    public static void StopBigPictureExitWatch() {
        if (_bpHook != IntPtr.Zero) {
            UnhookWinEvent(_bpHook);
            _bpHook = IntPtr.Zero;
        }
        _bpWatched = IntPtr.Zero;
        _bpCallback = null;
        BigPictureExitRequested = false;
        BigPictureWatchActive = false;
    }

    private static void BigPictureWinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
        if (hwnd != _bpWatched) return;
        if (eventType == EVENT_OBJECT_DESTROY) {
            BigPictureExitRequested = true;
        }
    }

    public static bool ConsumeBigPictureExitRequest() {
        if (!BigPictureExitRequested) return false;
        BigPictureExitRequested = false;
        return true;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public const int ENUM_CURRENT_SETTINGS = -1;

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

    public class DisplayModeInfo {
        public int Width;
        public int Height;
        public int Frequency;
        public int BitsPerPel;
        public string Key;
        public string Text;
    }

    private static DisplayModeInfo BuildDisplayModeInfo(int width, int height, int frequency, int bitsPerPel, string suffix) {
        string freqPart = frequency > 0 ? (" @ " + frequency + " Hz") : "";
        return new DisplayModeInfo {
            Width = width,
            Height = height,
            Frequency = frequency,
            BitsPerPel = bitsPerPel,
            Key = width + "x" + height + "@" + frequency,
            Text = width + " x " + height + freqPart + suffix
        };
    }

    public static DisplayModeInfo[] EnumerateDisplayModes(string deviceName) {
        var list = new System.Collections.Generic.List<DisplayModeInfo>();
        var seen = new System.Collections.Generic.HashSet<string>();
        int i = 0;
        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        while (EnumDisplaySettings(deviceName, i, ref dm)) {
            if (dm.dmPelsWidth >= 800 && dm.dmPelsHeight >= 600 && (dm.dmBitsPerPel == 32 || dm.dmBitsPerPel == 24 || dm.dmBitsPerPel == 16)) {
                string key = dm.dmPelsWidth + "x" + dm.dmPelsHeight + "@" + dm.dmDisplayFrequency;
                if (!seen.Contains(key)) {
                    seen.Add(key);
                    list.Add(BuildDisplayModeInfo(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel, ""));
                }
            }
            i++;
            dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        }
        list.Sort((a, b) => {
            int cmp = b.Width.CompareTo(a.Width);
            if (cmp != 0) return cmp;
            cmp = b.Height.CompareTo(a.Height);
            if (cmp != 0) return cmp;
            return b.Frequency.CompareTo(a.Frequency);
        });
        return list.ToArray();
    }

    public static DisplayModeInfo GetCurrentDisplayMode(string deviceName) {
        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm)) return null;
        return BuildDisplayModeInfo(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel, " (atual)");
    }
}
"@
if (-not ([System.Management.Automation.PSTypeName]'NativeHelpers').Type) {
    Add-Type -TypeDefinition $nativeSource
}

# Paths definidos por lib/Paths.ps1 via Initialize-ConsoleAppLayout / Set-ConsoleEnginePaths

$Script:ConsoleState = @{
    IsActive       = $false
    ShouldExit     = $false
    CurtainForms   = [System.Collections.ArrayList]@()
    BackupAudioId  = $null
    SteamMoved     = $false
    MoveCount      = 0
    HasAppeared    = $false
    ModeLaunched   = $false
    LaunchTime     = $null
    AbsenceCount   = 0
    FocusMonitor   = $null
    FocusMonitorRect = $null
    OriginalPrimary = $null
    FocusWasInactive = $false
    HideMonitors   = @()
    HideStrategy   = "disconnect"
    FullscreenMode = "bigPicture"
    AudioDeviceId  = $null
    AudioAutoSwitch = $false
    AudioDeviceHint = $null
    AudioBaselineActiveIds = @()
    LastAudioPoll  = $null
    LastAudioSwitchName = $null
    AudioPendingTarget = $false
    CachedBigPictureHandle = [IntPtr]::Zero
    CachedXboxHandle = [IntPtr]::Zero
    BigPictureWatchActive = $false
    RestoreInProgress = $false
    AudioWatchComplete = $false
    FpsLimit = 0
    RtssBackup = $null
    RtssLimitApplied = $false
}

$Script:MonitorListCache = $null
$Script:AudioDevicesCache = $null
$Script:CachedDefaultAudioId = $null
$Script:DisplayModesCache = @{}

function Clear-ConsoleDeviceCache {
    $Script:MonitorListCache = $null
    $Script:AudioDevicesCache = $null
    $Script:CachedDefaultAudioId = $null
    $Script:DisplayModesCache = @{}
}

function Test-MultiMonitorToolAvailable {
    return Test-Path -LiteralPath $Script:MmtPath
}

function Test-SoundVolumeViewAvailable {
    return Test-Path -LiteralPath $Script:SvvPath
}

function Invoke-Mmt {
    param(
        [Parameter(Mandatory)][string[]]$Arguments
    )

    if (-not (Test-MultiMonitorToolAvailable)) {
        throw "MultiMonitorTool.exe não encontrado em $Script:MmtPath"
    }

    $process = Start-Process -FilePath $Script:MmtPath -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
    return $process.ExitCode
}

function Invoke-Svv {
    param(
        [Parameter(Mandatory)][string[]]$Arguments
    )

    if (-not (Test-SoundVolumeViewAvailable)) {
        throw "SoundVolumeView.exe não encontrado em $Script:SvvPath"
    }

    $process = Start-Process -FilePath $Script:SvvPath -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
    return $process.ExitCode
}

function Get-ConsoleMonitors {
    param([switch]$ForceRefresh)

    if (-not $ForceRefresh -and $Script:MonitorListCache) {
        return $Script:MonitorListCache
    }

    $csvPath = Join-Path $env:TEMP "consolemode_monitors.csv"
    $monitors = @()

    try {
        Invoke-Mmt -Arguments @("/HideInactiveMonitors", "0", "/scomma", "`"$csvPath`"") | Out-Null
        if (-not (Test-Path -LiteralPath $csvPath)) {
            $Script:MonitorListCache = @()
            return @()
        }

        $rows = Import-Csv -LiteralPath $csvPath -Encoding UTF8
        $monitors = foreach ($row in $rows) {
            if ([string]::IsNullOrWhiteSpace($row.Name)) { continue }

            $isActive = ($row.Active -eq "Yes")
            $isDisconnected = ($row.Disconnected -eq "Yes") -or (-not $isActive)

            $resolution = $row.Resolution
            if ([string]::IsNullOrWhiteSpace($resolution)) {
                $resolution = if ($row.'Maximum Resolution') { $row.'Maximum Resolution' } else { "N/A" }
            }

            $width = $null
            $height = $null
            if ($resolution -match '(\d+)\s*[Xx]\s*(\d+)') {
                $width = [int]$Matches[1]
                $height = [int]$Matches[2]
            }

            [PSCustomObject]@{
                Name            = $row.Name
                Resolution      = $resolution
                Width           = $width
                Height          = $height
                Frequency       = $row.Frequency
                Colors          = $row.Colors
                IsPrimary       = ($row.Primary -eq "Yes")
                IsActive        = $isActive
                IsDisconnected  = $isDisconnected
                MonitorName     = $row.'Monitor Name'
                ShortId         = $row.'Short Monitor ID'
                LeftTop         = $row.'Left-Top'
            }
        }

        $monitors = @($monitors | Sort-Object { -not $_.IsActive }, Name)
    }
    finally {
        Remove-Item -LiteralPath $csvPath -Force -ErrorAction SilentlyContinue
    }

    $Script:MonitorListCache = $monitors
    return $Script:MonitorListCache
}

function Get-ConsoleAudioDevices {
    param([switch]$ForceRefresh)

    if (-not (Test-SoundVolumeViewAvailable)) {
        return @()
    }

    if (-not $ForceRefresh -and $Script:AudioDevicesCache) {
        return $Script:AudioDevicesCache
    }

    $csvPath = Join-Path $env:TEMP "consolemode_audio.csv"
    $devices = @()

    try {
        Invoke-Svv -Arguments @(
            "/ShowDisabledDevices", "1",
            "/ShowUnpluggedDevices", "1",
            "/scomma", "`"$csvPath`""
        ) | Out-Null
        if (-not (Test-Path -LiteralPath $csvPath)) {
            $Script:AudioDevicesCache = @()
            return @()
        }

        $rows = Import-Csv -LiteralPath $csvPath -Encoding UTF8
        $devices = foreach ($row in $rows) {
            $friendlyId = $row.'Command-Line Friendly ID'

            if ([string]::IsNullOrWhiteSpace($friendlyId)) { continue }
            if ($row.Type -ne 'Device') { continue }
            if ($row.Direction -ne 'Render') { continue }

            $deviceState = [string]$row.'Device State'
            $isActive = ($deviceState -match 'Active') -or [string]::IsNullOrWhiteSpace($deviceState)

            $friendlyName = $row.Name
            $driverName = $row.'Device Name'

            if ([string]::IsNullOrWhiteSpace($friendlyName)) {
                $displayName = $driverName
            }
            elseif (-not [string]::IsNullOrWhiteSpace($driverName) -and $driverName -ne $friendlyName) {
                $displayName = "$friendlyName ($driverName)"
            }
            else {
                $displayName = $friendlyName
            }

            if (-not $isActive) {
                $displayName += " [Desabilitado]"
            }

            [PSCustomObject]@{
                Name       = $displayName
                FriendlyId = $friendlyId
                IsDefault  = ($row.Default -match 'Render')
                IsActive   = $isActive
            }
        }

        $devices = @($devices | Sort-Object { -not $_.IsActive }, Name -Unique)
    }
    catch {
        $devices = @()
    }
    finally {
        Remove-Item -LiteralPath $csvPath -Force -ErrorAction SilentlyContinue
    }

    $Script:AudioDevicesCache = $devices
    foreach ($d in $devices) {
        if ($d.IsDefault) {
            $Script:CachedDefaultAudioId = $d.FriendlyId
            break
        }
    }
    return $Script:AudioDevicesCache
}

function Get-DefaultAudioDeviceId {
    if ($Script:CachedDefaultAudioId) { return $Script:CachedDefaultAudioId }
    if (-not (Test-SoundVolumeViewAvailable)) { return $null }

    try {
        $devices = Get-ConsoleAudioDevices
        $default = $devices | Where-Object { $_.IsDefault } | Select-Object -First 1
        if ($default) {
            $Script:CachedDefaultAudioId = $default.FriendlyId
            return $default.FriendlyId
        }
        return $null
    }
    catch {
        return $null
    }
}

function ConvertFrom-ConfigMonitorModes {
    param($Raw)

    $result = @{}
    if (-not $Raw) { return $result }

    foreach ($prop in $Raw.PSObject.Properties) {
        $entry = $prop.Value
        if (-not $entry) { continue }
        if ($entry.useCurrent -eq $true) { continue }

        $width = 0
        $height = 0
        $frequency = 0
        [void][int]::TryParse([string]$entry.width, [ref]$width)
        [void][int]::TryParse([string]$entry.height, [ref]$height)
        [void][int]::TryParse([string]$entry.frequency, [ref]$frequency)

        if ($width -gt 0 -and $height -gt 0) {
            $result[[string]$prop.Name] = @{
                Width     = $width
                Height    = $height
                Frequency = $frequency
            }
        }
    }

    return $result
}

function ConvertTo-ConfigMonitorModes {
    param([hashtable]$MonitorModes)

    $result = [ordered]@{}
    if (-not $MonitorModes) { return $result }

    foreach ($name in ($MonitorModes.Keys | Sort-Object)) {
        $mode = $MonitorModes[$name]
        if (-not $mode) { continue }

        $width = [int]$mode.Width
        $height = [int]$mode.Height
        $frequency = [int]$mode.Frequency
        if ($width -le 0 -or $height -le 0) { continue }

        $result[$name] = [ordered]@{
            width     = $width
            height    = $height
            frequency = $frequency
        }
    }

    return $result
}

function Get-ConsoleConfig {
    if (-not (Test-Path -LiteralPath $Script:ConfigPath)) {
        return [PSCustomObject]@{
            focusMonitor   = ""
            hideMonitors   = @()
            hideStrategy   = "disconnect"
            fullscreenMode = "bigPicture"
            audioDeviceId   = ""
            audioDeviceName = ""
            audioAutoSwitch = $false
            fpsLimit        = 0
            monitorModes    = @{}
        }
    }

    try {
        $raw = Get-Content -LiteralPath $Script:ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
        return [PSCustomObject]@{
            focusMonitor    = [string]$raw.focusMonitor
            hideMonitors    = @($raw.hideMonitors)
            hideStrategy    = if ($raw.hideStrategy) { [string]$raw.hideStrategy } else { "disconnect" }
            fullscreenMode  = if ($raw.fullscreenMode) { [string]$raw.fullscreenMode } else { "bigPicture" }
            audioDeviceId   = [string]$raw.audioDeviceId
            audioDeviceName = [string]$raw.audioDeviceName
            audioAutoSwitch = [bool]($raw.audioAutoSwitch -eq $true)
            fpsLimit        = if ($null -ne $raw.fpsLimit) { [int]$raw.fpsLimit } else { 0 }
            monitorModes    = ConvertFrom-ConfigMonitorModes -Raw $raw.monitorModes
        }
    }
    catch {
        return [PSCustomObject]@{
            focusMonitor    = ""
            hideMonitors    = @()
            hideStrategy    = "disconnect"
            fullscreenMode  = "bigPicture"
            audioDeviceId   = ""
            audioDeviceName = ""
            audioAutoSwitch = $false
            fpsLimit        = 0
            monitorModes    = @{}
        }
    }
}

function Set-ConsoleConfig {
    param(
        [string]$FocusMonitor,
        [string[]]$HideMonitors,
        [string]$HideStrategy,
        [string]$FullscreenMode,
        [string]$AudioDeviceId,
        [string]$AudioDeviceName,
        [bool]$AudioAutoSwitch = $false,
        [int]$FpsLimit = 0,
        [hashtable]$MonitorModes = @{}
    )

    $config = [ordered]@{
        focusMonitor    = $FocusMonitor
        hideMonitors    = @($HideMonitors)
        hideStrategy    = $HideStrategy
        fullscreenMode  = $FullscreenMode
        audioDeviceId   = $AudioDeviceId
        audioDeviceName = $AudioDeviceName
        audioAutoSwitch = $AudioAutoSwitch
        fpsLimit        = $FpsLimit
        monitorModes    = ConvertTo-ConfigMonitorModes -MonitorModes $MonitorModes
    }

    $config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $Script:ConfigPath -Encoding UTF8
}

function Save-MonitorBackup {
    Invoke-Mmt -Arguments @("/SaveConfig", "`"$Script:BackupMonitorConfig`"") | Out-Null
    return Get-ConsolePrimaryMonitorName
}

function Get-ConsolePrimaryMonitorName {
    $monitors = Get-ConsoleMonitors -ForceRefresh
    $primary = $monitors | Where-Object { $_.IsPrimary -and $_.IsActive } | Select-Object -First 1
    if (-not $primary) {
        $primary = $monitors | Where-Object { $_.IsPrimary } | Select-Object -First 1
    }
    if ($primary) { return $primary.Name }
    return $null
}

function Save-MonitorBackupMeta {
    param(
        [string]$OriginalPrimary,
        [bool]$FocusWasInactive,
        [string]$FocusMonitor,
        [string[]]$HideMonitors,
        [string]$HideStrategy
    )

    $meta = [ordered]@{
        originalPrimary  = $OriginalPrimary
        focusWasInactive = $FocusWasInactive
        focusMonitor     = $FocusMonitor
        hideMonitors     = @($HideMonitors)
        hideStrategy     = $HideStrategy
    }
    $meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $Script:BackupMonitorMeta -Encoding UTF8
}

function Get-BackupMonitorSpecs {
    param([Parameter(Mandatory)][string]$Path)

    $specs = @{}
    if (-not (Test-Path -LiteralPath $Path)) { return $specs }

    $current = $null
    $currentName = $null

    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        if ($line -match '^\[Monitor\d+\]') {
            if ($currentName -and $current) {
                $specs[$currentName] = $current
            }
            $current = @{}
            $currentName = $null
            continue
        }

        if ($line -match '^Name=(.+)$') {
            $currentName = $Matches[1].Trim()
            $current['Name'] = $currentName
            continue
        }

        if ($line -match '^(\w+)=(.+)$' -and $current) {
            $current[$Matches[1]] = $Matches[2].Trim()
        }
    }

    if ($currentName -and $current) {
        $specs[$currentName] = $current
    }

    return $specs
}

function Get-MonitorRestoreContext {
    $ctx = @{
        OriginalPrimary  = $Script:ConsoleState.OriginalPrimary
        FocusWasInactive = $Script:ConsoleState.FocusWasInactive
        FocusMonitor     = $Script:ConsoleState.FocusMonitor
        HideMonitors     = @($Script:ConsoleState.HideMonitors)
        HideStrategy     = $Script:ConsoleState.HideStrategy
    }

    if (Test-Path -LiteralPath $Script:BackupMonitorMeta) {
        try {
            $meta = Get-Content -LiteralPath $Script:BackupMonitorMeta -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($meta.originalPrimary) { $ctx.OriginalPrimary = [string]$meta.originalPrimary }
            if ($null -ne $meta.focusWasInactive) { $ctx.FocusWasInactive = [bool]$meta.focusWasInactive }
            if ($meta.focusMonitor) { $ctx.FocusMonitor = [string]$meta.focusMonitor }
            if ($meta.hideMonitors) { $ctx.HideMonitors = @($meta.hideMonitors) }
            if ($meta.hideStrategy) { $ctx.HideStrategy = [string]$meta.hideStrategy }
        }
        catch { }
    }

    return $ctx
}

function Set-PrimaryMonitorFromBackupSpec {
    param(
        [Parameter(Mandatory)][string]$MonitorName,
        [hashtable]$Spec
    )

    if ($Spec -and $Spec.Width -and $Spec.Height) {
        $width = 0
        $height = 0
        [void][int]::TryParse($Spec.Width, [ref]$width)
        [void][int]::TryParse($Spec.Height, [ref]$height)
        if ($width -gt 0 -and $height -gt 0) {
            $freq = $Spec.DisplayFrequency
            if (-not $freq) { $freq = $Spec.Frequency }
            Set-PrimaryAndMonitorMode -MonitorName $MonitorName `
                -Width $width -Height $height `
                -Frequency $freq -Colors $Spec.BitsPerPixel
            return
        }
    }

    Invoke-Mmt -Arguments @("/SetPrimary", "`"$MonitorName`"") | Out-Null
}

function Disable-FocusMonitorAfterRestore {
    param([Parameter(Mandatory)][string]$MonitorName)

    Invoke-Mmt -Arguments @("/disable", "`"$MonitorName`"") | Out-Null
    Start-Sleep -Milliseconds 120
    Invoke-Mmt -Arguments @("/disable", "`"$MonitorName`"") | Out-Null
}

function Restore-MonitorBackup {
    if (-not (Test-Path -LiteralPath $Script:BackupMonitorConfig)) { return }

    $ctx = Get-MonitorRestoreContext
    $backupSpecs = Get-BackupMonitorSpecs -Path $Script:BackupMonitorConfig
    $monitorsToEnable = @()

    if ($ctx.HideStrategy -eq "disconnect" -and $ctx.HideMonitors.Count -gt 0) {
        $monitorsToEnable = @($ctx.HideMonitors | Select-Object -Unique)
    }
    elseif ($ctx.HideStrategy -eq "turnOff" -and $ctx.HideMonitors.Count -gt 0) {
        foreach ($name in $ctx.HideMonitors) {
            Invoke-Mmt -Arguments @("/TurnOn", "`"$name`"") | Out-Null
        }
        $monitorsToEnable = @($ctx.HideMonitors | Select-Object -Unique)
    }

    if ($monitorsToEnable.Count -gt 0 -and $ctx.HideStrategy -eq "disconnect") {
        $enableArgs = @("/enable")
        foreach ($name in $monitorsToEnable) {
            $enableArgs += "`"$name`""
        }
        Invoke-Mmt -Arguments $enableArgs | Out-Null
    }

    Invoke-Mmt -Arguments @("/LoadConfig", "`"$Script:BackupMonitorConfig`"") | Out-Null

    if ($ctx.OriginalPrimary) {
        $primarySpec = $backupSpecs[$ctx.OriginalPrimary]
        Set-PrimaryMonitorFromBackupSpec -MonitorName $ctx.OriginalPrimary -Spec $primarySpec
    }

    if ($ctx.FocusWasInactive -and $ctx.FocusMonitor) {
        Invoke-Mmt -Arguments @("/disable", "`"$($ctx.FocusMonitor)`"") | Out-Null
        if ($ctx.OriginalPrimary) {
            Invoke-Mmt -Arguments @("/SetPrimary", "`"$($ctx.OriginalPrimary)`"") | Out-Null
        }
    }
}

function Set-MonitorCacheActive {
    param([Parameter(Mandatory)][string[]]$Names)

    if (-not $Script:MonitorListCache) { return }

    $Script:MonitorListCache = $Script:MonitorListCache | ForEach-Object {
        if ($Names -contains $_.Name) {
            $copy = $_ | Select-Object *
            $copy.IsActive = $true
            $copy.IsDisconnected = $false
            $copy
        }
        else { $_ }
    }
}

function Set-PrimaryMonitor {
    param([Parameter(Mandatory)][string]$MonitorName)
    Invoke-Mmt -Arguments @("/SetPrimary", "`"$MonitorName`"") | Out-Null
}

function Test-ValidMonitorModeInfo {
    param($Monitor)

    if (-not $Monitor) { return $false }
    $width = 0
    $height = 0
    if ($Monitor.Width) { [void][int]::TryParse([string]$Monitor.Width, [ref]$width) }
    if ($Monitor.Height) { [void][int]::TryParse([string]$Monitor.Height, [ref]$height) }
    return ($width -gt 0 -and $height -gt 0)
}

function Get-ParsedDisplayFrequency {
    param([string]$Frequency)

    if ([string]::IsNullOrWhiteSpace($Frequency)) { return 0 }
    $value = 0
    [void][int]::TryParse($Frequency, [ref]$value)
    return $value
}

function Get-MonitorDisplayModes {
    param(
        [Parameter(Mandatory)][string]$MonitorName,
        [switch]$ForceRefresh
    )

    if (-not $ForceRefresh -and $Script:DisplayModesCache.ContainsKey($MonitorName)) {
        return $Script:DisplayModesCache[$MonitorName]
    }

    $modes = @()
    try {
        $nativeModes = [NativeHelpers]::EnumerateDisplayModes($MonitorName)
        foreach ($mode in $nativeModes) {
            $modes += [PSCustomObject]@{
                Width     = [int]$mode.Width
                Height    = [int]$mode.Height
                Frequency = [int]$mode.Frequency
                BitsPerPel = [int]$mode.BitsPerPel
                Key       = [string]$mode.Key
                Text      = [string]$mode.Text
            }
        }
    }
    catch {
        $modes = @()
    }

    $Script:DisplayModesCache[$MonitorName] = $modes
    return $modes
}

function Get-MonitorModeLabel {
    param($Mode)

    if (-not $Mode) { return "(manter atual)" }

    $width = [int]$Mode.Width
    $height = [int]$Mode.Height
    $frequency = [int]$Mode.Frequency
    if ($width -le 0 -or $height -le 0) { return "(manter atual)" }

    if ($frequency -gt 0) {
        return "$width x $height @ $frequency Hz"
    }
    return "$width x $height"
}

function Apply-ConfiguredMonitorModes {
    param(
        [hashtable]$MonitorModes,
        [Parameter(Mandatory)][string]$FocusMonitor
    )

    if (-not $MonitorModes -or $MonitorModes.Count -eq 0) { return }

    $monitors = Get-ConsoleMonitors -ForceRefresh
    $otherNames = @($MonitorModes.Keys | Where-Object { $_ -ne $FocusMonitor })

    foreach ($name in $otherNames) {
        $mode = $MonitorModes[$name]
        if (-not $mode) { continue }

        $monitor = $monitors | Where-Object { $_.Name -eq $name -and $_.IsActive } | Select-Object -First 1
        if (-not $monitor) { continue }

        Set-MonitorMode `
            -MonitorName $name `
            -Width ([int]$mode.Width) `
            -Height ([int]$mode.Height) `
            -Frequency ([string]$mode.Frequency) `
            -Colors $monitor.Colors
        Start-Sleep -Milliseconds 350
    }

    if ($MonitorModes.ContainsKey($FocusMonitor)) {
        $focusMode = $MonitorModes[$FocusMonitor]
        $focusInfo = $monitors | Where-Object { $_.Name -eq $FocusMonitor } | Select-Object -First 1
        $colors = if ($focusInfo) { $focusInfo.Colors } else { $null }

        Set-PrimaryAndMonitorMode `
            -MonitorName $FocusMonitor `
            -Width ([int]$focusMode.Width) `
            -Height ([int]$focusMode.Height) `
            -Frequency ([string]$focusMode.Frequency) `
            -Colors $colors
        Start-Sleep -Milliseconds 400
        return
    }

    Set-PrimaryMonitor -MonitorName $FocusMonitor
    Start-Sleep -Milliseconds 400
}

function Update-FocusMonitorRect {
    param(
        [string]$MonitorName = $Script:ConsoleState.FocusMonitor,
        [switch]$AllowMmtFallback
    )

    if ([string]::IsNullOrWhiteSpace($MonitorName)) { return $false }

    $screen = Get-ScreenByDeviceName -DeviceName $MonitorName
    if ($screen) {
        $Script:ConsoleState.FocusMonitorRect = $screen.Bounds
        return $true
    }

    if (-not $AllowMmtFallback) { return $false }

    $monitorInfo = Get-ConsoleMonitors | Where-Object { $_.Name -eq $MonitorName } | Select-Object -First 1
    $rect = Get-RectFromMonitorInfo -Monitor $monitorInfo
    if ($rect) {
        $Script:ConsoleState.FocusMonitorRect = $rect
        return $true
    }

    return $false
}

function Set-PrimaryAndMonitorMode {
    param(
        [Parameter(Mandatory)][string]$MonitorName,
        [int]$Width,
        [int]$Height,
        [string]$Frequency,
        [string]$Colors
    )

    $spec = "Name=$MonitorName Primary=Yes"
    if ($Width -gt 0) { $spec += " Width=$Width" }
    if ($Height -gt 0) { $spec += " Height=$Height" }
    $freqValue = Get-ParsedDisplayFrequency -Frequency $Frequency
    if ($freqValue -gt 0) { $spec += " DisplayFrequency=$freqValue" }
    if ($Colors) { $spec += " BitsPerPixel=$Colors" }

    Invoke-Mmt -Arguments @("/SetMonitors", "`"$spec`"") | Out-Null
}

function Set-MonitorMode {
    param(
        [Parameter(Mandatory)][string]$MonitorName,
        [int]$Width,
        [int]$Height,
        [string]$Frequency,
        [string]$Colors
    )

    $spec = "Name=$MonitorName"
    if ($Width -gt 0) { $spec += " Width=$Width" }
    if ($Height -gt 0) { $spec += " Height=$Height" }
    $freqValue = Get-ParsedDisplayFrequency -Frequency $Frequency
    if ($freqValue -gt 0) { $spec += " DisplayFrequency=$freqValue" }
    if ($Colors) { $spec += " BitsPerPixel=$Colors" }

    Invoke-Mmt -Arguments @("/SetMonitors", "`"$spec`"") | Out-Null
}

function Enable-Monitors {
    param(
        [Parameter(Mandatory)][string[]]$MonitorNames,
        [switch]$WindowsEnable
    )

    if ($MonitorNames.Count -eq 0) { return }

    if ($WindowsEnable) {
        $args = @("/enable")
        foreach ($name in $MonitorNames) {
            $args += "`"$name`""
        }
        Invoke-Mmt -Arguments $args | Out-Null
        Set-MonitorCacheActive -Names $MonitorNames
        return
    }

    foreach ($name in $MonitorNames) {
        Invoke-Mmt -Arguments @("/TurnOn", "`"$name`"") | Out-Null
        Invoke-Mmt -Arguments @("/enable", "`"$name`"") | Out-Null
    }
    Set-MonitorCacheActive -Names $MonitorNames
}

function Disable-MonitorsWindows {
    param([Parameter(Mandatory)][string[]]$MonitorNames)

    if ($MonitorNames.Count -eq 0) { return }

    $args = @("/disable")
    foreach ($name in $MonitorNames) {
        $args += "`"$name`""
    }
    Invoke-Mmt -Arguments $args | Out-Null
}

function Disable-MonitorsDdc {
    param([Parameter(Mandatory)][string[]]$MonitorNames)

    foreach ($name in $MonitorNames) {
        Invoke-Mmt -Arguments @("/TurnOff", "`"$name`"") | Out-Null
    }
}

function Get-AudioDeviceMatchScore {
    param(
        $Device,
        [string]$Hint,
        $FocusMonitorInfo
    )

    $text = ($Device.Name -replace '\s*\[Desabilitado\]\s*$', '').Trim()
    $score = 0

    if ($Hint) {
        foreach ($part in ($Hint -split '\s+|/|\\' | Where-Object { $_.Length -ge 3 })) {
            if ($text -match [regex]::Escape($part)) { $score += 8 }
        }
    }
    if ($FocusMonitorInfo -and $FocusMonitorInfo.MonitorName) {
        foreach ($part in ($FocusMonitorInfo.MonitorName -split '\s+' | Where-Object { $_.Length -ge 3 })) {
            if ($text -match [regex]::Escape($part)) { $score += 5 }
        }
    }
    if ($text -match 'HDMI|Display|High Definition Audio|SSCR|TV') { $score += 1 }

    return $score
}

function Select-NewConsoleAudioDevice {
    param(
        [array]$Candidates,
        [string]$Hint,
        $FocusMonitorInfo
    )

    if ($Candidates.Count -eq 0) { return $null }
    if ($Candidates.Count -eq 1) { return $Candidates[0] }

    $best = $null
    $bestScore = -1
    foreach ($device in $Candidates) {
        $score = Get-AudioDeviceMatchScore -Device $device -Hint $Hint -FocusMonitorInfo $FocusMonitorInfo
        if ($score -gt $bestScore) {
            $bestScore = $score
            $best = $device
        }
    }

    if ($bestScore -le 0) { return $Candidates[0] }
    return $best
}

function Initialize-ConsoleAudioWatch {
    $devices = Get-ConsoleAudioDevices -ForceRefresh
    $Script:ConsoleState.AudioBaselineActiveIds = @(
        $devices | Where-Object { $_.IsActive } | ForEach-Object { $_.FriendlyId }
    )
    $Script:ConsoleState.LastAudioPoll = Get-Date
}

function Get-ConsoleMonitorPollDelayMs {
    if ($Script:ConsoleState.FullscreenMode -eq "xboxMode") {
        return 5000
    }
    if (-not $Script:ConsoleState.HasAppeared) { return 1500 }
    if (Test-ConsoleAudioWatchNeeded) { return 4000 }
    return 2000
}

function Test-ConsoleBigPictureWatchActive {
    return [bool]$Script:ConsoleState.BigPictureWatchActive
}

function Register-BigPictureExitWatch {
    param([IntPtr]$Handle)

    if ($Handle -eq [IntPtr]::Zero) { return $false }
    $started = [NativeHelpers]::StartBigPictureExitWatch($Handle)
    $Script:ConsoleState.BigPictureWatchActive = $started
    return $started
}

function Stop-BigPictureExitWatch {
    [NativeHelpers]::StopBigPictureExitWatch()
    $Script:ConsoleState.BigPictureWatchActive = $false
}

function Test-BigPictureExitSignaled {
    if ([NativeHelpers]::ConsumeBigPictureExitRequest()) { return $true }

    $handle = $Script:ConsoleState.CachedBigPictureHandle
    if ($handle -ne [IntPtr]::Zero -and -not [NativeHelpers]::IsWindowStillVisible($handle)) {
        return $true
    }

    if ($Script:ConsoleState.HasAppeared) {
        if (Test-BigPictureActive) {
            $Script:ConsoleState.AbsenceCount = 0
        }
        else {
            $Script:ConsoleState.AbsenceCount++
            if ($Script:ConsoleState.AbsenceCount -ge 2) {
                return $true
            }
        }
    }

    return $false
}

function Test-ConsoleAudioWatchNeeded {
    if (-not (Test-SoundVolumeViewAvailable)) { return $false }
    if ($Script:ConsoleState.AudioWatchComplete) { return $false }
    if ($Script:ConsoleState.AudioAutoSwitch) { return $true }
    if ($Script:ConsoleState.AudioPendingTarget) { return $true }
    return $false
}

function Complete-ConsoleAudioWatch {
    $Script:ConsoleState.AudioWatchComplete = $true
    $Script:ConsoleState.AudioPendingTarget = $false
}

function Update-ConsoleAudioAutoSwitch {
    # Aguarda nova saida de audio ativa (ex.: HDMI da TV ao ligar) e troca para ela.
    if (-not $Script:ConsoleState.AudioAutoSwitch) { return $null }
    if (-not (Test-SoundVolumeViewAvailable)) { return $null }

    $now = Get-Date
    if ($Script:ConsoleState.LastAudioPoll) {
        $elapsed = ($now - $Script:ConsoleState.LastAudioPoll).TotalSeconds
        $minInterval = if ($Script:ConsoleState.HasAppeared) { 8 } else { 3 }
        if ($elapsed -lt $minInterval) { return $null }
    }
    $Script:ConsoleState.LastAudioPoll = $now

    $devices = Get-ConsoleAudioDevices -ForceRefresh
    $newActive = @($devices | Where-Object {
        $_.IsActive -and ($_.FriendlyId -notin $Script:ConsoleState.AudioBaselineActiveIds)
    })

    if ($newActive.Count -eq 0) { return $null }

    $focusInfo = $null
    if ($Script:ConsoleState.FocusMonitor) {
        $focusInfo = $Script:MonitorListCache | Where-Object { $_.Name -eq $Script:ConsoleState.FocusMonitor } | Select-Object -First 1
        if (-not $focusInfo) {
            $focusInfo = Get-ConsoleMonitors | Where-Object { $_.Name -eq $Script:ConsoleState.FocusMonitor } | Select-Object -First 1
        }
    }

    $pick = Select-NewConsoleAudioDevice -Candidates $newActive `
        -Hint $Script:ConsoleState.AudioDeviceHint `
        -FocusMonitorInfo $focusInfo

    if (-not $pick) { return $null }

    Set-ConsoleAudioOutput -FriendlyId $pick.FriendlyId
    $Script:ConsoleState.AudioDeviceId = $pick.FriendlyId
    if ($pick.FriendlyId -notin $Script:ConsoleState.AudioBaselineActiveIds) {
        $Script:ConsoleState.AudioBaselineActiveIds += $pick.FriendlyId
    }

    $displayName = ($pick.Name -replace '\s*\[Desabilitado\]\s*$', '').Trim()
    $Script:ConsoleState.LastAudioSwitchName = $displayName
    Complete-ConsoleAudioWatch
    return $displayName
}

function Update-ConsolePendingAudioDevice {
    if ($Script:ConsoleState.AudioAutoSwitch) { return $null }
    if ([string]::IsNullOrWhiteSpace($Script:ConsoleState.AudioDeviceId)) { return $null }
    if (-not $Script:ConsoleState.AudioPendingTarget) { return $null }
    if (-not (Test-SoundVolumeViewAvailable)) { return $null }

    $now = Get-Date
    if ($Script:ConsoleState.LastAudioPoll) {
        $elapsed = ($now - $Script:ConsoleState.LastAudioPoll).TotalSeconds
        $minInterval = if ($Script:ConsoleState.HasAppeared) { 8 } else { 3 }
        if ($elapsed -lt $minInterval) { return $null }
    }
    $Script:ConsoleState.LastAudioPoll = $now

    $devices = Get-ConsoleAudioDevices -ForceRefresh
    $target = $devices | Where-Object {
        $_.FriendlyId -eq $Script:ConsoleState.AudioDeviceId -and $_.IsActive
    } | Select-Object -First 1

    if (-not $target) { return $null }

    Set-ConsoleAudioOutput -FriendlyId $target.FriendlyId
    $Script:ConsoleState.AudioPendingTarget = $false
    if ($target.FriendlyId -notin $Script:ConsoleState.AudioBaselineActiveIds) {
        $Script:ConsoleState.AudioBaselineActiveIds += $target.FriendlyId
    }

    $displayName = ($target.Name -replace '\s*\[Desabilitado\]\s*$', '').Trim()
    $Script:ConsoleState.LastAudioSwitchName = $displayName
    Complete-ConsoleAudioWatch
    return $displayName
}

function Set-ConsoleAudioOutput {
    param([Parameter(Mandatory)][string]$FriendlyId)

    Invoke-Svv -Arguments @("/Enable", "`"$FriendlyId`"") | Out-Null
    Invoke-Svv -Arguments @("/SetDefault", "`"$FriendlyId`"", "all") | Out-Null
    $Script:CachedDefaultAudioId = $FriendlyId
    $Script:AudioDevicesCache = $null
}

function Restore-ConsoleAudioOutput {
    if (-not $Script:ConsoleState.BackupAudioId) { return }
    if (-not (Test-SoundVolumeViewAvailable)) { return }

    try {
        Invoke-Svv -Arguments @("/SetDefault", "`"$($Script:ConsoleState.BackupAudioId)`"", "all") | Out-Null
    }
    catch {
        Write-Warning "Não foi possível restaurar o áudio padrão: $_"
    }
}

function Get-ScreenByDeviceName {
    param([Parameter(Mandatory)][string]$DeviceName)

    foreach ($screen in [System.Windows.Forms.Screen]::AllScreens) {
        if ($screen.DeviceName -eq $DeviceName) {
            return $screen
        }
    }

    $normalized = ($DeviceName -replace '\\\\\.\\', '').Trim()
    foreach ($screen in [System.Windows.Forms.Screen]::AllScreens) {
        $screenNorm = ($screen.DeviceName -replace '\\\\\.\\', '').Trim()
        if ($screenNorm -eq $normalized) {
            return $screen
        }
    }
    return $null
}

function Get-RectFromMonitorInfo {
    param($Monitor)

    if (-not $Monitor -or -not $Monitor.LeftTop) { return $null }

    if ($Monitor.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') {
        $x = [int]$Matches[1]
        $y = [int]$Matches[2]
        if ($Monitor.Width -and $Monitor.Height) {
            return New-Object System.Drawing.Rectangle($x, $y, [int]$Monitor.Width, [int]$Monitor.Height)
        }
    }
    return $null
}

function Show-BlackCurtains {
    param(
        [Parameter(Mandatory)][string[]]$MonitorNames
    )

    Close-BlackCurtains

    foreach ($monitorName in $MonitorNames) {
        $screen = Get-ScreenByDeviceName -DeviceName $monitorName
        if (-not $screen) { continue }
        $rect = $screen.Bounds

        $form = New-Object System.Windows.Forms.Form
        $form.BackColor = [System.Drawing.Color]::Black
        $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
        $form.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
        $form.TopMost = $true
        $form.ShowInTaskbar = $false
        $form.KeyPreview = $true

        $rectObj = $rect
        $form.Add_Shown({
            $this.SetBounds($rectObj.X, $rectObj.Y, $rectObj.Width, $rectObj.Height)
            $this.TopMost = $true
        }.GetNewClosure())

        $form.Add_KeyDown({
            param($sender, $e)
            if ($e.KeyCode -eq [System.Windows.Forms.Keys]::Escape) {
                $Script:ConsoleState.ShouldExit = $true
            }
        })

        $form.SetBounds($rect.X, $rect.Y, $rect.Width, $rect.Height)
        $form.Show()
        $form.SetBounds($rect.X, $rect.Y, $rect.Width, $rect.Height)
        [void]$Script:ConsoleState.CurtainForms.Add($form)
    }
}

function Close-BlackCurtains {
    foreach ($form in @($Script:ConsoleState.CurtainForms)) {
        try {
            if ($form -and -not $form.IsDisposed) {
                $form.Close()
                $form.Dispose()
            }
        }
        catch { }
    }
    $Script:ConsoleState.CurtainForms = [System.Collections.ArrayList]@()
}

function Get-BigPictureWindowHandles {
    $all = [NativeHelpers]::GetAllVisibleWindows()
    if ($all.Count -eq 0) { return @() }

    $candidates = $all | Where-Object {
        $_.ClassName -eq 'SDL_app' -or
        $_.Title -match 'Big Picture|Steam Big Picture'
    }

    if ($candidates.Count -eq 0) {
        $steamProcs = Get-Process -Name "steamwebhelper" -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 }
        foreach ($p in $steamProcs) {
            $area = [NativeHelpers]::GetWindowArea($p.MainWindowHandle)
            if ($area -gt 0) {
                $candidates += [PSCustomObject]@{
                    Handle = $p.MainWindowHandle
                    Title = $p.MainWindowTitle
                    ClassName = ''
                    Area = $area
                }
            }
        }
    }

    if ($candidates.Count -eq 0) { return @() }

    $best = $candidates | Sort-Object {
        $score = $_.Area
        if ($_.Title -match 'Big Picture') { $score += 1000000000 }
        if ($_.Title -match 'Steam') { $score += 100000000 }
        $score
    } -Descending | Select-Object -First 1

    return @($best.Handle)
}

function Get-MonitorRect {
    param([Parameter(Mandatory)][string]$MonitorName)

    $screen = Get-ScreenByDeviceName -DeviceName $MonitorName
    if ($screen) { return $screen.Bounds }

    if ($Script:ConsoleState.FocusMonitorRect -and $Script:ConsoleState.FocusMonitor -eq $MonitorName) {
        return $Script:ConsoleState.FocusMonitorRect
    }

    $monitorInfo = $Script:MonitorListCache | Where-Object { $_.Name -eq $MonitorName } | Select-Object -First 1
    if (-not $monitorInfo) {
        $monitorInfo = Get-ConsoleMonitors | Where-Object { $_.Name -eq $MonitorName } | Select-Object -First 1
    }
    $rect = Get-RectFromMonitorInfo -Monitor $monitorInfo
    if ($rect) { return $rect }

    return $null
}

function Move-BigPictureViaMmt {
    param(
        [Parameter(Mandatory)][string]$MonitorName,
        [Parameter(Mandatory)][System.Drawing.Rectangle]$Rect
    )

    $args = @(
        "/MoveWindow", "`"$MonitorName`"",
        "Process", "`"steamwebhelper`"",
        "/WindowLeft", $Rect.X,
        "/WindowTop", $Rect.Y,
        "/WindowWidth", $Rect.Width,
        "/WindowHeight", $Rect.Height
    )
    Invoke-Mmt -Arguments $args | Out-Null
}

function Move-BigPictureToMonitor {
    param(
        [Parameter(Mandatory)][string]$MonitorName
    )

    $rect = Get-MonitorRect -MonitorName $MonitorName
    if (-not $rect) { return $false }

    Move-BigPictureViaMmt -MonitorName $MonitorName -Rect $rect
    return $true
}

function Start-BigPictureMode {
    Start-Process "steam://open/bigpicture"
}

function Start-XboxMode {
    [NativeHelpers]::SendWinF11()
}

function Test-XboxModeActive {
    if ($Script:ConsoleState.CachedXboxHandle -ne [IntPtr]::Zero) {
        if ([NativeHelpers]::IsWindowStillVisible($Script:ConsoleState.CachedXboxHandle)) {
            $area = [NativeHelpers]::GetWindowArea($Script:ConsoleState.CachedXboxHandle)
            if ($area -gt 250000) { return $true }
        }
        $Script:ConsoleState.CachedXboxHandle = [IntPtr]::Zero
    }

    foreach ($procName in @('XboxGameCallableUI', 'GamingApp', 'XboxPcApp')) {
        $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 }
        foreach ($p in $procs) {
            $area = [NativeHelpers]::GetWindowArea($p.MainWindowHandle)
            if ($area -gt 250000) {
                $Script:ConsoleState.CachedXboxHandle = $p.MainWindowHandle
                return $true
            }
        }
    }

    if (-not $Script:ConsoleState.HasAppeared) {
        $windows = [NativeHelpers]::GetAllVisibleWindows()
        foreach ($w in $windows) {
            if ($w.Area -lt 250000) { continue }
            if ($w.Title -match 'Xbox|Game Pass|Gaming|Modo Xbox') {
                $Script:ConsoleState.CachedXboxHandle = $w.Handle
                return $true
            }
            if ($w.ClassName -eq 'ApplicationFrameWindow' -and $w.Title -match 'Xbox') {
                $Script:ConsoleState.CachedXboxHandle = $w.Handle
                return $true
            }
        }
    }

    return $false
}

function Test-BigPictureActive {
    if ($Script:ConsoleState.CachedBigPictureHandle -ne [IntPtr]::Zero) {
        if ([NativeHelpers]::IsWindowStillVisible($Script:ConsoleState.CachedBigPictureHandle)) {
            $area = [NativeHelpers]::GetWindowArea($Script:ConsoleState.CachedBigPictureHandle)
            if ($area -gt 200000) { return $true }
        }
        $Script:ConsoleState.CachedBigPictureHandle = [IntPtr]::Zero
    }

    $handles = Get-BigPictureWindowHandles
    if ($handles.Count -eq 0) { return $false }

    foreach ($h in $handles) {
        $area = [NativeHelpers]::GetWindowArea($h)
        if ($area -gt 200000) {
            $Script:ConsoleState.CachedBigPictureHandle = $h
            return $true
        }
    }
    return $false
}

function Test-FullscreenModeActive {
    param([string]$Mode)

    switch ($Mode) {
        "bigPicture" { return Test-BigPictureActive }
        "xboxMode"   { return Test-XboxModeActive }
        default      { return $false }
    }
}

function Start-ConsoleMode {
    param(
        [Parameter(Mandatory)][string]$FocusMonitor,
        [string[]]$HideMonitors = @(),
        [ValidateSet("disconnect", "blackCurtain", "turnOff")]
        [string]$HideStrategy = "disconnect",
        [ValidateSet("bigPicture", "xboxMode")]
        [string]$FullscreenMode = "bigPicture",
        [string]$AudioDeviceId = "",
        [switch]$AudioAutoSwitch,
        [string]$AudioDeviceHint = "",
        $FocusMonitorInfo = $null,
        [int]$FpsLimit = 0,
        [hashtable]$MonitorModes = @{}
    )

    if ($Script:ConsoleState.IsActive) {
        throw "O modo console já está ativo."
    }

    $Script:ConsoleState.FocusMonitor = $FocusMonitor
    $Script:ConsoleState.FocusMonitorRect = $null
    $Script:ConsoleState.HideMonitors = @($HideMonitors)
    $Script:ConsoleState.HideStrategy = $HideStrategy
    $Script:ConsoleState.FullscreenMode = $FullscreenMode
    $Script:ConsoleState.AudioDeviceId = $AudioDeviceId
    $Script:ConsoleState.AudioAutoSwitch = [bool]$AudioAutoSwitch
    $Script:ConsoleState.AudioDeviceHint = $AudioDeviceHint
    $Script:ConsoleState.LastAudioSwitchName = $null
    $Script:ConsoleState.AudioPendingTarget = $false
    $Script:ConsoleState.ShouldExit = $false
    $Script:ConsoleState.SteamMoved = $false
    $Script:ConsoleState.MoveCount = 0
    $Script:ConsoleState.HasAppeared = $false
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.LaunchTime = $null
    $Script:ConsoleState.AbsenceCount = 0
    $Script:ConsoleState.CachedBigPictureHandle = [IntPtr]::Zero
    $Script:ConsoleState.CachedXboxHandle = [IntPtr]::Zero
    $Script:ConsoleState.AudioWatchComplete = $false
    $Script:ConsoleState.BigPictureWatchActive = $false
    $Script:ConsoleState.FpsLimit = $FpsLimit
    $Script:ConsoleState.RtssBackup = $null
    $Script:ConsoleState.RtssLimitApplied = $false
    Stop-BigPictureExitWatch

    $focusMode = $FocusMonitorInfo
    if (-not $focusMode) {
        $focusMode = $Script:MonitorListCache | Where-Object { $_.Name -eq $FocusMonitor } | Select-Object -First 1
    }
    if (-not $focusMode) {
        $focusMode = Get-ConsoleMonitors | Where-Object { $_.Name -eq $FocusMonitor } | Select-Object -First 1
    }

    $Script:ConsoleState.FocusWasInactive = (-not $focusMode -or -not $focusMode.IsActive)

    $savedPrimary = Save-MonitorBackup
    $Script:ConsoleState.OriginalPrimary = if ($savedPrimary) { $savedPrimary } else { Get-ConsolePrimaryMonitorName }

    Save-MonitorBackupMeta `
        -OriginalPrimary $Script:ConsoleState.OriginalPrimary `
        -FocusWasInactive $Script:ConsoleState.FocusWasInactive `
        -FocusMonitor $FocusMonitor `
        -HideMonitors $HideMonitors `
        -HideStrategy $HideStrategy

    $Script:ConsoleState.IsActive = $true

    if (Test-SoundVolumeViewAvailable) {
        $Script:ConsoleState.BackupAudioId = Get-DefaultAudioDeviceId
        if ($Script:ConsoleState.BackupAudioId) {
            Set-Content -LiteralPath $Script:BackupAudioFile -Value $Script:ConsoleState.BackupAudioId -Encoding UTF8
        }
    }

    if (-not $focusMode -or -not $focusMode.IsActive) {
        Enable-Monitors -MonitorNames @($FocusMonitor) -WindowsEnable
        Start-Sleep -Milliseconds 500
        $focusMode = Get-ConsoleMonitors -ForceRefresh | Where-Object { $_.Name -eq $FocusMonitor } | Select-Object -First 1
    }

    if ($MonitorModes -and $MonitorModes.Count -gt 0) {
        Apply-ConfiguredMonitorModes -MonitorModes $MonitorModes -FocusMonitor $FocusMonitor
    }
    else {
        Set-PrimaryMonitor -MonitorName $FocusMonitor
        Start-Sleep -Milliseconds 400
    }

    if ($HideStrategy -eq "disconnect" -and $HideMonitors.Count -gt 0) {
        Disable-MonitorsWindows -MonitorNames $HideMonitors
        Start-Sleep -Milliseconds 500
    }
    elseif ($HideStrategy -eq "blackCurtain" -and $HideMonitors.Count -gt 0) {
        Show-BlackCurtains -MonitorNames $HideMonitors
    }
    elseif ($HideStrategy -eq "turnOff" -and $HideMonitors.Count -gt 0) {
        Disable-MonitorsDdc -MonitorNames $HideMonitors
        Start-Sleep -Milliseconds 400
    }

    Set-PrimaryMonitor -MonitorName $FocusMonitor
    Start-Sleep -Milliseconds 500
    Update-FocusMonitorRect -MonitorName $FocusMonitor -AllowMmtFallback | Out-Null

    if ($AudioDeviceId -and (Test-SoundVolumeViewAvailable)) {
        $audioDevices = Get-ConsoleAudioDevices -ForceRefresh
        $targetAudio = $audioDevices | Where-Object { $_.FriendlyId -eq $AudioDeviceId } | Select-Object -First 1
        if ($targetAudio -and $targetAudio.IsActive) {
            Set-ConsoleAudioOutput -FriendlyId $AudioDeviceId
            Complete-ConsoleAudioWatch
        }
        else {
            $Script:ConsoleState.AudioPendingTarget = $true
        }
    }

    if (Test-SoundVolumeViewAvailable) {
        Initialize-ConsoleAudioWatch
        if (-not $AudioAutoSwitch -and -not $Script:ConsoleState.AudioPendingTarget -and [string]::IsNullOrWhiteSpace($AudioDeviceId)) {
            Complete-ConsoleAudioWatch
        }
    }

    if ($FpsLimit -gt 0) {
        $rtssResult = Enable-ConsoleFpsLimit -FpsLimit $FpsLimit
        if (-not $rtssResult.Success) {
            Write-Warning $rtssResult.Message
        }
    }

    switch ($FullscreenMode) {
        "bigPicture" {
            Start-Sleep -Milliseconds 400
            Update-FocusMonitorRect -MonitorName $FocusMonitor -AllowMmtFallback | Out-Null
            Start-BigPictureMode
        }
        "xboxMode"   { Start-XboxMode }
    }
    $Script:ConsoleState.ModeLaunched = $true
    $Script:ConsoleState.LaunchTime = Get-Date

    $Script:MonitorListCache = $null
}

function Update-ConsoleMonitorLoop {
    if (-not $Script:ConsoleState.IsActive) { return "inactive" }
    if ($Script:ConsoleState.ShouldExit) { return "exit" }

    $mode = $Script:ConsoleState.FullscreenMode

    if ($mode -eq "xboxMode") {
        if (Test-ConsoleAudioWatchNeeded) {
            if ($Script:ConsoleState.AudioAutoSwitch) {
                Update-ConsoleAudioAutoSwitch | Out-Null
            }
            else {
                Update-ConsolePendingAudioDevice | Out-Null
            }
        }
        return "running"
    }

    if (Test-BigPictureExitSignaled) {
        return "exit"
    }

    if (-not $Script:ConsoleState.HasAppeared) {
        $bpHandles = Get-BigPictureWindowHandles
        if ($bpHandles.Count -gt 0) {
            Update-FocusMonitorRect -MonitorName $Script:ConsoleState.FocusMonitor | Out-Null
            $bpArea = [NativeHelpers]::GetWindowArea($bpHandles[0])
            if ($bpArea -lt 250000 -and $Script:ConsoleState.MoveCount -lt 3) {
                $moved = Move-BigPictureToMonitor -MonitorName $Script:ConsoleState.FocusMonitor
                $Script:ConsoleState.MoveCount++
                if ($moved) { $Script:ConsoleState.SteamMoved = $true }
            }

            foreach ($h in $bpHandles) {
                if ([NativeHelpers]::GetWindowArea($h) -gt 200000) {
                    $Script:ConsoleState.CachedBigPictureHandle = $h
                    $Script:ConsoleState.HasAppeared = $true
                    Register-BigPictureExitWatch -Handle $h | Out-Null
                    break
                }
            }
        }
    }

    if (Test-ConsoleAudioWatchNeeded) {
        if ($Script:ConsoleState.AudioAutoSwitch) {
            Update-ConsoleAudioAutoSwitch | Out-Null
        }
        else {
            Update-ConsolePendingAudioDevice | Out-Null
        }
    }

    return "running"
}

function Stop-ConsoleMode {
    if ($Script:ConsoleState.RestoreInProgress) { return }

    if (-not $Script:ConsoleState.IsActive -and $Script:ConsoleState.CurtainForms.Count -eq 0) {
        Restore-RtssFpsSettings
        return
    }

    $Script:ConsoleState.RestoreInProgress = $true
    try {
        Close-BlackCurtains
        Restore-MonitorBackup
        Restore-ConsoleAudioOutput
        Restore-RtssFpsSettings
        Clear-ConsoleDeviceCache
    }
    finally {
        $Script:ConsoleState.RestoreInProgress = $false
    }

    $Script:ConsoleState.IsActive = $false
    $Script:ConsoleState.ShouldExit = $false
    $Script:ConsoleState.SteamMoved = $false
    $Script:ConsoleState.MoveCount = 0
    $Script:ConsoleState.HasAppeared = $false
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.LaunchTime = $null
    $Script:ConsoleState.AbsenceCount = 0
    $Script:ConsoleState.FocusMonitorRect = $null
    $Script:ConsoleState.OriginalPrimary = $null
    $Script:ConsoleState.FocusWasInactive = $false
    $Script:ConsoleState.AudioDeviceId = $null
    $Script:ConsoleState.AudioAutoSwitch = $false
    $Script:ConsoleState.AudioDeviceHint = $null
    $Script:ConsoleState.AudioBaselineActiveIds = @()
    $Script:ConsoleState.LastAudioPoll = $null
    $Script:ConsoleState.LastAudioSwitchName = $null
    $Script:ConsoleState.AudioPendingTarget = $false
    $Script:ConsoleState.CachedBigPictureHandle = [IntPtr]::Zero
    $Script:ConsoleState.CachedXboxHandle = [IntPtr]::Zero
    $Script:ConsoleState.AudioWatchComplete = $false
    $Script:ConsoleState.BigPictureWatchActive = $false
    $Script:ConsoleState.FpsLimit = 0
    $Script:ConsoleState.RtssBackup = $null
    $Script:ConsoleState.RtssLimitApplied = $false
    Stop-BigPictureExitWatch
    $Script:ConsoleState.CurtainForms = [System.Collections.ArrayList]@()
}

function Request-ConsoleModeExit {
    $Script:ConsoleState.ShouldExit = $true
}
