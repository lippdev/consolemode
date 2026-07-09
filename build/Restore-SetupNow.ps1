#Requires -Version 5.1
# Restaura o setup: habilita desktops -> move primary -> desconecta TV -> LoadConfig
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { 'C:\MultiMonitorTool' }
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
. (Join-Path $root 'lib\Engine.ps1')

function Show-State($label) {
    Write-Host "`n=== $label ==="
    Get-ConsoleMonitors -ForceRefresh | ForEach-Object {
        Write-Host "  $($_.Name) active=$($_.IsActive) $($_.Frequency)Hz primary=$($_.IsPrimary)"
    }
}

Show-State 'antes'
$result = Restore-MonitorBackup
Show-State 'depois'

if ($result.Success) {
    Write-Host 'OK: setup restaurado.'
    exit 0
}

Write-Host 'FALHA:'
$result.Issues | ForEach-Object { Write-Host "  - $_" }
exit 1
