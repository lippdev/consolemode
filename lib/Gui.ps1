#Requires -Version 5.1
# Console Mode - Interface gráfica (wizard)

$script:TrayIcon = $null
$script:LoadedMonitors = @()
$script:AppIcon = $null
$script:WizardStep = 0
$script:AllowFormClosePrompt = $false
$script:ConsoleUiLocked = $false
$script:MonitorLoopBusy = $false
$script:LastStatusMessage = ""
$script:MonitorCancel = $false
$script:MonitorThread = $null
$script:AudioOnConnectId = "__on_connect__"
$script:AudioOnConnectLabel = "Usar áudio ao conectar (TV/monitor)"
$script:Theme = @{
    Bg      = [System.Drawing.Color]::FromArgb(28, 28, 30)
    Surface = [System.Drawing.Color]::FromArgb(37, 37, 40)
    Input   = [System.Drawing.Color]::FromArgb(50, 50, 54)
    Border  = [System.Drawing.Color]::FromArgb(62, 62, 66)
    Text    = [System.Drawing.Color]::FromArgb(230, 230, 230)
    Muted   = [System.Drawing.Color]::FromArgb(150, 150, 155)
    Accent  = [System.Drawing.Color]::FromArgb(62, 207, 160)
    Success = [System.Drawing.Color]::FromArgb(78, 201, 176)
    Warning = [System.Drawing.Color]::FromArgb(220, 180, 90)
}

function Get-ConsoleAppIcon {
    if ($script:AppIcon) { return $script:AppIcon }

    try {
        $assembly = [Reflection.Assembly]::GetExecutingAssembly()
        $location = $assembly.Location
        if ($location -and (Test-Path -LiteralPath $location) -and $location -like '*.exe') {
            $script:AppIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($location)
            if ($script:AppIcon) { return $script:AppIcon }
        }
    }
    catch { }

    $iconPath = Get-ConsoleIconPath
    if ($iconPath) {
        try {
            $script:AppIcon = New-Object System.Drawing.Icon($iconPath)
            return $script:AppIcon
        }
        catch { }
    }

    $script:AppIcon = [System.Drawing.SystemIcons]::Application
    return $script:AppIcon
}

function Move-FormToPrimaryScreen {
    param([System.Windows.Forms.Form]$Form)

    $targetScreen = $null
    $monitors = Get-ConsoleMonitors
    $primaryName = ($monitors | Where-Object { $_.IsPrimary } | Select-Object -First 1).Name

    if ($primaryName) {
        $targetScreen = Get-ScreenByDeviceName -DeviceName $primaryName
    }
    if (-not $targetScreen) {
        $targetScreen = [System.Windows.Forms.Screen]::PrimaryScreen
    }
    if (-not $targetScreen) { return }

    $wa = $targetScreen.WorkingArea
    $x = $wa.X + [Math]::Max(0, ($wa.Width - $Form.Width) / 2)
    $y = $wa.Y + [Math]::Max(0, ($wa.Height - $Form.Height) / 2)

    $Form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $Form.Location = New-Object System.Drawing.Point([int]$x, [int]$y)
}

function Minimize-ConsoleForm {
    param([System.Windows.Forms.Form]$Form)

    $Form.ShowInTaskbar = $false
    $Form.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
}

function Show-ConsoleActiveView {
    param(
        [System.Windows.Forms.Form]$Form,
        $WizardContext
    )

    Set-WizardEnabled @WizardContext -Enabled $false
    Minimize-ConsoleForm -Form $Form
}

function Show-FormOnPrimary {
    param([System.Windows.Forms.Form]$Form)

    $Form.ShowInTaskbar = $true
    $Form.WindowState = [System.Windows.Forms.FormWindowState]::Normal
    Move-FormToPrimaryScreen -Form $Form
    $Form.Show()
    $Form.Activate()
}

function New-Label {
    param(
        [string]$Text,
        [int]$X,
        [int]$Y,
        [int]$W = 200,
        [int]$H = 20,
        [System.Drawing.Font]$Font = $null
    )
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Text = $Text
    $lbl.Location = New-Object System.Drawing.Point($X, $Y)
    $lbl.Size = New-Object System.Drawing.Size($W, $H)
    $lbl.BackColor = $script:Theme.Bg
    $lbl.ForeColor = $script:Theme.Text
    if ($Font) { $lbl.Font = $Font }
    return $lbl
}

function Set-DarkThemeOnControl {
    param(
        [System.Windows.Forms.Control]$Control,
        [switch]$IsSurface
    )

    $bg = if ($IsSurface) { $script:Theme.Surface } else { $script:Theme.Bg }

    if ($Control -is [System.Windows.Forms.Form]) {
        $Control.BackColor = $script:Theme.Bg
        $Control.ForeColor = $script:Theme.Text
    }
    elseif ($Control -is [System.Windows.Forms.Panel]) {
        $Control.BackColor = $bg
        $Control.ForeColor = $script:Theme.Text
    }
    elseif ($Control -is [System.Windows.Forms.GroupBox]) {
        $Control.BackColor = $script:Theme.Bg
        $Control.ForeColor = $script:Theme.Accent
    }
    elseif ($Control -is [System.Windows.Forms.Label]) {
        $Control.BackColor = $bg
        if ($Control.ForeColor -eq [System.Drawing.Color]::Empty -or
            $Control.ForeColor -eq [System.Drawing.SystemColors]::ControlText) {
            $Control.ForeColor = $script:Theme.Text
        }
    }
    elseif ($Control -is [System.Windows.Forms.Button]) {
        if ($Control.BackColor -eq [System.Drawing.Color]::Empty -or
            $Control.BackColor -eq [System.Drawing.SystemColors]::Control) {
            $Control.BackColor = $script:Theme.Input
            $Control.ForeColor = $script:Theme.Text
            $Control.FlatAppearance.BorderColor = $script:Theme.Border
        }
        $Control.UseVisualStyleBackColor = $false
    }
    elseif ($Control -is [System.Windows.Forms.ComboBox]) {
        $Control.BackColor = $script:Theme.Input
        $Control.ForeColor = $script:Theme.Text
        $Control.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    }
    elseif ($Control -is [System.Windows.Forms.RadioButton] -or
            $Control -is [System.Windows.Forms.CheckBox]) {
        $Control.BackColor = $bg
        $Control.ForeColor = $script:Theme.Text
        $Control.UseVisualStyleBackColor = $false
    }

    foreach ($child in $Control.Controls) {
        $childSurface = $IsSurface -or ($Control -is [System.Windows.Forms.GroupBox])
        Set-DarkThemeOnControl -Control $child -IsSurface:($childSurface -or ($child -is [System.Windows.Forms.Panel]))
    }
}

function New-StyledButton {
    param(
        [string]$Text,
        [int]$X,
        [int]$Y,
        [int]$W,
        [int]$H,
        [switch]$Primary
    )
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = $Text
    $btn.Location = New-Object System.Drawing.Point($X, $Y)
    $btn.Size = New-Object System.Drawing.Size($W, $H)
    $btn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $btn.UseVisualStyleBackColor = $false
    if ($Primary) {
        $btn.BackColor = $script:Theme.Accent
        $btn.ForeColor = [System.Drawing.Color]::FromArgb(28, 28, 30)
        $btn.FlatAppearance.BorderSize = 0
        $btn.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
    }
    else {
        $btn.BackColor = $script:Theme.Input
        $btn.ForeColor = $script:Theme.Text
        $btn.FlatAppearance.BorderColor = $script:Theme.Border
    }
    return $btn
}

