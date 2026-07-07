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
}
"@
if (-not ([System.Management.Automation.PSTypeName]'NativeHelpers').Type) {
    Add-Type -TypeDefinition $nativeSource
}

$Script:AppRoot = Split-Path -Parent $PSScriptRoot
$Script:MmtPath = Join-Path $Script:AppRoot "MultiMonitorTool.exe"
$Script:SvvPath = Join-Path $Script:AppRoot "SoundVolumeView.exe"
$Script:ConfigPath = Join-Path $Script:AppRoot "config.json"
$Script:BackupMonitorConfig = Join-Path $Script:AppRoot "backup_monitores.cfg"
$Script:BackupAudioFile = Join-Path $Script:AppRoot "backup_audio.txt"

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
    HideMonitors   = @()
    HideStrategy   = "disconnect"
    FullscreenMode = "bigPicture"
    AudioDeviceId  = $null
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
        throw "MultiMonitorTool.exe nao encontrado em $Script:MmtPath"
    }

    $process = Start-Process -FilePath $Script:MmtPath -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
    return $process.ExitCode
}

function Invoke-Svv {
    param(
        [Parameter(Mandatory)][string[]]$Arguments
    )

    if (-not (Test-SoundVolumeViewAvailable)) {
        throw "SoundVolumeView.exe nao encontrado em $Script:SvvPath"
    }

    $process = Start-Process -FilePath $Script:SvvPath -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
    return $process.ExitCode
}

