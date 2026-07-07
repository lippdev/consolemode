#Requires -Version 5.1
# Console Mode - Interface grafica

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptRoot "lib\Engine.ps1")

$script:MonitorRows = @()
$script:MonitorTimer = $null
$script:TrayIcon = $null

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
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 200, [int]$H = 20)
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Text = $Text
    $lbl.Location = New-Object System.Drawing.Point($X, $Y)
    $lbl.Size = New-Object System.Drawing.Size($W, $H)
    return $lbl
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

    $colFoco = 15
    $colHide = 95
    $colName = 180

    $hdrFoco = New-Label -Text "Foco" -X $colFoco -Y 8 -W 70 -H 18
    $hdrFoco.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
    $Panel.Controls.Add($hdrFoco)

    $hdrHide = New-Label -Text "Esconder" -X $colHide -Y 8 -W 80 -H 18
    $hdrHide.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
    $Panel.Controls.Add($hdrHide)

    $hdrName = New-Label -Text "Monitor" -X $colName -Y 8 -W 420 -H 18
    $hdrName.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
    $Panel.Controls.Add($hdrName)

    $y = 32
    $firstMonitor = $true
    foreach ($monitor in $Monitors) {
        $display = "$($monitor.Name)"
        if ($monitor.MonitorName) { $display += " - $($monitor.MonitorName)" }
        $display += " ($($monitor.Resolution))"
        if ($monitor.IsPrimary -and $monitor.IsActive) { $display += " [Primario atual]" }
        if (-not $monitor.IsActive) { $display += " [Desconectado]" }

        $radio = New-Object System.Windows.Forms.RadioButton
        $radio.Text = ""
        $radio.Tag = $monitor.Name
        $radio.Location = New-Object System.Drawing.Point(($colFoco + 15), $y)
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
        $check.Location = New-Object System.Drawing.Point(($colHide + 20), $y)
        $check.Size = New-Object System.Drawing.Size(20, 20)
        $check.Checked = $SavedHide -contains $monitor.Name
        $check.Enabled = $monitor.IsActive
        $Panel.Controls.Add($check)

        $nameLabel = New-Label -Text $display -X $colName -Y ($y + 2) -W 420 -H 18
        if (-not $monitor.IsActive) {
            $nameLabel.ForeColor = [System.Drawing.Color]::Gray
        }
        $Panel.Controls.Add($nameLabel)

        $y += 28
        $firstMonitor = $false
    }

    $hint = New-Label -Text "Dica: desconectados aparecem em cinza e serao reativados ao iniciar. Sem marcar 'Esconder', os demais ativos serao desconectados." -X $colFoco -Y ($y + 4) -W 600 -H 30
    $hint.ForeColor = [System.Drawing.Color]::DimGray
    $Panel.Controls.Add($hint)
}