function Show-StatusMessage {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        [string]$Message,
        [System.Drawing.Color]$Color,
        [switch]$Force
    )

    if (-not $Force -and $script:LastStatusMessage -eq $Message) { return }
    $script:LastStatusMessage = $Message
    $StatusLabel.Text = $Message
    $StatusLabel.ForeColor = $Color

    if ($Form.WindowState -ne [System.Windows.Forms.FormWindowState]::Minimized -and $Form.Visible) {
        $StatusLabel.Refresh()
    }
}

function Get-MonitorFriendlyLabel {
    param($Monitor)

    $shortName = ($Monitor.Name -replace '\\\\\.\\', '').Trim()
    if ($Monitor.MonitorName) {
        return "$($Monitor.MonitorName) ($shortName)"
    }
    return $shortName
}

function Get-MonitorSecondaryLabel {
    param($Monitor)
    $parts = @()
    if ($Monitor.Resolution) { $parts += $Monitor.Resolution }
    if ($Monitor.Frequency) { $parts += "$($Monitor.Frequency) Hz" }
    if ($Monitor.IsPrimary -and $Monitor.IsActive) { $parts += "Primário" }
    if (-not $Monitor.IsActive) { $parts += "Desconectado" }
    return ($parts -join " · ")
}

$script:FpsPresetValues = @(30, 48, 50, 59, 60, 72, 75, 90, 120, 144)
$script:FpsCustomComboValue = -1

function Initialize-FpsLimitCombo {
    param([System.Windows.Forms.ComboBox]$Combo)

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "(Não limitar)"; Value = 0 })
    foreach ($fps in $script:FpsPresetValues) {
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = "$fps FPS"; Value = $fps })
    }
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "Personalizado"; Value = $script:FpsCustomComboValue })
    $Combo.DisplayMember = "Text"
}

function Select-FpsLimitInCombo {
    param(
        [System.Windows.Forms.ComboBox]$Combo,
        [System.Windows.Forms.NumericUpDown]$CustomNumeric,
        [int]$SavedLimit
    )

    if ($SavedLimit -le 0) {
        $Combo.SelectedIndex = 0
        $CustomNumeric.Visible = $false
        return
    }

    $selectedIndex = 0
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ([int]$Combo.Items[$i].Value -eq $SavedLimit) {
            $selectedIndex = $i
            $CustomNumeric.Visible = $false
            $Combo.SelectedIndex = $selectedIndex
            return
        }
    }

    $CustomNumeric.Value = [Math]::Max(1, [Math]::Min(360, $SavedLimit))
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ([int]$Combo.Items[$i].Value -eq $script:FpsCustomComboValue) {
            $Combo.SelectedIndex = $i
            break
        }
    }
    $CustomNumeric.Visible = $true
}

function Get-FpsLimitFromControls {
    param(
        [System.Windows.Forms.ComboBox]$Combo,
        [System.Windows.Forms.NumericUpDown]$CustomNumeric
    )

    $item = $Combo.SelectedItem
    if (-not $item) { return 0 }
    $value = [int]$item.Value
    if ($value -eq $script:FpsCustomComboValue) {
        return [int]$CustomNumeric.Value
    }
    return $value
}

function Update-FpsLimitStatusLabel {
    param([System.Windows.Forms.Label]$StatusLabel)

    if (Test-ConsoleRtssReady) {
        $StatusLabel.Text = "Requer RivaTuner em execução. Limite global durante o modo console (restaurado ao sair)."
        $StatusLabel.ForeColor = $script:Theme.Muted
    }
    elseif (Test-RtssInstalled) {
        $StatusLabel.Text = "RTSS instalado, mas rtss-cli ausente. Execute build\Get-RtssCli.ps1."
        $StatusLabel.ForeColor = $script:Theme.Warning
    }
    else {
        $StatusLabel.Text = "Instale RivaTuner Statistics Server (MSI Afterburner) para usar limite de FPS."
        $StatusLabel.ForeColor = $script:Theme.Warning
    }
}

function Suggest-FpsLimitForFocus {
    param(
        [System.Windows.Forms.ComboBox]$Combo,
        [System.Windows.Forms.NumericUpDown]$CustomNumeric,
        [string]$FocusMonitorName,
        [int]$CurrentSavedLimit
    )

    if ($CurrentSavedLimit -gt 0) { return }

    $monitor = $script:LoadedMonitors | Where-Object { $_.Name -eq $FocusMonitorName } | Select-Object -First 1
    if (-not $monitor -or -not $monitor.Frequency) { return }

    $hz = 0
    if (-not [int]::TryParse([string]$monitor.Frequency, [ref]$hz)) { return }
    if ($hz -le 0) { return }

    Select-FpsLimitInCombo -Combo $Combo -CustomNumeric $CustomNumeric -SavedLimit $hz
}

function Get-GuiSelections {
    param([System.Windows.Forms.Panel]$MonitorPanel)

    $focusMonitor = ""
    $hideMonitors = [System.Collections.ArrayList]@()

    foreach ($control in $MonitorPanel.Controls) {
        if ($control -is [System.Windows.Forms.RadioButton] -and $control.Checked) {
            $focusMonitor = [string]$control.Tag
        }
        if ($control -is [System.Windows.Forms.CheckBox] -and $control.Checked -and $control.Tag) {
            [void]$hideMonitors.Add([string]$control.Tag)
        }
    }

    return @{
        FocusMonitor = $focusMonitor
        HideMonitors = @($hideMonitors)
    }
}

