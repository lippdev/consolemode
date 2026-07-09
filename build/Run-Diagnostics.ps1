#Requires -Version 5.1
# Diagnostico completo + grava baseline do setup saudavel
$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
. (Join-Path $root 'lib\Engine.ps1')

$ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$baselineDir = Join-Path $root 'ConsoleMode_Data'
$baselineFile = Join-Path $baselineDir 'diagnostic_baseline.json'

function Get-MonitorSnapshot {
    Get-ConsoleMonitors -ForceRefresh | ForEach-Object {
        [ordered]@{
            Name                 = $_.Name
            WindowsDisplayNumber = $_.WindowsDisplayNumber
            IsActive             = $_.IsActive
            IsPrimary            = $_.IsPrimary
            Frequency            = $_.Frequency
            Resolution           = $_.Resolution
        }
    }
}

Write-Host "=== DIAGNOSTICO $ts ===" -ForegroundColor Cyan

$snapshot = @{
    timestamp   = $ts
    note        = 'Baseline apos destrava manual do usuario (Monitor2 primary esq, Monitor1 dir, Monitor3 desconectado)'
    monitors    = @(Get-MonitorSnapshot)
    backupPath  = $Script:BackupMonitorConfig
    metaPath    = $Script:BackupMonitorMeta
}

Write-Host "`n--- Estado atual ---"
$snapshot.monitors | ForEach-Object {
    Write-Host "  $($_.Name) win#$($_.WindowsDisplayNumber) active=$($_.IsActive) $($_.Frequency)Hz primary=$($_.IsPrimary)"
}

# Teste 1: MMT responde? SetPrimary DISPLAY1
Write-Host "`n--- Teste 1: MMT SetPrimary DISPLAY1 ---"
$beforePrimary = (Get-ConsoleMonitors -ForceRefresh | Where-Object { $_.IsPrimary } | Select-Object -First 1).Name
Set-PrimaryMonitor -MonitorName '\\.\DISPLAY1'
Start-Sleep -Seconds 1
$afterPrimary = (Get-ConsoleMonitors -ForceRefresh | Where-Object { $_.IsPrimary } | Select-Object -First 1).Name
$setPrimaryOk = ($afterPrimary -eq '\\.\DISPLAY1')
Write-Host "  Antes: $beforePrimary | Depois: $afterPrimary | OK=$setPrimaryOk"
$snapshot.setPrimaryTest = @{ before = $beforePrimary; after = $afterPrimary; ok = $setPrimaryOk }

# Teste 2: Restore-SetupNow (restore engine)
Write-Host "`n--- Teste 2: Restore-MonitorBackup ---"
$restoreResult = Restore-MonitorBackup
Write-Host "  Success=$($restoreResult.Success) Attempts=$($restoreResult.Attempts)"
if ($restoreResult.Issues) { $restoreResult.Issues | ForEach-Object { Write-Host "  - $_" } }
$snapshot.restoreAfterBaseline = @{
    success  = $restoreResult.Success
    attempts = $restoreResult.Attempts
    issues   = @($restoreResult.Issues)
    monitors = @(Get-MonitorSnapshot)
}

Write-Host "`n--- Pos-restore ---"
$snapshot.restoreAfterBaseline.monitors | ForEach-Object {
    Write-Host "  $($_.Name) active=$($_.IsActive) $($_.Frequency)Hz primary=$($_.IsPrimary)"
}

# Teste 3: Sim console + restore (ciclo completo)
Write-Host "`n--- Teste 3: Sim console start ---"
$focus = '\\.\DISPLAY3'
$hide = @('\\.\DISPLAY1', '\\.\DISPLAY2')
Enable-Monitors -MonitorNames @($focus) -WindowsEnable
Start-Sleep -Milliseconds 500
Set-PrimaryMonitor -MonitorName $focus
Start-Sleep -Milliseconds 400
Disable-MonitorsWindows -MonitorNames $hide
Start-Sleep -Milliseconds 500
Set-PrimaryMonitor -MonitorName $focus
Start-Sleep -Milliseconds 400

$afterConsole = @(Get-MonitorSnapshot)
Write-Host "  Pos-console:"
$afterConsole | ForEach-Object {
    Write-Host "    $($_.Name) active=$($_.IsActive) primary=$($_.IsPrimary)"
}
$snapshot.afterConsoleSim = $afterConsole

Write-Host "`n--- Teste 4: Restore apos console sim ---"
$restore2 = Restore-MonitorBackup
Write-Host "  Success=$($restore2.Success) Attempts=$($restore2.Attempts)"
if ($restore2.Issues) { $restore2.Issues | ForEach-Object { Write-Host "  - $_" } }
$snapshot.restoreAfterConsoleSim = @{
    success  = $restore2.Success
    attempts = $restore2.Attempts
    issues   = @($restore2.Issues)
    monitors = @(Get-MonitorSnapshot)
}

Write-Host "`n--- Pos-restore (ciclo completo) ---"
$snapshot.restoreAfterConsoleSim.monitors | ForEach-Object {
    Write-Host "  $($_.Name) active=$($_.IsActive) $($_.Frequency)Hz primary=$($_.IsPrimary)"
}

# Salvar baseline
$snapshot | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $baselineFile -Encoding UTF8
Write-Host "`nBaseline gravado em: $baselineFile" -ForegroundColor Green

# Resumo
Write-Host "`n=== RESUMO ===" -ForegroundColor Cyan
Write-Host "MMT SetPrimary: $(if ($setPrimaryOk) { 'OK' } else { 'FALHOU' })"
Write-Host "Restore baseline: $(if ($restoreResult.Success) { 'OK' } else { 'FALHOU' })"
Write-Host "Ciclo console->restore: $(if ($restore2.Success) { 'OK' } else { 'FALHOU' })"

if (-not $restore2.Success) { exit 1 }
exit 0
