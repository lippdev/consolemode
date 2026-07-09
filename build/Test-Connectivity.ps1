#Requires -Version 5.1
$conn = 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Connectivity'
Write-Host '=== Connectivity ==='
Get-ChildItem -LiteralPath $conn -ErrorAction SilentlyContinue | Select-Object -First 15 | ForEach-Object {
    Write-Host "`n$($_.PSChildName)"
    Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue | Format-List *
}

# MMT order as returned (no sort)
$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
$csv = Join-Path $env:TEMP 'mmt_order.csv'
& $Script:MmtPath /HideInactiveMonitors 0 /scomma "`"$csv`""
Write-Host "`n=== MMT raw row order ==="
$i = 1
Import-Csv $csv | ForEach-Object {
    Write-Host "  Row $i : $($_.Name) $($_.'Monitor Name') $($_.Frequency)Hz $($_.'Left-Top') active=$($_.Active)"
    $i++
}
Remove-Item $csv -Force