function Build-MonitorLayoutDiagram {
    param(
        [System.Windows.Forms.Panel]$Panel,
        [array]$Monitors,
        [string]$FocusName
    )

    $Panel.Controls.Clear()
    $Panel.BackColor = $script:Theme.Surface

    if ($Monitors.Count -eq 0) {
        $lbl = New-Label -Text "Nenhum monitor para exibir." -X 10 -Y 10 -W 300
        $Panel.Controls.Add($lbl)
        return
    }

    $active = @($Monitors | Where-Object { $_.IsActive -and $_.Width -gt 0 -and $_.Height -gt 0 })
    if ($active.Count -eq 0) {
        $active = @($Monitors)
    }

    $minX = ($active | ForEach-Object {
        if ($_.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') { [int]$Matches[1] } else { 0 }
    } | Measure-Object -Minimum).Minimum
    $minY = ($active | ForEach-Object {
        if ($_.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') { [int]$Matches[2] } else { 0 }
    } | Measure-Object -Minimum).Minimum

    $maxX = 0
    $maxY = 0
    foreach ($m in $active) {
        $x = 0; $y = 0
        if ($m.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') {
            $x = [int]$Matches[1]
            $y = [int]$Matches[2]
        }
        $w = if ($m.Width) { [int]$m.Width } else { 1920 }
        $h = if ($m.Height) { [int]$m.Height } else { 1080 }
        $maxX = [Math]::Max($maxX, ($x - $minX) + $w)
        $maxY = [Math]::Max($maxY, ($y - $minY) + $h)
    }
    if ($maxX -le 0) { $maxX = 1920 }
    if ($maxY -le 0) { $maxY = 1080 }

    $pad = 10
    $availW = [Math]::Max(100, $Panel.Width - ($pad * 2))
    $availH = [Math]::Max(40, $Panel.Height - ($pad * 2))
    $scale = [Math]::Min($availW / $maxX, $availH / $maxY)

    foreach ($m in $active) {
        $x = 0; $y = 0
        if ($m.LeftTop -match '(-?\d+)\s*,\s*(-?\d+)') {
            $x = [int]$Matches[1] - $minX
            $y = [int]$Matches[2] - $minY
        }
        $w = if ($m.Width) { [int]$m.Width } else { 1920 }
        $h = if ($m.Height) { [int]$m.Height } else { 1080 }

        $box = New-Object System.Windows.Forms.Panel
        $box.Location = New-Object System.Drawing.Point(
            [int]($pad + ($x * $scale)),
            [int]($pad + ($y * $scale))
        )
        $box.Size = New-Object System.Drawing.Size(
            [Math]::Max(24, [int]($w * $scale)),
            [Math]::Max(18, [int]($h * $scale))
        )
        $isFocus = ($m.Name -eq $FocusName)
        if ($isFocus) {
            $box.BackColor = $script:Theme.Accent
            $box.ForeColor = [System.Drawing.Color]::FromArgb(28, 28, 30)
        }
        elseif (-not $m.IsActive) {
            $box.BackColor = [System.Drawing.Color]::FromArgb(55, 55, 58)
            $box.ForeColor = $script:Theme.Muted
        }
        else {
            $box.BackColor = [System.Drawing.Color]::FromArgb(70, 70, 74)
            $box.ForeColor = $script:Theme.Text
        }

        $lbl = New-Object System.Windows.Forms.Label
        $lbl.Text = (Get-MonitorFriendlyLabel -Monitor $m)
        $lbl.Dock = [System.Windows.Forms.DockStyle]::Fill
        $lbl.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $lbl.ForeColor = $box.ForeColor
        $lbl.Font = New-Object System.Drawing.Font("Segoe UI", 7.5)
        $box.Controls.Add($lbl)
        $Panel.Controls.Add($box)
    }
}

function Build-MonitorPanel {
    param(
        [System.Windows.Forms.Panel]$Panel,
        [array]$Monitors,
        [string]$SavedFocus,
        [string[]]$SavedHide
    )

    $Panel.Controls.Clear()

    if ($Monitors.Count -eq 0) {
        $lbl = New-Label -Text "Nenhum monitor detectado." -X 10 -Y 10 -W 500
        $Panel.Controls.Add($lbl)
        return
    }

    $colFoco = 10
    $colHide = 70
    $colName = 140

    $hdrFoco = New-Label -Text "Foco" -X $colFoco -Y 4 -W 50 -H 18 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold))
    $Panel.Controls.Add($hdrFoco)

    $hdrHide = New-Label -Text "Esconder" -X $colHide -Y 4 -W 70 -H 18 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold))
    $Panel.Controls.Add($hdrHide)

    $hdrName = New-Label -Text "Monitor" -X $colName -Y 4 -W 420 -H 18 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold))
    $Panel.Controls.Add($hdrName)

    $y = 26
    $firstMonitor = $true
    foreach ($monitor in $Monitors) {
        $friendly = Get-MonitorFriendlyLabel -Monitor $monitor
        $secondary = Get-MonitorSecondaryLabel -Monitor $monitor

        $radio = New-Object System.Windows.Forms.RadioButton
        $radio.Text = ""
        $radio.Tag = $monitor.Name
        $radio.Location = New-Object System.Drawing.Point(($colFoco + 12), $y)
        $radio.Size = New-Object System.Drawing.Size(20, 20)
        if ($SavedFocus) {
            $radio.Checked = ($monitor.Name -eq $SavedFocus)
        }
        elseif ($firstMonitor) {
            $radio.Checked = $true
        }
        $Panel.Controls.Add($radio)

        $check = New-Object System.Windows.Forms.CheckBox
        $check.Text = ""
        $check.Tag = $monitor.Name
        $check.Location = New-Object System.Drawing.Point(($colHide + 22), $y)
        $check.Size = New-Object System.Drawing.Size(20, 20)
        $check.Checked = $SavedHide -contains $monitor.Name
        $check.Enabled = $monitor.IsActive
        $Panel.Controls.Add($check)

        $nameLabel = New-Label -Text $friendly -X $colName -Y $y -W 420 -H 16 `
            -Font (New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold))
        if (-not $monitor.IsActive) {
            $nameLabel.ForeColor = $script:Theme.Muted
        }
        $Panel.Controls.Add($nameLabel)

        $subLabel = New-Label -Text $secondary -X ($colName + 2) -Y ($y + 16) -W 420 -H 14
        $subLabel.ForeColor = $script:Theme.Muted
        $subLabel.Font = New-Object System.Drawing.Font("Segoe UI", 8)
        $Panel.Controls.Add($subLabel)

        $y += 38
        $firstMonitor = $false
    }
}

function Populate-AudioCombo {
    param(
        [System.Windows.Forms.ComboBox]$Combo,
        [string]$SavedAudioId,
        [string]$SavedAudioName = "",
        [bool]$SavedAudioAutoSwitch = $false
    )

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "(Não mudar)"; Id = "" })
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = $script:AudioOnConnectLabel; Id = $script:AudioOnConnectId })

    if (-not (Test-SoundVolumeViewAvailable)) {
        $Combo.SelectedIndex = 0
        return
    }

    $devices = @(Get-ConsoleAudioDevices | Where-Object { $_.IsActive })
    foreach ($device in $devices) {
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = $device.Name; Id = $device.FriendlyId })
    }

    $selectedIndex = 0
    $targetId = if ($SavedAudioAutoSwitch) { $script:AudioOnConnectId } else { $SavedAudioId }
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ($Combo.Items[$i].Id -eq $targetId) {
            $selectedIndex = $i
            break
        }
    }
    $Combo.SelectedIndex = $selectedIndex
}

function Get-HideStrategyFromCombo {
    param([System.Windows.Forms.ComboBox]$Combo)

    switch ($Combo.SelectedIndex) {
        1 { return "blackCurtain" }
        2 { return "turnOff" }
        default { return "disconnect" }
    }
}

function Get-HideStrategyLabel {
    param([string]$Strategy)

    switch ($Strategy) {
        "blackCurtain" { return "Cortinas pretas" }
        "turnOff" { return "Desligar fisicamente (DDC/CI)" }
        default { return "Desconectar monitores" }
    }
}

function Get-FullscreenModeLabel {
    param([string]$Mode)
    if ($Mode -eq "xboxMode") { return "Modo Xbox (Win+F11) — Alpha" }
    return "Steam Big Picture (recomendado)"
}

function Set-ActiveConsoleMessaging {
    param(
        [System.Windows.Forms.Label]$ActiveDesc,
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        [string]$FullscreenMode
    )

    if ($FullscreenMode -eq "xboxMode") {
        $ActiveDesc.Text = @(
            "Modo Xbox (Alpha): sem restauração automática."
            "Ao terminar, use Restaurar agora, o menu da bandeja ou Mostrar janela."
        ) -join [Environment]::NewLine
        Show-StatusMessage -Form $Form -StatusLabel $StatusLabel `
            -Message "Modo Xbox (Alpha) ativo. Restaure manualmente ao sair." `
            -Color $script:Theme.Warning -Force
    }
    else {
        $ActiveDesc.Text = "O app fica minimizado na bandeja. Ao sair do Big Picture, o setup é restaurado automaticamente."
        Show-StatusMessage -Form $Form -StatusLabel $StatusLabel `
            -Message "Modo console ativo. Ao sair do Big Picture, tudo será restaurado." `
            -Color $script:Theme.Accent -Force
    }
}

function Test-ConsoleWatchNeeded {
  param([string]$FullscreenMode)

  if ($FullscreenMode -eq "bigPicture") { return $true }
  return (Test-ConsoleAudioWatchNeeded)
}

function Get-WizardSelections {
    param(
        $MonitorPanel,
        $HideStrategyCombo,
        $ModeBigPicture,
        $AudioCombo,
        $FpsLimitCombo = $null,
        $FpsCustomNumeric = $null
    )

    $selection = Get-GuiSelections -MonitorPanel $MonitorPanel
    $hideStrategy = Get-HideStrategyFromCombo -Combo $HideStrategyCombo
    $fullscreenMode = if ($ModeBigPicture.Checked) { "bigPicture" } else { "xboxMode" }
    $audioItem = $AudioCombo.SelectedItem
    $audioId = if ($audioItem) { [string]$audioItem.Id } else { "" }
    $audioName = if ($audioItem) { [string]$audioItem.Text } else { "" }
    $audioAutoSwitch = ($audioId -eq $script:AudioOnConnectId -or $audioId -eq "__auto__")
    if ($audioAutoSwitch) {
        $audioId = ""
        $audioName = $script:AudioOnConnectLabel
    }

    $hideMonitors = @($selection.HideMonitors | Where-Object { $_ -ne $selection.FocusMonitor })
    if ($hideMonitors.Count -eq 0) {
        $hideMonitors = @($script:LoadedMonitors | Where-Object {
            $_.Name -ne $selection.FocusMonitor -and $_.IsActive
        } | ForEach-Object { $_.Name })
    }

    $focusInfo = $script:LoadedMonitors | Where-Object { $_.Name -eq $selection.FocusMonitor } | Select-Object -First 1
    $focusLabel = if ($focusInfo) { Get-MonitorFriendlyLabel -Monitor $focusInfo } else { $selection.FocusMonitor }

    $hideLabels = @()
    foreach ($name in $hideMonitors) {
        $m = $script:LoadedMonitors | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        $hideLabels += if ($m) { Get-MonitorFriendlyLabel -Monitor $m } else { $name }
    }

    $audioHint = ""
    if ($focusInfo -and $focusInfo.MonitorName) {
        $audioHint = $focusInfo.MonitorName
    }

    $fpsLimit = 0
    if ($FpsLimitCombo -and $FpsCustomNumeric) {
        $fpsLimit = Get-FpsLimitFromControls -Combo $FpsLimitCombo -CustomNumeric $FpsCustomNumeric
    }

    return @{
        FocusMonitor   = $selection.FocusMonitor
        FocusLabel     = $focusLabel
        HideMonitors   = $hideMonitors
        HideLabels     = $hideLabels
        HideStrategy   = $hideStrategy
        FullscreenMode = $fullscreenMode
        AudioDeviceId  = $audioId
        AudioDeviceName = $audioName
        AudioAutoSwitch = $audioAutoSwitch
        AudioDeviceHint = $audioHint
        FocusMonitorInfo = $focusInfo
        FpsLimit       = $fpsLimit
    }
}

function Save-FromWizard {
    param($Form, $WizardData, $StatusLabel)

    $data = Get-WizardSelections @WizardData

    if ([string]::IsNullOrWhiteSpace($data.FocusMonitor)) {
        [System.Windows.Forms.MessageBox]::Show(
            "Selecione o monitor de foco (TV/console).",
            "Console Mode",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        ) | Out-Null
        return $false
    }

    Set-ConsoleConfig `
        -FocusMonitor $data.FocusMonitor `
        -HideMonitors $data.HideMonitors `
        -HideStrategy $data.HideStrategy `
        -FullscreenMode $data.FullscreenMode `
        -AudioDeviceId $data.AudioDeviceId `
        -AudioDeviceName $data.AudioDeviceName `
        -AudioAutoSwitch $data.AudioAutoSwitch `
        -FpsLimit $data.FpsLimit

    Show-StatusMessage -Form $Form -StatusLabel $StatusLabel -Message "Configuração salva." -Color $script:Theme.Success
    return $true
}

function Update-ReviewPanel {
    param(
        [System.Windows.Forms.Label]$ReviewLabel,
        $WizardData
    )

    $data = Get-WizardSelections @WizardData
    $hideText = if ($data.HideLabels.Count -gt 0) { $data.HideLabels -join ", " } else { "(nenhum)" }
    $audioText = if ($data.AudioAutoSwitch) {
        $script:AudioOnConnectLabel
    }
    elseif ($data.AudioDeviceName) {
        $data.AudioDeviceName
    }
    else {
        "(não mudar)"
    }

    $fpsText = Get-FpsLimitLabel -FpsLimit $data.FpsLimit

    $ReviewLabel.Text = @(
        "Monitor de foco: $($data.FocusLabel)"
        "Esconder: $hideText"
        "Estratégia: $(Get-HideStrategyLabel -Strategy $data.HideStrategy)"
        "Modo: $(Get-FullscreenModeLabel -Mode $data.FullscreenMode)"
        "Áudio: $audioText"
        "Limite de FPS: $fpsText"
    ) -join [Environment]::NewLine
}

function Set-WizardEnabled {
    param(
        $Form,
        [bool]$Enabled,
        $WizardPanels,
        $NavButtons,
        $ActivePanel,
        $RestoreButton
    )

    foreach ($panel in $WizardPanels) {
        if ($panel) { $panel.Visible = $Enabled }
    }
    foreach ($btn in $NavButtons) {
        if ($btn) { $btn.Enabled = $Enabled }
    }
    if ($ActivePanel) { $ActivePanel.Visible = -not $Enabled }
    if ($RestoreButton) { $RestoreButton.Enabled = -not $Enabled }
}

function Initialize-TrayIcon {
    param(
        [System.Windows.Forms.Form]$Form,
        [scriptblock]$OnRestore
    )

    if ($script:TrayIcon) { return }

    $script:TrayIcon = New-Object System.Windows.Forms.NotifyIcon
    $script:TrayIcon.Icon = Get-ConsoleAppIcon
    $script:TrayIcon.Text = "Console Mode"
    $script:TrayIcon.Visible = $true

    $menu = New-Object System.Windows.Forms.ContextMenuStrip
    $restoreItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $restoreItem.Text = "Restaurar setup"
    $restoreItem.Add_Click($OnRestore)
    [void]$menu.Items.Add($restoreItem)

    $showItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $showItem.Text = "Mostrar janela"
    $showItem.Add_Click({ Show-FormOnPrimary -Form $Form })
    [void]$menu.Items.Add($showItem)

    $exitItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $exitItem.Text = "Sair"
    $exitItem.Add_Click({
        if ($Script:ConsoleState.IsActive) { Stop-ConsoleMode }
        $script:ConsoleUiLocked = $false
        $script:TrayIcon.Visible = $false
        $script:TrayIcon.Dispose()
        $script:AllowFormClosePrompt = $true
        $Form.Close()
    })
    [void]$menu.Items.Add($exitItem)

    $script:TrayIcon.ContextMenuStrip = $menu
    $script:TrayIcon.Add_DoubleClick({ Show-FormOnPrimary -Form $Form })
}

function Stop-MonitorWorker {
    $script:MonitorCancel = $true
    if ($script:MonitorThread -and $script:MonitorThread.IsAlive) {
        $script:MonitorThread.Join(1500) | Out-Null
    }
    $script:MonitorThread = $null
}

function Invoke-ConsoleMonitorExitUi {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        $WizardContext
    )

    Stop-MonitorWorker
    Stop-ConsoleMode
    $script:ConsoleUiLocked = $false
    Set-WizardEnabled @WizardContext -Enabled $true
    Show-FormOnPrimary -Form $Form
    Show-StatusMessage -Form $Form -StatusLabel $StatusLabel `
        -Message "Modo console encerrado. Setup restaurado." `
        -Color $script:Theme.Success -Force
}

function Start-MonitorTimer {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        $WizardContext
    )

    Stop-MonitorWorker
    $script:MonitorCancel = $false

    $formRef = $Form
    $statusRef = $StatusLabel
    $contextRef = $WizardContext

    $script:MonitorThread = New-Object System.Threading.Thread(
        [System.Threading.ParameterizedThreadStart]{
            param($state)
            $formLocal = $state[0]
            $statusLocal = $state[1]
            $contextLocal = $state[2]

            while (-not $script:MonitorCancel -and $Script:ConsoleState.IsActive) {
                if ($script:MonitorLoopBusy) {
                    Start-Sleep -Milliseconds 250
                    continue
                }

                $script:MonitorLoopBusy = $true
                $result = "running"
                try {
                    $result = Update-ConsoleMonitorLoop
                }
                catch { }
                finally {
                    $script:MonitorLoopBusy = $false
                }

                if ($result -eq "exit") {
                    if ($formLocal -and -not $formLocal.IsDisposed) {
                        [void]$formLocal.BeginInvoke([Action]{
                            Invoke-ConsoleMonitorExitUi -Form $formLocal -StatusLabel $statusLocal -WizardContext $contextLocal
                        })
                    }
                    break
                }

                if ($result -eq "running" -and $Script:ConsoleState.LastAudioSwitchName) {
                    if ($formLocal -and -not $formLocal.IsDisposed) {
                        $audioName = $Script:ConsoleState.LastAudioSwitchName
                        $Script:ConsoleState.LastAudioSwitchName = $null
                        [void]$formLocal.BeginInvoke([Action]{
                            Show-StatusMessage -Form $formLocal -StatusLabel $statusLocal `
                                -Message "Áudio alterado para: $audioName." -Color $script:Theme.Accent -Force
                        })
                    }
                }

                $delay = Get-ConsoleMonitorPollDelayMs
                $waited = 0
                while ($waited -lt $delay -and -not $script:MonitorCancel -and $Script:ConsoleState.IsActive) {
                    if ([NativeHelpers]::BigPictureExitRequested) { break }
                    Start-Sleep -Milliseconds 200
                    $waited += 200
                }
            }
        }
    )
    $script:MonitorThread.IsBackground = $true
    $script:MonitorThread.Priority = [System.Threading.ThreadPriority]::BelowNormal
    $script:MonitorThread.Name = "ConsoleModeMonitor"
    [void]$script:MonitorThread.Start(@($formRef, $statusRef, $contextRef))
}

