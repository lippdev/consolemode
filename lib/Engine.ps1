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
    ModeLaunched   = $false
    AbsenceCount   = 0
    FocusMonitor   = $null
    HideMonitors   = @()
    HideStrategy   = "blackCurtain"
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

            [PSCustomObject]@{
                Name       = $row.Name
                Resolution = $row.Resolution
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

    $stdoutPath = Join-Path $env:TEMP "consolemode_default_audio.txt"
    try {
        Start-Process -FilePath $Script:SvvPath -ArgumentList @("/Stdout") -Wait -WindowStyle Hidden -RedirectStandardOutput $stdoutPath | Out-Null
        if (-not (Test-Path -LiteralPath $stdoutPath)) { return $null }

        $lines = Get-Content -LiteralPath $stdoutPath -Encoding UTF8
        foreach ($line in $lines) {
            if ($line -match 'Default.*Render' -or $line -match '\(Default\)') {
                $parts = $line -split '\s{2,}'
                if ($parts.Count -ge 2) {
                    return $parts[-1].Trim()
                }
            }
        }

        $devices = Get-ConsoleAudioDevices
        $default = $devices | Where-Object { $_.IsDefault } | Select-Object -First 1
        return $default.FriendlyId
    }
    catch {
        return $null
    }
    finally {
        Remove-Item -LiteralPath $stdoutPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-ConsoleConfig {
    if (-not (Test-Path -LiteralPath $Script:ConfigPath)) {
        return [PSCustomObject]@{
            focusMonitor   = ""
            hideMonitors   = @()
            hideStrategy   = "blackCurtain"
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
            hideStrategy    = if ($raw.hideStrategy) { [string]$raw.hideStrategy } else { "blackCurtain" }
            fullscreenMode  = if ($raw.fullscreenMode) { [string]$raw.fullscreenMode } else { "bigPicture" }
            audioDeviceId   = [string]$raw.audioDeviceId
            audioDeviceName = [string]$raw.audioDeviceName
        }
    }
    catch {
        return [PSCustomObject]@{
            focusMonitor    = ""
            hideMonitors    = @()
            hideStrategy    = "blackCurtain"
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

function Enable-Monitors {
    param([Parameter(Mandatory)][string[]]$MonitorNames)

    foreach ($name in $MonitorNames) {
        Invoke-Mmt -Arguments @("/TurnOn", "`"$name`"") | Out-Null
        Invoke-Mmt -Arguments @("/enable", "`"$name`"") | Out-Null
    }
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

    $normalized = $DeviceName -replace '\\\\\.\\', ''
    foreach ($screen in [System.Windows.Forms.Screen]::AllScreens) {
        if ($screen.DeviceName -match [regex]::Escape($normalized)) {
            return $screen
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

        $form = New-Object System.Windows.Forms.Form
        $form.BackColor = [System.Drawing.Color]::Black
        $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
        $form.WindowState = [System.Windows.Forms.FormWindowState]::Maximized
        $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
        $form.Location = $screen.Bounds.Location
        $form.Size = $screen.Bounds.Size
        $form.TopMost = $true
        $form.ShowInTaskbar = $false
        $form.KeyPreview = $true

        $form.Add_KeyDown({
            param($sender, $e)
            if ($e.KeyCode -eq [System.Windows.Forms.Keys]::Escape) {
                $Script:ConsoleState.ShouldExit = $true
            }
        })

        $form.Show()
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

function Move-ProcessWindowToMonitor {
    param(
        [Parameter(Mandatory)][string]$ProcessName,
        [Parameter(Mandatory)][string]$MonitorName,
        [string]$TitlePattern = ""
    )

    $screen = Get-ScreenByDeviceName -DeviceName $MonitorName
    if (-not $screen) { return $false }

    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Where-Object {
        $_.MainWindowHandle -ne 0 -and (
            [string]::IsNullOrWhiteSpace($TitlePattern) -or
            $_.MainWindowTitle -match $TitlePattern
        )
    }

    $target = $processes | Select-Object -First 1
    if (-not $target) { return $false }

    $bounds = $screen.Bounds
    [NativeHelpers]::SetWindowPos(
        $target.MainWindowHandle,
        [IntPtr]::Zero,
        $bounds.X,
        $bounds.Y,
        $bounds.Width,
        $bounds.Height,
        0x0040
    ) | Out-Null

    return $true
}

function Start-BigPictureMode {
    Start-Process "steam://open/bigpicture"
}

function Start-XboxMode {
    Start-Sleep -Milliseconds 500
    [NativeHelpers]::SendWinF11()
}

function Test-BigPictureActive {
    $steamProcess = Get-Process -Name "steamwebhelper" -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -match "Steam" -or $_.MainWindowHandle -ne 0 } |
        Select-Object -First 1

    return [bool]$steamProcess
}

function Test-XboxModeActive {
    $xboxProcesses = @(
        "GamingApp",
        "XboxPcApp",
        "XboxApp",
        "GameBar",
        "ApplicationFrameHost"
    )

    foreach ($procName in $xboxProcesses) {
        $proc = Get-Process -Name $procName -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -match "Xbox|Gaming|Full.?screen" } |
            Select-Object -First 1
        if ($proc) { return $true }
    }

    $found = [NativeHelpers]::SearchXboxWindows()
    return $found
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
        [ValidateSet("blackCurtain", "turnOff")]
        [string]$HideStrategy = "blackCurtain",
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
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.AbsenceCount = 0

    Save-MonitorBackup

    if (Test-SoundVolumeViewAvailable) {
        $Script:ConsoleState.BackupAudioId = Get-DefaultAudioDeviceId
        if ($Script:ConsoleState.BackupAudioId) {
            Set-Content -LiteralPath $Script:BackupAudioFile -Value $Script:ConsoleState.BackupAudioId -Encoding UTF8
        }
    }

    Enable-Monitors -MonitorNames @($FocusMonitor)
    Start-Sleep -Seconds 1
    Set-PrimaryMonitor -MonitorName $FocusMonitor
    Start-Sleep -Seconds 1

    if ($HideStrategy -eq "blackCurtain" -and $HideMonitors.Count -gt 0) {
        Show-BlackCurtains -MonitorNames $HideMonitors
    }
    elseif ($HideStrategy -eq "turnOff" -and $HideMonitors.Count -gt 0) {
        Disable-MonitorsDdc -MonitorNames $HideMonitors
    }

    if ($AudioDeviceId -and (Test-SoundVolumeViewAvailable)) {
        Set-ConsoleAudioOutput -FriendlyId $AudioDeviceId
    }

    switch ($FullscreenMode) {
        "bigPicture" { Start-BigPictureMode }
        "xboxMode"   { Start-XboxMode }
    }

    $Script:ConsoleState.IsActive = $true
    $Script:ConsoleState.ModeLaunched = $true
}

function Update-ConsoleMonitorLoop {
    if (-not $Script:ConsoleState.IsActive) { return "inactive" }
    if ($Script:ConsoleState.ShouldExit) { return "exit" }

    [System.Windows.Forms.Application]::DoEvents()

    $mode = $Script:ConsoleState.FullscreenMode
    $isActive = Test-FullscreenModeActive -Mode $mode

    if ($mode -eq "bigPicture") {
        if ($isActive -and -not $Script:ConsoleState.SteamMoved) {
            $moved = Move-ProcessWindowToMonitor -ProcessName "steamwebhelper" -MonitorName $Script:ConsoleState.FocusMonitor -TitlePattern "Steam"
            if ($moved) {
                $Script:ConsoleState.SteamMoved = $true
            }
        }

        if ($isActive) {
            $Script:ConsoleState.AbsenceCount = 0
        }
        elseif ($Script:ConsoleState.ModeLaunched) {
            $Script:ConsoleState.AbsenceCount++
            if ($Script:ConsoleState.AbsenceCount -ge 3) {
                return "exit"
            }
        }
    }
    else {
        if ($isActive) {
            $Script:ConsoleState.AbsenceCount = 0
        }
        elseif ($Script:ConsoleState.ModeLaunched) {
            $Script:ConsoleState.AbsenceCount++
            if ($Script:ConsoleState.AbsenceCount -ge 5) {
                return "exit"
            }
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
    $Script:ConsoleState.ModeLaunched = $false
    $Script:ConsoleState.AbsenceCount = 0
    $Script:ConsoleState.CurtainForms = [System.Collections.ArrayList]@()
}

function Request-ConsoleModeExit {
    $Script:ConsoleState.ShouldExit = $true
}
