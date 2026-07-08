#Requires -Version 5.1
$cfgRoot = 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration'
$sets = Get-ChildItem -LiteralPath $cfgRoot | Sort-Object { [int64](Get-ItemProperty $_.PSPath).Timestamp } -Descending | Select-Object -First 5
Write-Host 'Recent configuration sets:'
foreach ($set in $sets) {
    $setId = (Get-ItemProperty -LiteralPath $set.PSPath).SetId
    Write-Host "`n$setId"
    Get-ChildItem -LiteralPath $set.PSPath -ErrorAction SilentlyContinue | ForEach-Object {
        $props = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
        $info = @()
        if ($props.PSObject.Properties.Name -contains 'PrimSurfSize.cx') {
            $info += "surface=$($props.'PrimSurfSize.cx')x$($props.'PrimSurfSize.cy')"
        }
        if ($props.PSObject.Properties.Name -contains 'Attach.ToDesktop') {
            $info += "attach=$($props.'Attach.ToDesktop')"
        }
        if ($props.PSObject.Properties.Name -contains 'Attach.RelativeX') {
            $info += "pos=$($props.'Attach.RelativeX'),$($props.'Attach.RelativeY')"
        }
        Write-Host "  $($_.PSChildName) $($info -join ' ')"
    }
}

# Parse SetId tokens for monitor hardware ids
Write-Host "`nSetId token order (latest):"
$latest = $sets | Select-Object -First 1
$setId = (Get-ItemProperty -LiteralPath $latest.PSPath).SetId
$tokens = $setId -split '\+' | ForEach-Object { $_.Trim() }
$n = 1
foreach ($t in $tokens) {
    Write-Host "  Monitor $n : $t"
    $n++
}

# Map tokens to GDI via MMT
$root = 'C:\MultiMonitorTool'
$Script:EntryRoot = $root
. (Join-Path $root 'lib\Paths.ps1')
Initialize-ConsoleAppLayout
Write-Host "`nMMT mapping:"
Get-ConsoleMonitors | ForEach-Object {
    Write-Host "  $($_.Name) ShortId=$($_.ShortId) MonitorName=$($_.MonitorName) Hz=$($_.Frequency)"
}
