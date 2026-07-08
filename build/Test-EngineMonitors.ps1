$ErrorActionPreference = 'Stop'
$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
. (Join-Path $root 'lib\Engine.ps1')

$errs = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $root 'lib\Engine.ps1'), [ref]$null, [ref]$errs)
if ($errs) { $errs | ForEach-Object { Write-Host $_.ToString() }; exit 1 }
Write-Host 'Engine syntax OK'

$monitors = Get-ConsoleMonitors
Write-Host 'Monitors:'
$monitors | ForEach-Object {
    Write-Host "  Monitor $($_.WindowsDisplayNumber): $($_.Name) [$($_.MonitorName)] $($_.Frequency)Hz primary=$($_.IsPrimary)"
}