function Get-ConsoleMonitors {
    $csvPath = Join-Path $env:TEMP "consolemode_monitors.csv"

    try {
        Invoke-Mmt -Arguments @("/scomma", "`"$csvPath`"") | Out-Null
        if (-not (Test-Path -LiteralPath $csvPath)) {
            return @()
        }

        $rows = Import-Csv -LiteralPath $csvPath -Encoding UTF8
        $monitors = foreach ($row in $rows) {
            if ($row.Active -ne "Yes" -or $row.Disconnected -eq "Yes") { continue }

            $width = $null
            $height = $null
            if ($row.Resolution -match '(\d+)\s*[Xx]\s*(\d+)') {
                $width = [int]$Matches[1]
                $height = [int]$Matches[2]
            }

            [PSCustomObject]@{
                Name       = $row.Name
                Resolution = $row.Resolution
                Width      = $width
                Height     = $height
                Frequency  = $row.Frequency
                Colors     = $row.Colors
                IsPrimary  = ($row.Primary -eq "Yes")
                MonitorName = $row.'Monitor Name'
                ShortId    = $row.'Short Monitor ID'
                LeftTop    = $row.'Left-Top'
            }
        }

        return @($monitors)
    }
    finally {
        Remove-Item -LiteralPath $csvPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-ConsoleAudioDevices {
    if (-not (Test-SoundVolumeViewAvailable)) {
        return @()
    }

    $csvPath = Join-Path $env:TEMP "consolemode_audio.csv"

    try {
        Invoke-Svv -Arguments @("/scomma", "`"$csvPath`"") | Out-Null
        if (-not (Test-Path -LiteralPath $csvPath)) {
            return @()
        }

        $rows = Import-Csv -LiteralPath $csvPath -Encoding UTF8
        $devices = foreach ($row in $rows) {
            $friendlyId = $row.'Command-Line Friendly ID'

            if ([string]::IsNullOrWhiteSpace($friendlyId)) { continue }
            if ($row.Type -ne 'Device') { continue }
            if ($row.Direction -ne 'Render') { continue }
            if ($row.'Device State' -and $row.'Device State' -notmatch 'Active') { continue }

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

            [PSCustomObject]@{
                Name       = $displayName
                FriendlyId = $friendlyId
                IsDefault  = ($row.Default -match 'Render')
            }
        }

        return @($devices | Sort-Object Name -Unique)
    }
    catch {
        return @()
    }
    finally {
        Remove-Item -LiteralPath $csvPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-DefaultAudioDeviceId {
    if (-not (Test-SoundVolumeViewAvailable)) { return $null }

    try {
        $devices = Get-ConsoleAudioDevices
        $default = $devices | Where-Object { $_.IsDefault } | Select-Object -First 1
        if ($default) { return $default.FriendlyId }
        return $null
    }
    catch {
        return $null
    }
}

function Get-ConsoleConfig {
    if (-not (Test-Path -LiteralPath $Script:ConfigPath)) {
        return [PSCustomObject]@{
            focusMonitor   = ""
            hideMonitors   = @()
            hideStrategy   = "disconnect"
            fullscreenMode = "bigPicture"
            audioDeviceId  = ""
            audioDeviceName = ""
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
        [string]$AudioDeviceName
    )

    $config = [ordered]@{
        focusMonitor    = $FocusMonitor
        hideMonitors    = @($HideMonitors)
        hideStrategy    = $HideStrategy
        fullscreenMode  = $FullscreenMode
        audioDeviceId   = $AudioDeviceId
        audioDeviceName = $AudioDeviceName
    }

    $config | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $Script:ConfigPath -Encoding UTF8
}

function Save-MonitorBackup {
    Invoke-Mmt -Arguments @("/SaveConfig", "`"$Script:BackupMonitorConfig`"") | Out-Null
}

function Restore-MonitorBackup {
    if (Test-Path -LiteralPath $Script:BackupMonitorConfig) {
        Invoke-Mmt -Arguments @("/LoadConfig", "`"$Script:BackupMonitorConfig`"") | Out-Null
    }
}

function Set-PrimaryMonitor {
    param([Parameter(Mandatory)][string]$MonitorName)
    Invoke-Mmt -Arguments @("/SetPrimary", "`"$MonitorName`"") | Out-Null
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
    if ($Frequency) { $spec += " DisplayFrequency=$Frequency" }
    if ($Colors) { $spec += " BitsPerPixel=$Colors" }

    Invoke-Mmt -Arguments @("/SetMonitors", "`"$spec`"") | Out-Null
}

function Enable-Monitors {
    param([Parameter(Mandatory)][string[]]$MonitorNames)

    foreach ($name in $MonitorNames) {
        Invoke-Mmt -Arguments @("/TurnOn", "`"$name`"") | Out-Null
        Invoke-Mmt -Arguments @("/enable", "`"$name`"") | Out-Null
    }
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

function Set-ConsoleAudioOutput {
    param([Parameter(Mandatory)][string]$FriendlyId)

    Invoke-Svv -Arguments @("/SetDefault", "`"$FriendlyId`"", "all") | Out-Null
}

function Restore-ConsoleAudioOutput {
    if (-not $Script:ConsoleState.BackupAudioId) { return }
    if (-not (Test-SoundVolumeViewAvailable)) { return }

    try {
        Invoke-Svv -Arguments @("/SetDefault", "`"$($Script:ConsoleState.BackupAudioId)`"", "all") | Out-Null
    }
    catch {
        Write-Warning "Nao foi possivel restaurar o audio padrao: $_"
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

    $monitorInfo = Get-ConsoleMonitors | Where-Object { $_.Name -eq $MonitorName } | Select-Object -First 1
    $rect = Get-RectFromMonitorInfo -Monitor $monitorInfo
    if ($rect) { return $rect }

    $screen = Get-ScreenByDeviceName -DeviceName $MonitorName
    if ($screen) { return $screen.Bounds }
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
    foreach ($procName in @('XboxGameCallableUI', 'GamingApp', 'XboxPcApp')) {
        $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 }
        foreach ($p in $procs) {
            $area = [NativeHelpers]::GetWindowArea($p.MainWindowHandle)
            if ($area -gt 250000) { return $true }
        }
    }

    $windows = [NativeHelpers]::GetAllVisibleWindows()
    foreach ($w in $windows) {
        if ($w.Area -lt 250000) { continue }
        if ($w.Title -match 'Xbox|Game Pass|Gaming|Modo Xbox') { return $true }
        if ($w.ClassName -eq 'ApplicationFrameWindow' -and $w.Title -match 'Xbox') { return $true }
    }

    return $false
}

function Test-BigPictureActive {
    $handles = Get-BigPictureWindowHandles
    if ($handles.Count -eq 0) { return $false }

    foreach ($h in $handles) {
        $area = [NativeHelpers]::GetWindowArea($h)
        if ($area -gt 200000) { return $true }
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
        [string]$AudioDeviceId = ""
    )

    if ($Script:ConsoleState.IsActive) {
        throw "O modo console ja esta ativo."
    }

    $Script:ConsoleState.FocusMonitor = $FocusMonitor
    $Script:ConsoleState.HideMonitors = @($HideMonitors)
    $Script:ConsoleState.HideStrategy = $HideStrategy
    $Script:ConsoleState.FullscreenMode = $FullscreenMode
    $Script:ConsoleState.AudioDeviceId = $AudioDeviceId
    $Script:ConsoleState.ShouldExit = $false
    $Script:ConsoleState.SteamMoved = $false
    $Script:ConsoleState.MoveCount = 0
    $Script:ConsoleState.HasAppeared = $false
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.LaunchTime = $null
    $Script:ConsoleState.AbsenceCount = 0

    $focusMode = Get-ConsoleMonitors | Where-Object { $_.Name -eq $FocusMonitor } | Select-Object -First 1

    Save-MonitorBackup

    if (Test-SoundVolumeViewAvailable) {
        $Script:ConsoleState.BackupAudioId = Get-DefaultAudioDeviceId
        if ($Script:ConsoleState.BackupAudioId) {
            Set-Content -LiteralPath $Script:BackupAudioFile -Value $Script:ConsoleState.BackupAudioId -Encoding UTF8
        }
    }

    if (-not $focusMode) {
        Enable-Monitors -MonitorNames @($FocusMonitor)
        Start-Sleep -Milliseconds 200
    }

    Set-PrimaryMonitor -MonitorName $FocusMonitor

    if ($focusMode -and $focusMode.Frequency) {
        Set-MonitorMode -MonitorName $FocusMonitor -Width $focusMode.Width -Height $focusMode.Height -Frequency $focusMode.Frequency -Colors $focusMode.Colors
    }

    switch ($FullscreenMode) {
        "bigPicture" { Start-BigPictureMode }
        "xboxMode"   { Start-XboxMode }
    }

    if ($HideStrategy -eq "disconnect" -and $HideMonitors.Count -gt 0) {
        Disable-MonitorsWindows -MonitorNames $HideMonitors
    }
    elseif ($HideStrategy -eq "blackCurtain" -and $HideMonitors.Count -gt 0) {
        Show-BlackCurtains -MonitorNames $HideMonitors
    }
    elseif ($HideStrategy -eq "turnOff" -and $HideMonitors.Count -gt 0) {
        Disable-MonitorsDdc -MonitorNames $HideMonitors
    }

    if ($AudioDeviceId -and (Test-SoundVolumeViewAvailable)) {
        Set-ConsoleAudioOutput -FriendlyId $AudioDeviceId
    }

    $Script:ConsoleState.IsActive = $true
    $Script:ConsoleState.ModeLaunched = $true
    $Script:ConsoleState.LaunchTime = Get-Date
}

function Update-ConsoleMonitorLoop {
    if (-not $Script:ConsoleState.IsActive) { return "inactive" }
    if ($Script:ConsoleState.ShouldExit) { return "exit" }

    [System.Windows.Forms.Application]::DoEvents()

    $mode = $Script:ConsoleState.FullscreenMode
    $isActive = Test-FullscreenModeActive -Mode $mode

    if ($mode -eq "xboxMode" -and -not $Script:ConsoleState.HasAppeared -and $Script:ConsoleState.LaunchTime) {
        $elapsed = ((Get-Date) - $Script:ConsoleState.LaunchTime).TotalSeconds
        if ($elapsed -ge 2 -or $isActive) {
            $Script:ConsoleState.HasAppeared = $true
        }
    }
    elseif ($isActive) {
        $Script:ConsoleState.HasAppeared = $true
    }

    if ($mode -eq "bigPicture" -and $Script:ConsoleState.MoveCount -lt 12) {
        $bpHandles = Get-BigPictureWindowHandles
        if ($bpHandles.Count -gt 0) {
            $moved = Move-BigPictureToMonitor -MonitorName $Script:ConsoleState.FocusMonitor
            $Script:ConsoleState.MoveCount++
            if ($moved) { $Script:ConsoleState.SteamMoved = $true }

            foreach ($h in $bpHandles) {
                if ([NativeHelpers]::GetWindowArea($h) -gt 200000) {
                    $Script:ConsoleState.HasAppeared = $true
                    break
                }
            }
        }
    }

    $absenceThreshold = if ($mode -eq "bigPicture") { 3 } else { 2 }

    if ($isActive) {
        $Script:ConsoleState.AbsenceCount = 0
    }
    elseif ($Script:ConsoleState.HasAppeared) {
        $Script:ConsoleState.AbsenceCount++
        if ($Script:ConsoleState.AbsenceCount -ge $absenceThreshold) {
            return "exit"
        }
    }

    return "running"
}

function Stop-ConsoleMode {
    if (-not $Script:ConsoleState.IsActive -and $Script:ConsoleState.CurtainForms.Count -eq 0) {
        return
    }

    Close-BlackCurtains
    Start-Sleep -Milliseconds 300
    Restore-MonitorBackup
    Restore-ConsoleAudioOutput

    $Script:ConsoleState.IsActive = $false
    $Script:ConsoleState.ShouldExit = $false
    $Script:ConsoleState.SteamMoved = $false
    $Script:ConsoleState.MoveCount = 0
    $Script:ConsoleState.HasAppeared = $false
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.LaunchTime = $null
    $Script:ConsoleState.AbsenceCount = 0
    $Script:ConsoleState.CurtainForms = [System.Collections.ArrayList]@()
}

function Request-ConsoleModeExit {
    $Script:ConsoleState.ShouldExit = $true
}