function Show-WizardStep {
    param(
        [int]$Step,
        $StepPanels,
        $ProgressLabels,
        $BtnBack,
        $BtnNext,
        $BtnStart,
        $ReviewPanel
    )

    $script:WizardStep = $Step
    for ($i = 0; $i -lt $StepPanels.Count; $i++) {
        $StepPanels[$i].Visible = ($i -eq $Step)
    }

    for ($i = 0; $i -lt $ProgressLabels.Count; $i++) {
        if ($i -eq $Step) {
            $ProgressLabels[$i].Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
            $ProgressLabels[$i].ForeColor = $script:Theme.Accent
        }
        else {
            $ProgressLabels[$i].Font = New-Object System.Drawing.Font("Segoe UI", 9)
            $ProgressLabels[$i].ForeColor = $script:Theme.Muted
        }
    }

    $BtnBack.Enabled = ($Step -gt 0)
    $BtnNext.Visible = ($Step -lt 3)
    $BtnStart.Visible = ($Step -eq 3)
    $ReviewPanel.Visible = ($Step -eq 3)
}

function Show-ConsoleModeGui {
    if (-not (Test-MultiMonitorToolAvailable)) {
        [System.Windows.Forms.MessageBox]::Show(
            "MultiMonitorTool.exe não encontrado.`n`nEm desenvolvimento: coloque na pasta do projeto.`nNo executável: será extraído em ConsoleMode_Data\tools na primeira execução.",
            "Console Mode - Erro",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
        return
    }

    $config = Get-ConsoleConfig
    $monitors = Get-ConsoleMonitors
    $script:LoadedMonitors = $monitors

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Console Mode"
    $form.Size = New-Object System.Drawing.Size(720, 640)
    $form.MinimumSize = New-Object System.Drawing.Size(720, 640)
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false
    $form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)
    $form.BackColor = $script:Theme.Bg
    $form.ForeColor = $script:Theme.Text
    $form.Icon = Get-ConsoleAppIcon
    Move-FormToPrimaryScreen -Form $form
    $form.Add_Shown({ Move-FormToPrimaryScreen -Form $form })

    $form.Add_FormClosing({
        param($sender, $e)

        if (($Script:ConsoleState.IsActive -or $script:ConsoleUiLocked) -and -not $script:AllowFormClosePrompt) {
            if ($e.CloseReason -eq [System.Windows.Forms.CloseReason]::UserClosing) {
                $answer = [System.Windows.Forms.MessageBox]::Show(
                    "O modo console está ativo. Restaurar o setup e sair?`n`nNão = manter minimizado na bandeja.",
                    "Console Mode",
                    [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
                    [System.Windows.Forms.MessageBoxIcon]::Question
                )
                if ($answer -eq [System.Windows.Forms.DialogResult]::Cancel) {
                    $e.Cancel = $true
                    return
                }
                if ($answer -eq [System.Windows.Forms.DialogResult]::No) {
                    $e.Cancel = $true
                    Minimize-ConsoleForm -Form $sender
                    return
                }
                Stop-MonitorWorker
                Stop-ConsoleMode
                $script:ConsoleUiLocked = $false
                $script:AllowFormClosePrompt = $true
                return
            }

            $e.Cancel = $true
            return
        }

        Stop-MonitorWorker
        if ($script:TrayIcon) {
            $script:TrayIcon.Visible = $false
            $script:TrayIcon.Dispose()
        }
    })

    $titleLbl = New-Label -Text "Console Mode" -X 20 -Y 14 -W 300 -H 28 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold))
    $titleLbl.ForeColor = $script:Theme.Accent
    $form.Controls.Add($titleLbl)

    $subtitleLbl = New-Label -Text "Transforme seu PC em console de jogos em poucos passos." -X 20 -Y 44 -W 660 -H 20
    $subtitleLbl.ForeColor = $script:Theme.Muted
    $form.Controls.Add($subtitleLbl)

    $progressY = 72
    $stepTitles = @("1. Monitores", "2. Modo", "3. Áudio", "Iniciar")
    $progressLabels = @()
    $px = 20
    foreach ($title in $stepTitles) {
        $pl = New-Label -Text $title -X $px -Y $progressY -W 150 -H 20
        $form.Controls.Add($pl)
        $progressLabels += $pl
        $px += 155
    }

    $linePanel = New-Object System.Windows.Forms.Panel
    $linePanel.Location = New-Object System.Drawing.Point(20, ($progressY + 22))
    $linePanel.Size = New-Object System.Drawing.Size(660, 2)
    $linePanel.BackColor = $script:Theme.Border
    $form.Controls.Add($linePanel)

    $contentTop = $progressY + 36
    $contentH = 400

    $step1 = New-Object System.Windows.Forms.Panel
    $step1.Location = New-Object System.Drawing.Point(15, $contentTop)
    $step1.Size = New-Object System.Drawing.Size(680, $contentH)
    $form.Controls.Add($step1)

    $diagramPanel = New-Object System.Windows.Forms.Panel
    $diagramPanel.Location = New-Object System.Drawing.Point(5, 5)
    $diagramPanel.Size = New-Object System.Drawing.Size(650, 70)
    $step1.Controls.Add($diagramPanel)

    $monitorPanel = New-Object System.Windows.Forms.Panel
    $monitorPanel.Name = "monitorPanel"
    $monitorPanel.Location = New-Object System.Drawing.Point(5, 80)
    $monitorPanel.Size = New-Object System.Drawing.Size(650, 165)
    $monitorPanel.AutoScroll = $true
    $step1.Controls.Add($monitorPanel)

    Build-MonitorPanel -Panel $monitorPanel -Monitors $monitors -SavedFocus $config.focusMonitor -SavedHide $config.hideMonitors
    $initialFocus = if ($config.focusMonitor) { $config.focusMonitor } else { ($monitors | Select-Object -First 1).Name }
    Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $monitors -FocusName $initialFocus

    $btnHideOthers = New-StyledButton -Text "Esconder todos exceto o foco" -X 5 -Y 252 -W 220 -H 30
    $btnHideOthers.Add_Click({
        $sel = Get-GuiSelections -MonitorPanel $monitorPanel
        foreach ($control in $monitorPanel.Controls) {
            if ($control -is [System.Windows.Forms.CheckBox] -and $control.Tag) {
                $control.Checked = ($control.Tag -ne $sel.FocusMonitor) -and $control.Enabled
            }
        }
        Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $script:LoadedMonitors -FocusName $sel.FocusMonitor
    })
    $step1.Controls.Add($btnHideOthers)

    $fpsGroup = New-Object System.Windows.Forms.GroupBox
    $fpsGroup.Text = "Limite de FPS (anti-tearing)"
    $fpsGroup.Location = New-Object System.Drawing.Point(5, 288)
    $fpsGroup.Size = New-Object System.Drawing.Size(650, 100)
    $step1.Controls.Add($fpsGroup)

    $fpsLimitCombo = New-Object System.Windows.Forms.ComboBox
    $fpsLimitCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $fpsLimitCombo.Location = New-Object System.Drawing.Point(15, 28)
    $fpsLimitCombo.Size = New-Object System.Drawing.Size(220, 28)
    $fpsGroup.Controls.Add($fpsLimitCombo)
    Initialize-FpsLimitCombo -Combo $fpsLimitCombo

    $fpsCustomNumeric = New-Object System.Windows.Forms.NumericUpDown
    $fpsCustomNumeric.Location = New-Object System.Drawing.Point(245, 28)
    $fpsCustomNumeric.Size = New-Object System.Drawing.Size(80, 28)
    $fpsCustomNumeric.Minimum = 1
    $fpsCustomNumeric.Maximum = 360
    $fpsCustomNumeric.Value = 60
    $fpsCustomNumeric.Visible = $false
    $fpsGroup.Controls.Add($fpsCustomNumeric)

    $fpsLimitStatus = New-Label -Text "" -X 15 -Y 58 -W 620 -H 36
    $fpsLimitStatus.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $fpsGroup.Controls.Add($fpsLimitStatus)
    Update-FpsLimitStatusLabel -StatusLabel $fpsLimitStatus

    $savedFpsLimit = if ($null -ne $config.fpsLimit) { [int]$config.fpsLimit } else { 0 }
    Select-FpsLimitInCombo -Combo $fpsLimitCombo -CustomNumeric $fpsCustomNumeric -SavedLimit $savedFpsLimit
    if ($savedFpsLimit -le 0) {
        Suggest-FpsLimitForFocus -Combo $fpsLimitCombo -CustomNumeric $fpsCustomNumeric `
            -FocusMonitorName $initialFocus -CurrentSavedLimit $savedFpsLimit
    }

    $fpsLimitCombo.Add_SelectedIndexChanged({
        $item = $fpsLimitCombo.SelectedItem
        $isCustom = ($item -and [int]$item.Value -eq $script:FpsCustomComboValue)
        $fpsCustomNumeric.Visible = $isCustom
    })

    $hint1 = New-Label -Text "Desconectados (cinza) serão reativados ao iniciar. Sem marcar Esconder, os demais ativos serão desconectados." `
        -X 235 -Y 258 -W 420 -H 30
    $hint1.ForeColor = $script:Theme.Muted
    $hint1.Font = New-Object System.Drawing.Font("Segoe UI", 8)
    $step1.Controls.Add($hint1)

    foreach ($control in $monitorPanel.Controls) {
        if ($control -is [System.Windows.Forms.RadioButton]) {
            $control.Add_CheckedChanged({
                $sel = Get-GuiSelections -MonitorPanel $monitorPanel
                if ($sel.FocusMonitor) {
                    Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $script:LoadedMonitors -FocusName $sel.FocusMonitor
                    $currentFps = Get-FpsLimitFromControls -Combo $fpsLimitCombo -CustomNumeric $fpsCustomNumeric
                    Suggest-FpsLimitForFocus -Combo $fpsLimitCombo -CustomNumeric $fpsCustomNumeric `
                        -FocusMonitorName $sel.FocusMonitor -CurrentSavedLimit $currentFps
                }
            })
        }
    }

    $step2 = New-Object System.Windows.Forms.Panel
    $step2.Location = New-Object System.Drawing.Point(15, $contentTop)
    $step2.Size = New-Object System.Drawing.Size(680, $contentH)
    $step2.Visible = $false
    $form.Controls.Add($step2)

    $hideGroup = New-Object System.Windows.Forms.GroupBox
    $hideGroup.Text = "Como esconder os outros monitores?"
    $hideGroup.Location = New-Object System.Drawing.Point(5, 10)
    $hideGroup.Size = New-Object System.Drawing.Size(650, 120)
    $step2.Controls.Add($hideGroup)

    $hideStrategyCombo = New-Object System.Windows.Forms.ComboBox
    $hideStrategyCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $hideStrategyCombo.Location = New-Object System.Drawing.Point(15, 30)
    $hideStrategyCombo.Size = New-Object System.Drawing.Size(610, 28)
    [void]$hideStrategyCombo.Items.Add("Desconectar monitores (recomendado)")
    [void]$hideStrategyCombo.Items.Add("Cortinas pretas")
    [void]$hideStrategyCombo.Items.Add("Desligar fisicamente (DDC/CI)")
    $hideStrategyCombo.SelectedIndex = switch ($config.hideStrategy) {
        "blackCurtain" { 1 }
        "turnOff" { 2 }
        default { 0 }
    }
    $hideGroup.Controls.Add($hideStrategyCombo)

    $hideDesc = New-Label -Text "Desconectar: desativa no Windows (rápido). Cortinas: overlay preto. DDC/CI: apaga o painel via hardware." `
        -X 15 -Y 65 -W 610 -H 40
    $hideDesc.ForeColor = $script:Theme.Muted
    $hideDesc.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $hideGroup.Controls.Add($hideDesc)
    $tt = New-Object System.Windows.Forms.ToolTip
    $tt.SetToolTip($hideStrategyCombo, "Desconectar é o mais confiável para a maioria dos setups com 3 monitores.")

    $modeGroup = New-Object System.Windows.Forms.GroupBox
    $modeGroup.Text = "Modo de tela cheia"
    $modeGroup.Location = New-Object System.Drawing.Point(5, 145)
    $modeGroup.Size = New-Object System.Drawing.Size(650, 155)
    $step2.Controls.Add($modeGroup)

    $modeBigPicture = New-Object System.Windows.Forms.RadioButton
    $modeBigPicture.Text = "Steam Big Picture (recomendado)"
    $modeBigPicture.Location = New-Object System.Drawing.Point(15, 30)
    $modeBigPicture.Size = New-Object System.Drawing.Size(610, 24)
    $modeBigPicture.Checked = ($config.fullscreenMode -ne "xboxMode")
    $modeGroup.Controls.Add($modeBigPicture)

    $bpDesc = New-Label -Text "Abre a Steam em modo Big Picture no monitor de foco. Restauração automática ao sair." `
        -X 35 -Y 52 -W 600 -H 18
    $bpDesc.ForeColor = $script:Theme.Muted
    $bpDesc.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $modeGroup.Controls.Add($bpDesc)

    $modeXbox = New-Object System.Windows.Forms.RadioButton
    $modeXbox.Text = "Modo Xbox (Win+F11) — Alpha"
    $modeXbox.Location = New-Object System.Drawing.Point(15, 78)
    $modeXbox.Size = New-Object System.Drawing.Size(610, 24)
    $modeXbox.Checked = ($config.fullscreenMode -eq "xboxMode")
    $modeGroup.Controls.Add($modeXbox)

    $xboxDesc = New-Label -Text "Experimental: envia Win+F11. Sem detecção de fechamento — restaure manualmente pela bandeja ou pelo botão Restaurar agora." `
        -X 35 -Y 100 -W 600 -H 36
    $xboxDesc.ForeColor = $script:Theme.Warning
    $xboxDesc.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $modeGroup.Controls.Add($xboxDesc)

    $step3 = New-Object System.Windows.Forms.Panel
    $step3.Location = New-Object System.Drawing.Point(15, $contentTop)
    $step3.Size = New-Object System.Drawing.Size(680, $contentH)
    $step3.Visible = $false
    $form.Controls.Add($step3)

    $audioGroup = New-Object System.Windows.Forms.GroupBox
    $audioGroup.Text = "Saída de áudio"
    $audioGroup.Location = New-Object System.Drawing.Point(5, 10)
    $audioGroup.Size = New-Object System.Drawing.Size(650, 115)
    $step3.Controls.Add($audioGroup)

    $audioCombo = New-Object System.Windows.Forms.ComboBox
    $audioCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $audioCombo.Location = New-Object System.Drawing.Point(15, 35)
    $audioCombo.Size = New-Object System.Drawing.Size(610, 28)
    $audioCombo.DisplayMember = "Text"
    $audioGroup.Controls.Add($audioCombo)
    Populate-AudioCombo -Combo $audioCombo `
        -SavedAudioId $config.audioDeviceId `
        -SavedAudioName $config.audioDeviceName `
        -SavedAudioAutoSwitch $config.audioAutoSwitch

    if (-not $config.audioDeviceId -and -not $config.audioAutoSwitch) {
        $focusForAudio = $monitors | Where-Object { $_.Name -eq $config.focusMonitor } | Select-Object -First 1
        if ($focusForAudio -and -not $focusForAudio.IsActive) {
            for ($i = 0; $i -lt $audioCombo.Items.Count; $i++) {
                if ($audioCombo.Items[$i].Id -eq $script:AudioOnConnectId) {
                    $audioCombo.SelectedIndex = $i
                    break
                }
            }
        }
    }

    $audioHint = New-Label -Text "Use ""$($script:AudioOnConnectLabel)"" quando a TV ainda não está ligada. Ao ligá-la, o Windows mostra o áudio HDMI como nova saída e o app troca para ela automaticamente." `
        -X 5 -Y 125 -W 650 -H 40
    $audioHint.ForeColor = $script:Theme.Muted
    $audioHint.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $step3.Controls.Add($audioHint)

    if (-not (Test-SoundVolumeViewAvailable)) {
        $audioWarn = New-Label -Text "SoundVolumeView não encontrado — troca de áudio desabilitada." -X 5 -Y 165 -W 650 -H 20
        $audioWarn.ForeColor = $script:Theme.Warning
        $step3.Controls.Add($audioWarn)
    }

    $step4 = New-Object System.Windows.Forms.Panel
    $step4.Location = New-Object System.Drawing.Point(15, $contentTop)
    $step4.Size = New-Object System.Drawing.Size(680, $contentH)
    $step4.Visible = $false
    $form.Controls.Add($step4)

    $reviewTitle = New-Label -Text "Revise antes de iniciar" -X 5 -Y 10 -W 400 -H 24 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold))
    $step4.Controls.Add($reviewTitle)

    $reviewLabel = New-Label -Text "" -X 5 -Y 45 -W 650 -H 140
    $reviewLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $step4.Controls.Add($reviewLabel)

    $wizardData = @{
        MonitorPanel       = $monitorPanel
        HideStrategyCombo  = $hideStrategyCombo
        ModeBigPicture     = $modeBigPicture
        AudioCombo         = $audioCombo
        FpsLimitCombo      = $fpsLimitCombo
        FpsCustomNumeric   = $fpsCustomNumeric
    }
    Update-ReviewPanel -ReviewLabel $reviewLabel -WizardData $wizardData

    $panelActive = New-Object System.Windows.Forms.Panel
    $panelActive.Location = New-Object System.Drawing.Point(15, $contentTop)
    $panelActive.Size = New-Object System.Drawing.Size(680, $contentH)
    $panelActive.Visible = $false
    $form.Controls.Add($panelActive)

    $activeTitle = New-Label -Text "Modo console ativo" -X 5 -Y 20 -W 400 -H 28 `
        -Font (New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold))
    $panelActive.Controls.Add($activeTitle)

    $activeDesc = New-Label -Text "" -X 5 -Y 55 -W 650 -H 70
    $activeDesc.ForeColor = $script:Theme.Muted
    $panelActive.Controls.Add($activeDesc)

    $statusLabel = New-Label -Text "Pronto." -X 20 -Y 520 -W 660 -H 36
    $statusLabel.ForeColor = $script:Theme.Muted
    $form.Controls.Add($statusLabel)

    $btnBack = New-StyledButton -Text "< Voltar" -X 20 -Y 565 -W 100 -H 34
    $btnNext = New-StyledButton -Text "Próximo >" -X 490 -Y 565 -W 100 -H 34
    $btnStart = New-StyledButton -Text "Iniciar modo console" -X 430 -Y 555 -W 250 -H 44 -Primary
    $btnStart.Visible = $false

    $btnSave = New-StyledButton -Text "Salvar" -X 130 -Y 565 -W 90 -H 34
    $btnRefresh = New-StyledButton -Text "Atualizar" -X 230 -Y 565 -W 90 -H 34
    $btnRestore = New-StyledButton -Text "Restaurar agora" -X 330 -Y 565 -W 130 -H 34
    $btnRestore.Enabled = $false

    $form.Controls.AddRange(@($btnBack, $btnNext, $btnStart, $btnSave, $btnRefresh, $btnRestore))

    $stepPanels = @($step1, $step2, $step3, $step4)
    $wizardContext = @{
        Form          = $form
        WizardPanels  = $stepPanels
        NavButtons    = @($btnBack, $btnNext, $btnStart, $btnSave, $btnRefresh)
        ActivePanel   = $panelActive
        RestoreButton = $btnRestore
    }

    $btnBack.Add_Click({
        if ($script:WizardStep -gt 0) {
            Show-WizardStep -Step ($script:WizardStep - 1) -StepPanels $stepPanels `
                -ProgressLabels $progressLabels -BtnBack $btnBack -BtnNext $btnNext `
                -BtnStart $btnStart -ReviewPanel $step4
        }
    })

    $btnNext.Add_Click({
        if ($script:WizardStep -eq 0) {
            $sel = Get-GuiSelections -MonitorPanel $monitorPanel
            if ([string]::IsNullOrWhiteSpace($sel.FocusMonitor)) {
                [System.Windows.Forms.MessageBox]::Show("Selecione o monitor de foco.", "Console Mode") | Out-Null
                return
            }
        }
        if ($script:WizardStep -lt 3) {
            $next = $script:WizardStep + 1
            if ($next -eq 3) { Update-ReviewPanel -ReviewLabel $reviewLabel -WizardData $wizardData }
            Show-WizardStep -Step $next -StepPanels $stepPanels -ProgressLabels $progressLabels `
                -BtnBack $btnBack -BtnNext $btnNext -BtnStart $btnStart -ReviewPanel $step4
        }
    })

    $btnSave.Add_Click({
        Save-FromWizard -Form $form -WizardData $wizardData -StatusLabel $statusLabel | Out-Null
    })

    $btnRefresh.Add_Click({
        Clear-ConsoleDeviceCache
        $monitors = Get-ConsoleMonitors -ForceRefresh
        $script:LoadedMonitors = $monitors
        $cfg = Get-ConsoleConfig
        Build-MonitorPanel -Panel $monitorPanel -Monitors $monitors -SavedFocus $cfg.focusMonitor -SavedHide $cfg.hideMonitors
        Select-FpsLimitInCombo -Combo $fpsLimitCombo -CustomNumeric $fpsCustomNumeric -SavedLimit ([int]$cfg.fpsLimit)
        Populate-AudioCombo -Combo $audioCombo `
            -SavedAudioId $cfg.audioDeviceId `
            -SavedAudioName $cfg.audioDeviceName `
            -SavedAudioAutoSwitch $cfg.audioAutoSwitch
        $sel = Get-GuiSelections -MonitorPanel $monitorPanel
        $focus = if ($sel.FocusMonitor) { $sel.FocusMonitor } else { ($monitors | Select-Object -First 1).Name }
        Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $monitors -FocusName $focus
        Update-FpsLimitStatusLabel -StatusLabel $fpsLimitStatus
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Listas atualizadas." -Color $script:Theme.Success
    })

    $restoreAction = {
        Stop-MonitorWorker
        Request-ConsoleModeExit
        Stop-ConsoleMode
        $script:ConsoleUiLocked = $false
        Set-WizardEnabled @wizardContext -Enabled $true
        Show-FormOnPrimary -Form $form
        Show-StatusMessage -Form $form -StatusLabel $statusLabel `
            -Message "Setup restaurado." -Color $script:Theme.Success -Force
    }

    $startConsoleAction = {
        if (-not (Save-FromWizard -Form $form -WizardData $wizardData -StatusLabel $statusLabel)) { return }

        $data = Get-WizardSelections @wizardData
        try {
            $script:ConsoleUiLocked = $true
            Initialize-TrayIcon -Form $form -OnRestore $restoreAction

            Start-ConsoleMode `
                -FocusMonitor $data.FocusMonitor `
                -HideMonitors $data.HideMonitors `
                -HideStrategy $data.HideStrategy `
                -FullscreenMode $data.FullscreenMode `
                -AudioDeviceId $data.AudioDeviceId `
                -AudioAutoSwitch:$data.AudioAutoSwitch `
                -AudioDeviceHint $data.AudioDeviceHint `
                -FocusMonitorInfo $data.FocusMonitorInfo `
                -FpsLimit $data.FpsLimit

            if ($data.FpsLimit -gt 0 -and -not $Script:ConsoleState.RtssLimitApplied) {
                [System.Windows.Forms.MessageBox]::Show(
                    "O limite de FPS nao foi aplicado (RTSS ausente ou indisponivel).`nO modo console continuara normalmente.",
                    "Console Mode",
                    [System.Windows.Forms.MessageBoxButtons]::OK,
                    [System.Windows.Forms.MessageBoxIcon]::Warning
                ) | Out-Null
            }

            Show-ConsoleActiveView -Form $form -WizardContext $wizardContext
            Set-ActiveConsoleMessaging -ActiveDesc $activeDesc -Form $form -StatusLabel $statusLabel `
                -FullscreenMode $data.FullscreenMode
            if (Test-ConsoleWatchNeeded -FullscreenMode $data.FullscreenMode) {
                Start-MonitorTimer -Form $form -StatusLabel $statusLabel -WizardContext $wizardContext
            }
            if ($data.FullscreenMode -eq "xboxMode") {
                $btnRestore.Enabled = $true
            }
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show("Erro ao iniciar modo console:`n$_", "Console Mode") | Out-Null
            Stop-ConsoleMode
            $script:ConsoleUiLocked = $false
            Set-WizardEnabled @wizardContext -Enabled $true
            Show-FormOnPrimary -Form $form
        }
    }

    $btnStart.Add_Click($startConsoleAction)

    $btnRestore.Add_Click($restoreAction)
    Initialize-TrayIcon -Form $form -OnRestore $restoreAction

    Show-WizardStep -Step 0 -StepPanels $stepPanels -ProgressLabels $progressLabels `
        -BtnBack $btnBack -BtnNext $btnNext -BtnStart $btnStart -ReviewPanel $step4

    Set-DarkThemeOnControl -Control $form

    [void]$form.Show()
    [System.Windows.Forms.Application]::Run($form)
}