function Populate-AudioCombo {
    param(
        [System.Windows.Forms.ComboBox]$Combo,
        [string]$SavedAudioId
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

function Save-FromGui {
    param($Form, $MonitorPanel, $HideStrategyCombo, $ModeBigPicture, $AudioCombo, $StatusLabel)

    $selection = Get-GuiSelections -MonitorPanel $MonitorPanel
    if ([string]::IsNullOrWhiteSpace($selection.FocusMonitor)) {
        [System.Windows.Forms.MessageBox]::Show(
            "Selecione o monitor de foco (TV/console).",
            "Console Mode",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        ) | Out-Null
        return $false
    }

    $hideStrategy = Get-HideStrategyFromCombo -Combo $HideStrategyCombo
    $fullscreenMode = if ($ModeBigPicture.Checked) { "bigPicture" } else { "xboxMode" }
    $audioItem = $AudioCombo.SelectedItem
    $audioId = if ($audioItem) { [string]$audioItem.Id } else { "" }
    $audioName = if ($audioItem) { [string]$audioItem.Text } else { "" }

    $hideMonitors = @($selection.HideMonitors | Where-Object { $_ -ne $selection.FocusMonitor })

    Set-ConsoleConfig `
        -FocusMonitor $selection.FocusMonitor `
        -HideMonitors $hideMonitors `
        -HideStrategy $hideStrategy `
        -FullscreenMode $fullscreenMode `
        -AudioDeviceId $audioId `
        -AudioDeviceName $audioName

    Show-StatusMessage -Form $Form -StatusLabel $StatusLabel -Message "Configuracao salva." -Color ([System.Drawing.Color]::DarkGreen)
    return $true
}

function Set-GuiEnabled {
    param($Form, $Enabled, $StartButton, $RestoreButton, $SaveButton)

    $StartButton.Enabled = $Enabled
    $SaveButton.Enabled = $Enabled
    $RestoreButton.Enabled = -not $Enabled
    foreach ($control in $Form.Controls) {
        if ($control -is [System.Windows.Forms.Panel] -and $control.Name -eq "monitorPanel") {
            foreach ($child in $control.Controls) {
                $child.Enabled = $Enabled
            }
        }
        if ($control -is [System.Windows.Forms.ComboBox] -or $control -is [System.Windows.Forms.RadioButton] -or $control -is [System.Windows.Forms.GroupBox]) {
            if ($control.Name -ne "statusGroup") {
                $control.Enabled = $Enabled
            }
        }
    }
}

function Initialize-TrayIcon {
    param(
        [System.Windows.Forms.Form]$Form,
        [scriptblock]$OnRestore
    )

    if ($script:TrayIcon) { return }

    $script:TrayIcon = New-Object System.Windows.Forms.NotifyIcon
    $script:TrayIcon.Icon = [System.Drawing.SystemIcons]::Application
    $script:TrayIcon.Text = "Console Mode"
    $script:TrayIcon.Visible = $true

    $menu = New-Object System.Windows.Forms.ContextMenuStrip
    $restoreItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $restoreItem.Text = "Restaurar setup"
    $restoreItem.Add_Click($OnRestore)
    [void]$menu.Items.Add($restoreItem)

    $showItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $showItem.Text = "Mostrar janela"
    $showItem.Add_Click({
        Show-FormOnPrimary -Form $Form
    })
    [void]$menu.Items.Add($showItem)

    $exitItem = New-Object System.Windows.Forms.ToolStripMenuItem
    $exitItem.Text = "Sair"
    $exitItem.Add_Click({
        if ($Script:ConsoleState.IsActive) {
            Stop-ConsoleMode
        }
        $script:TrayIcon.Visible = $false
        $script:TrayIcon.Dispose()
        $Form.Close()
    })
    [void]$menu.Items.Add($exitItem)

    $script:TrayIcon.ContextMenuStrip = $menu
    $script:TrayIcon.Add_DoubleClick({
        Show-FormOnPrimary -Form $Form
    })
}

function Start-MonitorTimer {
    param(
        [System.Windows.Forms.Form]$Form,
        [System.Windows.Forms.Label]$StatusLabel,
        [System.Windows.Forms.Button]$StartButton,
        [System.Windows.Forms.Button]$RestoreButton,
        [System.Windows.Forms.Button]$SaveButton
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
            Set-GuiEnabled -Form $Form -Enabled $true -StartButton $StartButton -RestoreButton $RestoreButton -SaveButton $SaveButton
            Show-FormOnPrimary -Form $Form
            Show-StatusMessage -Form $Form -StatusLabel $StatusLabel -Message "Modo console encerrado. Setup restaurado." -Color ([System.Drawing.Color]::DarkGreen)
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

function Show-ConsoleModeGui {
    if (-not (Test-MultiMonitorToolAvailable)) {
        [System.Windows.Forms.MessageBox]::Show(
            "MultiMonitorTool.exe nao encontrado na pasta do app.",
            "Console Mode - Erro",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
        return
    }

    $config = Get-ConsoleConfig
    $monitors = Get-ConsoleMonitors

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "Console Mode - Big Picture / Modo Xbox"
    $form.Size = New-Object System.Drawing.Size(680, 620)
    $form.MinimumSize = New-Object System.Drawing.Size(680, 620)
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
    $form.MaximizeBox = $false
    $form.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    Move-FormToPrimaryScreen -Form $form

    $form.Add_Shown({
        Move-FormToPrimaryScreen -Form $form
    })

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

    $intro = New-Label -Text "Transforme seu PC em console: escolha monitores, modo de tela cheia e audio." -X 15 -Y 15 -W 640 -H 35
    $form.Controls.Add($intro)

    $monitorsGroup = New-Object System.Windows.Forms.GroupBox
    $monitorsGroup.Text = "Monitores"
    $monitorsGroup.Location = New-Object System.Drawing.Point(15, 55)
    $monitorsGroup.Size = New-Object System.Drawing.Size(640, 200)
    $form.Controls.Add($monitorsGroup)

    $monitorPanel = New-Object System.Windows.Forms.Panel
    $monitorPanel.Name = "monitorPanel"
    $monitorPanel.Location = New-Object System.Drawing.Point(10, 20)
    $monitorPanel.Size = New-Object System.Drawing.Size(620, 170)
    $monitorPanel.AutoScroll = $true
    $monitorsGroup.Controls.Add($monitorPanel)

    Build-MonitorPanel -Panel $monitorPanel -Monitors $monitors -SavedFocus $config.focusMonitor -SavedHide $config.hideMonitors

    $hideGroup = New-Object System.Windows.Forms.GroupBox
    $hideGroup.Text = "Estrategia para esconder monitores"
    $hideGroup.Location = New-Object System.Drawing.Point(15, 265)
    $hideGroup.Size = New-Object System.Drawing.Size(310, 80)
    $form.Controls.Add($hideGroup)

    $hideStrategyCombo = New-Object System.Windows.Forms.ComboBox
    $hideStrategyCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $hideStrategyCombo.Location = New-Object System.Drawing.Point(15, 30)
    $hideStrategyCombo.Size = New-Object System.Drawing.Size(280, 25)
    [void]$hideStrategyCombo.Items.Add("Desconectar monitores (recomendado)")
    [void]$hideStrategyCombo.Items.Add("Cortinas pretas")
    [void]$hideStrategyCombo.Items.Add("Desligar fisicamente (DDC/CI)")
    $hideStrategyCombo.SelectedIndex = switch ($config.hideStrategy) {
        "blackCurtain" { 1 }
        "turnOff" { 2 }
        default { 0 }
    }
    $hideGroup.Controls.Add($hideStrategyCombo)

    $modeGroup = New-Object System.Windows.Forms.GroupBox
    $modeGroup.Text = "Modo de tela cheia"
    $modeGroup.Location = New-Object System.Drawing.Point(340, 265)
    $modeGroup.Size = New-Object System.Drawing.Size(315, 80)
    $form.Controls.Add($modeGroup)

    $modeBigPicture = New-Object System.Windows.Forms.RadioButton
    $modeBigPicture.Text = "Steam Big Picture"
    $modeBigPicture.Location = New-Object System.Drawing.Point(15, 28)
    $modeBigPicture.Size = New-Object System.Drawing.Size(140, 22)
    $modeBigPicture.Checked = ($config.fullscreenMode -ne "xboxMode")
    $modeGroup.Controls.Add($modeBigPicture)

    $modeXbox = New-Object System.Windows.Forms.RadioButton
    $modeXbox.Text = "Modo Xbox (Win+F11)"
    $modeXbox.Location = New-Object System.Drawing.Point(160, 28)
    $modeXbox.Size = New-Object System.Drawing.Size(140, 22)
    $modeXbox.Checked = ($config.fullscreenMode -eq "xboxMode")
    $modeGroup.Controls.Add($modeXbox)

    $audioGroup = New-Object System.Windows.Forms.GroupBox
    $audioGroup.Text = "Saida de audio"
    $audioGroup.Location = New-Object System.Drawing.Point(15, 355)
    $audioGroup.Size = New-Object System.Drawing.Size(640, 70)
    $form.Controls.Add($audioGroup)

    $audioCombo = New-Object System.Windows.Forms.ComboBox
    $audioCombo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $audioCombo.Location = New-Object System.Drawing.Point(15, 28)
    $audioCombo.Size = New-Object System.Drawing.Size(610, 25)
    $audioCombo.DisplayMember = "Text"
    $audioGroup.Controls.Add($audioCombo)
    Populate-AudioCombo -Combo $audioCombo -SavedAudioId $config.audioDeviceId

    if (-not (Test-SoundVolumeViewAvailable)) {
        $audioWarning = New-Label -Text "SoundVolumeView.exe nao encontrado - troca de audio desabilitada." -X 15 -Y 430 -W 640 -H 20
        $audioWarning.ForeColor = [System.Drawing.Color]::DarkOrange
        $form.Controls.Add($audioWarning)
    }

    $statusLabel = New-Label -Text "Pronto." -X 15 -Y 455 -W 640 -H 40
    $statusLabel.ForeColor = [System.Drawing.Color]::DimGray
    $form.Controls.Add($statusLabel)

    $btnSave = New-Object System.Windows.Forms.Button
    $btnSave.Text = "Salvar configuracao"
    $btnSave.Location = New-Object System.Drawing.Point(15, 505)
    $btnSave.Size = New-Object System.Drawing.Size(160, 32)
    $btnSave.Add_Click({
        Save-FromGui -Form $form -MonitorPanel $monitorPanel -HideStrategyCombo $hideStrategyCombo `
            -ModeBigPicture $modeBigPicture -AudioCombo $audioCombo -StatusLabel $statusLabel | Out-Null
    })
    $form.Controls.Add($btnSave)

    $btnStart = New-Object System.Windows.Forms.Button
    $btnStart.Text = "Iniciar modo console"
    $btnStart.Location = New-Object System.Drawing.Point(190, 505)
    $btnStart.Size = New-Object System.Drawing.Size(160, 32)
    $btnStart.Add_Click({
        if (-not (Save-FromGui -Form $form -MonitorPanel $monitorPanel -HideStrategyCombo $hideStrategyCombo `
                -ModeBigPicture $modeBigPicture -AudioCombo $audioCombo -StatusLabel $statusLabel)) {
            return
        }

        $selection = Get-GuiSelections -MonitorPanel $monitorPanel
        $hideStrategy = Get-HideStrategyFromCombo -Combo $hideStrategyCombo
        $fullscreenMode = if ($modeBigPicture.Checked) { "bigPicture" } else { "xboxMode" }
        $audioItem = $audioCombo.SelectedItem
        $audioId = if ($audioItem) { [string]$audioItem.Id } else { "" }
        $hideMonitors = @($selection.HideMonitors | Where-Object { $_ -ne $selection.FocusMonitor })
        if ($hideMonitors.Count -eq 0) {
            $allMonitors = Get-ConsoleMonitors
            $hideMonitors = @($allMonitors | Where-Object { $_.Name -ne $selection.FocusMonitor -and $_.IsActive } | ForEach-Object { $_.Name })
        }

        try {
            Start-ConsoleMode `
                -FocusMonitor $selection.FocusMonitor `
                -HideMonitors $hideMonitors `
                -HideStrategy $hideStrategy `
                -FullscreenMode $fullscreenMode `
                -AudioDeviceId $audioId

            Set-GuiEnabled -Form $form -Enabled $false -StartButton $btnStart -RestoreButton $btnRestore -SaveButton $btnSave
            $form.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
            Start-MonitorTimer -Form $form -StatusLabel $statusLabel -StartButton $btnStart -RestoreButton $btnRestore -SaveButton $btnSave
            Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Modo console iniciado..." -Color ([System.Drawing.Color]::DarkBlue)
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Erro ao iniciar modo console:`n$_",
                "Console Mode",
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Error
            ) | Out-Null
            Stop-ConsoleMode
            Set-GuiEnabled -Form $form -Enabled $true -StartButton $btnStart -RestoreButton $btnRestore -SaveButton $btnSave
        }
    })
    $form.Controls.Add($btnStart)

    $btnRestore = New-Object System.Windows.Forms.Button
    $btnRestore.Text = "Restaurar agora"
    $btnRestore.Location = New-Object System.Drawing.Point(365, 505)
    $btnRestore.Size = New-Object System.Drawing.Size(140, 32)
    $btnRestore.Enabled = $false
    $btnRestore.Add_Click({
        if ($script:MonitorTimer) { $script:MonitorTimer.Stop() }
        Request-ConsoleModeExit
        Stop-ConsoleMode
        Set-GuiEnabled -Form $form -Enabled $true -StartButton $btnStart -RestoreButton $btnRestore -SaveButton $btnSave
        Show-FormOnPrimary -Form $form
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Setup restaurado manualmente." -Color ([System.Drawing.Color]::DarkGreen)
    })
    $form.Controls.Add($btnRestore)

    $btnRefresh = New-Object System.Windows.Forms.Button
    $btnRefresh.Text = "Atualizar listas"
    $btnRefresh.Location = New-Object System.Drawing.Point(520, 505)
    $btnRefresh.Size = New-Object System.Drawing.Size(135, 32)
    $btnRefresh.Add_Click({
        $monitors = Get-ConsoleMonitors
        $cfg = Get-ConsoleConfig
        Build-MonitorPanel -Panel $monitorPanel -Monitors $monitors -SavedFocus $cfg.focusMonitor -SavedHide $cfg.hideMonitors
        Populate-AudioCombo -Combo $audioCombo -SavedAudioId $cfg.audioDeviceId
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Listas atualizadas." -Color ([System.Drawing.Color]::DarkGreen)
    })
    $form.Controls.Add($btnRefresh)

    $restoreAction = {
        if ($script:MonitorTimer) { $script:MonitorTimer.Stop() }
        Request-ConsoleModeExit
        Stop-ConsoleMode
        Set-GuiEnabled -Form $form -Enabled $true -StartButton $btnStart -RestoreButton $btnRestore -SaveButton $btnSave
        Show-FormOnPrimary -Form $form
        Show-StatusMessage -Form $form -StatusLabel $statusLabel -Message "Setup restaurado pela bandeja." -Color ([System.Drawing.Color]::DarkGreen)
    }

    Initialize-TrayIcon -Form $form -OnRestore $restoreAction

  [void]$form.ShowDialog()
}

Show-ConsoleModeGui
