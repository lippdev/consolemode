#Requires -Version 5.1
# Console Mode - Interface grafica (wizard)

$script:MonitorTimer = $null
$script:TrayIcon = $null
$script:LoadedMonitors = @()
$script:AppIcon = $null
$script:WizardStep = 0
$script:BrandAccent = [System.Drawing.Color]::FromArgb(62, 207, 160)
$script:BrandDark = [System.Drawing.Color]::FromArgb(45, 45, 45)

function Get-ConsoleAssetsDir {
    return Join-Path (Get-ConsoleExeDir) "assets"
}

function Get-ConsoleBrandFilePath {
    param([Parameter(Mandatory)][string[]]$FileNames)

    $dir = Get-ConsoleAssetsDir
    foreach ($name in $FileNames) {
        $path = Join-Path $dir $name
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }
    return $null
}

function Get-ConsoleBrandLogoPath {
    return Get-ConsoleBrandFilePath -FileNames @(
        'Console Mode.png',
        'Console mode logo.png',
        'Console mode png.png'
    )
}

function Get-ConsoleAppIconPath {
    return Get-ConsoleBrandFilePath -FileNames @(
        'icon.ico',
        'ConsoleMode.ico'
    )
}

function Get-ConsoleAppIcon {
    if ($script:AppIcon) { return $script:AppIcon }

    $iconPath = Get-ConsoleAppIconPath
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

function Show-FormOnPrimary {
    param([System.Windows.Forms.Form]$Form)

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
    if ($Font) { $lbl.Font = $Font }
    return $lbl
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
    if ($Primary) {
        $btn.BackColor = $script:BrandAccent
        $btn.ForeColor = [System.Drawing.Color]::White
        $btn.FlatAppearance.BorderSize = 0
        $btn.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
    }
    return $btn
}

function Show-StatusMessage {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        [string]$Message,
        [System.Drawing.Color]$Color
    )
    $StatusLabel.Text = $Message
    $StatusLabel.ForeColor = $Color
    $Form.Refresh()
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
    if ($Monitor.IsPrimary -and $Monitor.IsActive) { $parts += "Primario" }
    if (-not $Monitor.IsActive) { $parts += "Desconectado" }
    return ($parts -join " · ")
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
    $Panel.BackColor = [System.Drawing.Color]::FromArgb(245, 245, 245)

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
            $box.BackColor = $script:BrandAccent
            $box.ForeColor = [System.Drawing.Color]::White
        }
        elseif (-not $m.IsActive) {
            $box.BackColor = [System.Drawing.Color]::LightGray
        }
        else {
            $box.BackColor = [System.Drawing.Color]::FromArgb(70, 70, 70)
            $box.ForeColor = [System.Drawing.Color]::White
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
            $nameLabel.ForeColor = [System.Drawing.Color]::Gray
        }
        $Panel.Controls.Add($nameLabel)

        $subLabel = New-Label -Text $secondary -X ($colName + 2) -Y ($y + 16) -W 420 -H 14
        $subLabel.ForeColor = [System.Drawing.Color]::DimGray
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
        [string]$SavedAudioName = ""
    )

    $Combo.Items.Clear()
    [void]$Combo.Items.Add([PSCustomObject]@{ Text = "(Nao mudar)"; Id = "" })

    if (-not (Test-SoundVolumeViewAvailable)) {
        $Combo.SelectedIndex = 0
        return
    }

    $devices = Get-ConsoleAudioDevices
    foreach ($device in $devices) {
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = $device.Name; Id = $device.FriendlyId })
    }

    if ($SavedAudioId -and -not ($devices | Where-Object { $_.FriendlyId -eq $SavedAudioId })) {
        $label = if ($SavedAudioName) { $SavedAudioName } else { $SavedAudioId }
        if ($label -notmatch '\[Desabilitado\]') {
            $label += " [Desabilitado]"
        }
        [void]$Combo.Items.Add([PSCustomObject]@{ Text = $label; Id = $SavedAudioId })
    }

    $selectedIndex = 0
    for ($i = 0; $i -lt $Combo.Items.Count; $i++) {
        if ($Combo.Items[$i].Id -eq $SavedAudioId) {
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
    if ($Mode -eq "xboxMode") { return "Modo Xbox (Win+F11)" }
    return "Steam Big Picture"
}

function Get-WizardSelections {
    param(
        $MonitorPanel,
        $HideStrategyCombo,
        $ModeBigPicture,
        $AudioCombo
    )

    $selection = Get-GuiSelections -MonitorPanel $MonitorPanel
    $hideStrategy = Get-HideStrategyFromCombo -Combo $HideStrategyCombo
    $fullscreenMode = if ($ModeBigPicture.Checked) { "bigPicture" } else { "xboxMode" }
    $audioItem = $AudioCombo.SelectedItem
    $audioId = if ($audioItem) { [string]$audioItem.Id } else { "" }
    $audioName = if ($audioItem) { [string]$audioItem.Text } else { "" }

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

    return @{
        FocusMonitor   = $selection.FocusMonitor
        FocusLabel     = $focusLabel
        HideMonitors   = $hideMonitors
        HideLabels     = $hideLabels
        HideStrategy   = $hideStrategy
        FullscreenMode = $fullscreenMode
        AudioDeviceId  = $audioId
        AudioDeviceName = $audioName
        FocusMonitorInfo = $focusInfo
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
        -AudioDeviceName $data.AudioDeviceName

    Show-StatusMessage -Form $Form -StatusLabel $StatusLabel -Message "Configuracao salva." -Color ([System.Drawing.Color]::DarkGreen)
    return $true
}

function Update-ReviewPanel {
    param(
        [System.Windows.Forms.Label]$ReviewLabel,
        $WizardData
    )

    $data = Get-WizardSelections @WizardData
    $hideText = if ($data.HideLabels.Count -gt 0) { $data.HideLabels -join ", " } else { "(nenhum)" }
    $audioText = if ($data.AudioDeviceName) { $data.AudioDeviceName } else { "(nao mudar)" }

    $ReviewLabel.Text = @(
        "Monitor de foco: $($data.FocusLabel)"
        "Esconder: $hideText"
        "Estrategia: $(Get-HideStrategyLabel -Strategy $data.HideStrategy)"
        "Modo: $(Get-FullscreenModeLabel -Mode $data.FullscreenMode)"
        "Audio: $audioText"
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
        $script:TrayIcon.Visible = $false
        $script:TrayIcon.Dispose()
        $Form.Close()
    })
    [void]$menu.Items.Add($exitItem)

    $script:TrayIcon.ContextMenuStrip = $menu
    $script:TrayIcon.Add_DoubleClick({ Show-FormOnPrimary -Form $Form })
}

function Start-MonitorTimer {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        $WizardContext
    )

    if ($script:MonitorTimer) {
        $script:MonitorTimer.Stop()
        $script:MonitorTimer.Dispose()
    }

    $script:MonitorTimer = New-Object System.Windows.Forms.Timer
    $script:MonitorTimer.Interval = 1000
    $script:MonitorTimer.Add_Tick({
        $result = Update-ConsoleMonitorLoop
        if ($result -eq "exit") {
            $script:MonitorTimer.Stop()
            Stop-ConsoleMode
            Set-WizardEnabled @WizardContext -Enabled $true
            Show-FormOnPrimary -Form $Form
            Show-StatusMessage -Form $Form -StatusLabel $StatusLabel `
                -Message "Modo console encerrado. Setup restaurado." -Color ([System.Drawing.Color]::DarkGreen)
        }
        elseif ($result -eq "running") {
            $statusMsg = if ($Script:ConsoleState.FullscreenMode -eq "xboxMode") {
                "Modo console ativo (Xbox). Ao sair, o setup sera restaurado automaticamente."
            } else {
                "Modo console ativo. Pressione ESC nas cortinas pretas ou use Restaurar."
            }
            Show-StatusMessage -Form $Form -StatusLabel $StatusLabel -Message $statusMsg -Color ([System.Drawing.Color]::DarkBlue)
        }
    })
    $script:MonitorTimer.Start()
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
            $ProgressLabels[$i].ForeColor = $script:BrandAccent
        }
        else {
            $ProgressLabels[$i].Font = New-Object System.Drawing.Font("Segoe UI", 9)
            $ProgressLabels[$i].ForeColor = [System.Drawing.Color]::DimGray
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
            "MultiMonitorTool.exe nao encontrado.`n`nEm desenvolvimento: coloque na pasta do projeto.`nNo executavel: sera extraido em ConsoleMode_Data\tools na primeira execucao.",
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
    $form.BackColor = [System.Drawing.Color]::White
    $form.Icon = Get-ConsoleAppIcon
    Move-FormToPrimaryScreen -Form $form
    $form.Add_Shown({ Move-FormToPrimaryScreen -Form $form })

    $form.Add_FormClosing({
        param($sender, $e)
        if ($Script:ConsoleState.IsActive) {
            $answer = [System.Windows.Forms.MessageBox]::Show(
                "O modo console esta ativo. Deseja restaurar o setup antes de sair?",
                "Console Mode",
                [System.Windows.Forms.MessageBoxButtons]::YesNoCancel,
                [System.Windows.Forms.MessageBoxIcon]::Question
            )
            if ($answer -eq [System.Windows.Forms.DialogResult]::Cancel) {
                $e.Cancel = $true
                return
            }
            if ($answer -eq [System.Windows.Forms.DialogResult]::Yes) {
                if ($script:MonitorTimer) { $script:MonitorTimer.Stop() }
                Stop-ConsoleMode
            }
        }
        if ($script:TrayIcon) {
            $script:TrayIcon.Visible = $false
            $script:TrayIcon.Dispose()
        }
    })

    $headerY = 12
    $progressY = 78

    $logoPath = Get-ConsoleBrandLogoPath
    if ($logoPath) {
        $logoBox = New-Object System.Windows.Forms.PictureBox
        $logoBox.Location = New-Object System.Drawing.Point(20, $headerY)
        $logoBox.Size = New-Object System.Drawing.Size(300, 54)
        $logoBox.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
        $logoBox.BackColor = [System.Drawing.Color]::White
        try {
            $logoBox.Image = [System.Drawing.Image]::FromFile($logoPath)
        }
        catch { }
        $form.Controls.Add($logoBox)
    }
    else {
        $titleLbl = New-Label -Text "Console Mode" -X 20 -Y $headerY -W 300 -H 28 `
            -Font (New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold))
        $titleLbl.ForeColor = $script:BrandDark
        $form.Controls.Add($titleLbl)
    }

    $subtitleLbl = New-Label -Text "Transforme seu PC em console de jogos em poucos passos." -X 20 -Y 48 -W 660 -H 20
    $subtitleLbl.ForeColor = [System.Drawing.Color]::DimGray
    $form.Controls.Add($subtitleLbl)
    $stepTitles = @("1. Monitores", "2. Modo", "3. Audio", "Iniciar")
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
    $linePanel.BackColor = [System.Drawing.Color]::LightGray
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
    $monitorPanel.Size = New-Object System.Drawing.Size(650, 250)
    $monitorPanel.AutoScroll = $true
    $step1.Controls.Add($monitorPanel)

    Build-MonitorPanel -Panel $monitorPanel -Monitors $monitors -SavedFocus $config.focusMonitor -SavedHide $config.hideMonitors
    $initialFocus = if ($config.focusMonitor) { $config.focusMonitor } else { ($monitors | Select-Object -First 1).Name }
    Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $monitors -FocusName $initialFocus

    $btnHideOthers = New-StyledButton -Text "Esconder todos exceto o foco" -X 5 -Y 340 -W 220 -H 30
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

    $hint1 = New-Label -Text "Desconectados (cinza) serao reativados ao iniciar. Sem marcar Esconder, os demais ativos serao desconectados." `
        -X 235 -Y 346 -W 420 -H 30
    $hint1.ForeColor = [System.Drawing.Color]::DimGray
    $hint1.Font = New-Object System.Drawing.Font("Segoe UI", 8)
    $step1.Controls.Add($hint1)

    foreach ($control in $monitorPanel.Controls) {
        if ($control -is [System.Windows.Forms.RadioButton]) {
            $control.Add_CheckedChanged({
                $sel = Get-GuiSelections -MonitorPanel $monitorPanel
                if ($sel.FocusMonitor) {
                    Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $script:LoadedMonitors -FocusName $sel.FocusMonitor
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

    $hideDesc = New-Label -Text "Desconectar: desativa no Windows (rapido). Cortinas: overlay preto. DDC/CI: apaga o painel via hardware." `
        -X 15 -Y 65 -W 610 -H 40
    $hideDesc.ForeColor = [System.Drawing.Color]::DimGray
    $hideDesc.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $hideGroup.Controls.Add($hideDesc)
    $tt = New-Object System.Windows.Forms.ToolTip
    $tt.SetToolTip($hideStrategyCombo, "Desconectar e o mais confiavel para a maioria dos setups com 3 monitores.")

    $modeGroup = New-Object System.Windows.Forms.GroupBox
    $modeGroup.Text = "Modo de tela cheia"
    $modeGroup.Location = New-Object System.Drawing.Point(5, 145)
    $modeGroup.Size = New-Object System.Drawing.Size(650, 120)
    $step2.Controls.Add($modeGroup)

    $modeBigPicture = New-Object System.Windows.Forms.RadioButton
    $modeBigPicture.Text = "Steam Big Picture"
    $modeBigPicture.Location = New-Object System.Drawing.Point(15, 35)
    $modeBigPicture.Size = New-Object System.Drawing.Size(600, 24)
    $modeBigPicture.Checked = ($config.fullscreenMode -ne "xboxMode")
    $modeGroup.Controls.Add($modeBigPicture)

    $bpDesc = New-Label -Text "Abre a Steam em modo Big Picture no monitor de foco." -X 35 -Y 58 -W 580 -H 18
    $bpDesc.ForeColor = [System.Drawing.Color]::DimGray
    $bpDesc.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $modeGroup.Controls.Add($bpDesc)

    $modeXbox = New-Object System.Windows.Forms.RadioButton
    $modeXbox.Text = "Modo Xbox (Win+F11)"
    $modeXbox.Location = New-Object System.Drawing.Point(15, 82)
    $modeXbox.Size = New-Object System.Drawing.Size(600, 24)
    $modeXbox.Checked = ($config.fullscreenMode -eq "xboxMode")
    $modeGroup.Controls.Add($modeXbox)

    $step3 = New-Object System.Windows.Forms.Panel
    $step3.Location = New-Object System.Drawing.Point(15, $contentTop)
    $step3.Size = New-Object System.Drawing.Size(680, $contentH)
    $step3.Visible = $false
    $form.Controls.Add($step3)

    $audioGroup = New-Object System.Windows.Forms.GroupBox
    $audioGroup.Text = "Saida de audio"
    $audioGroup.Location = New-Object System.Drawing.Point(5, 10)
    $audioGroup.Size = New-Object System.Drawing.Size(650, 100)
    $step3.Controls.Add($audioGroup)

    $audioCombo = New-Object System.Windows.Forms.ComboBox
    $audioCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $audioCombo.Location = New-Object System.Drawing.Point(15, 35)
    $audioCombo.Size = New-Object System.Drawing.Size(610, 28)
    $audioCombo.DisplayMember = "Text"
    $audioGroup.Controls.Add($audioCombo)
    Populate-AudioCombo -Combo $audioCombo -SavedAudioId $config.audioDeviceId -SavedAudioName $config.audioDeviceName

    $audioHint = New-Label -Text "Dispositivos desabilitados (ex.: audio da TV desconectada) aparecem com [Desabilitado] e serao ativados ao iniciar." `
        -X 5 -Y 125 -W 650 -H 36
    $audioHint.ForeColor = [System.Drawing.Color]::DimGray
    $audioHint.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
    $step3.Controls.Add($audioHint)

    if (-not (Test-SoundVolumeViewAvailable)) {
        $audioWarn = New-Label -Text "SoundVolumeView nao encontrado - troca de audio desabilitada." -X 5 -Y 165 -W 650 -H 20
        $audioWarn.ForeColor = [System.Drawing.Color]::DarkOrange
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

    $reviewLabel = New-Label -Text "" -X 5 -Y 45 -W 650 -H 120
    $reviewLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $step4.Controls.Add($reviewLabel)

    $wizardData = @{
        MonitorPanel       = $monitorPanel
        HideStrategyCombo  = $hideStrategyCombo
        ModeBigPicture     = $modeBigPicture
        AudioCombo         = $audioCombo
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

    $activeDesc = New-Label -Text "O app esta na bandeja. Ao sair do Big Picture ou Modo Xbox, tudo sera restaurado automaticamente." `
        -X 5 -Y 55 -W 650 -H 50
    $activeDesc.ForeColor = [System.Drawing.Color]::DimGray
    $panelActive.Controls.Add($activeDesc)

    $statusLabel = New-Label -Text "Pronto." -X 20 -Y 520 -W 660 -H 36
    $statusLabel.ForeColor = [System.Drawing.Color]::DimGray
    $form.Controls.Add($statusLabel)

    $btnBack = New-StyledButton -Text "< Voltar" -X 20 -Y 565 -W 100 -H 34
    $btnNext = New-StyledButton -Text "Proximo >" -X 490 -Y 565 -W 100 -H 34
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
        Populate-AudioCombo -Combo $audioCombo -SavedAudioId $cfg.audioDeviceId -SavedAudioName $cfg.audioDeviceName
        $sel = Get-GuiSelections -MonitorPanel $monitorPanel
        $focus = if ($sel.FocusMonitor) { $sel.FocusMonitor } else { ($monitors | Select-Object -First 1).Name }
        Build-MonitorLayoutDiagram -Panel $diagramPanel -Monitors $monitors -FocusName $focus
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Listas atualizadas." -Color ([System.Drawing.Color]::DarkGreen)
    })

    $startConsoleAction = {
        if (-not (Save-FromWizard -Form $form -WizardData $wizardData -StatusLabel $statusLabel)) { return }

        $data = Get-WizardSelections @wizardData
        try {
            Set-WizardEnabled @wizardContext -Enabled $false
            $form.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
            [System.Windows.Forms.Application]::DoEvents()

            Start-ConsoleMode `
                -FocusMonitor $data.FocusMonitor `
                -HideMonitors $data.HideMonitors `
                -HideStrategy $data.HideStrategy `
                -FullscreenMode $data.FullscreenMode `
                -AudioDeviceId $data.AudioDeviceId `
                -FocusMonitorInfo $data.FocusMonitorInfo

            Start-MonitorTimer -Form $form -StatusLabel $statusLabel -WizardContext $wizardContext
            Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Modo console iniciado..." -Color ([System.Drawing.Color]::DarkBlue)
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show("Erro ao iniciar modo console:`n$_", "Console Mode") | Out-Null
            Stop-ConsoleMode
            Set-WizardEnabled @wizardContext -Enabled $true
        }
    }

    $btnStart.Add_Click($startConsoleAction)

    $restoreAction = {
        if ($script:MonitorTimer) { $script:MonitorTimer.Stop() }
        Request-ConsoleModeExit
        Stop-ConsoleMode
        Set-WizardEnabled @wizardContext -Enabled $true
        Show-FormOnPrimary -Form $form
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Setup restaurado." -Color ([System.Drawing.Color]::DarkGreen)
    }

    $btnRestore.Add_Click($restoreAction)
    Initialize-TrayIcon -Form $form -OnRestore $restoreAction

    Show-WizardStep -Step 0 -StepPanels $stepPanels -ProgressLabels $progressLabels `
        -BtnBack $btnBack -BtnNext $btnNext -BtnStart $btnStart -ReviewPanel $step4

    [void]$form.ShowDialog()
}
