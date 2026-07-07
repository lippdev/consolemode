Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$windowSource = @"
using System;
using System.Runtime.InteropServices;
public class WindowManager {
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}
"@
Add-Type -TypeDefinition $windowSource

# 1. Cria as cortinas pretas nos monitores 1 e 2
$monitores = [System.Windows.Forms.Screen]::AllScreens
$monitor3 = $null
$forms = @()

foreach ($monitor in $monitores) {
    if ($monitor.DeviceName -match "DISPLAY3") {
        $monitor3 = $monitor
        continue
    }

    $form = New-Object System.Windows.Forms.Form
    $form.BackColor = [System.Drawing.Color]::Black
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
    $form.WindowState = [System.Windows.Forms.FormWindowState]::Maximized
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $form.Location = $monitor.Bounds.Location
    $form.Size = $monitor.Bounds.Size
    $form.TopMost = $true
    
    # Se você clicar em uma das telas pretas e apertar ESC, o script fecha na hora (Garantia extra)
    $form.Add_KeyDown({
        if ($_.KeyCode -eq [System.Windows.Forms.Keys]::Escape) {
            $script:sair = $true
        }
    })
    
    $form.Show()
    $forms += $form
}

Start-Sleep -Seconds 3

# 2. Loop de Monitoramento baseado em Foco e Existência Real
$steamMoved = $false
$script:sair = $false
$contadorSemSteam = 0

while (-not $script:sair) {
    # Busca o processo principal da Steam que gerencia a janela do Big Picture
    $steamProcess = Get-Process -Name "steamwebhelper" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "Steam" -or $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    
    if ($steamProcess) {
        $hwnd = $steamProcess.MainWindowHandle
        
        if (-not $steamMoved -and $monitor3 -and $hwnd -ne 0) {
            $x = $monitor3.Bounds.X
            $y = $monitor3.Bounds.Y
            $w = $monitor3.Bounds.Width
            $h = $monitor3.Bounds.Height
            [WindowManager]::SetWindowPos($hwnd, [IntPtr]::Zero, $x, $y, $w, $h, 0x0040)
            $steamMoved = $true
        }
        $contadorSemSteam = 0
    } else {
        # Se por 3 checagens seguidas (6 segundos) a janela real não for detectada, assume que fechou
        if ($steamMoved) {
            $contadorSemSteam++
            if ($contadorSemSteam -ge 3) {
                break
            }
        }
    }
    
    # Processa os eventos das janelas pretas (necessário para o atalho do ESC funcionar)
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Seconds 2
}

# Garante o fechamento de todas as cortinas antes de sair
foreach ($form in $forms) { $form.Close() }