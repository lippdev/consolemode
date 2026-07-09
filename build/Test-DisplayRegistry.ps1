#Requires -Version 5.1
$paths = @(
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\DisplaySettings',
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\DisplaySettings\ConnectedDevices',
    'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration'
)
foreach ($p in $paths) {
    if (Test-Path -LiteralPath $p) {
        Write-Host "=== $p ==="
        Get-ChildItem -LiteralPath $p -ErrorAction SilentlyContinue | Select-Object -First 10 | ForEach-Object {
            Write-Host "  $($_.PSChildName)"
            $props = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
            if ($props) {
                $props.PSObject.Properties | Where-Object { $_.Name -notmatch '^PS' } | ForEach-Object {
                    Write-Host "    $($_.Name) = $($_.Value)"
                }
            }
        }
    }
}

# MMT csv full columns
$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
$csv = Join-Path $env:TEMP 'mmt_probe.csv'
& $Script:MmtPath /HideInactiveMonitors 0 /scomma "`"$csv`""
if (Test-Path $csv) {
    Write-Host "`n=== MMT CSV columns ==="
    $rows = Import-Csv $csv
    $rows[0].PSObject.Properties.Name
    $rows | Format-Table Name, 'Monitor Name', Primary, Active, Frequency, Resolution, 'Left-Top', 'Short Monitor ID' -AutoSize
    Remove-Item $csv -Force
}
