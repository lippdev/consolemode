#Requires -Version 5.1
function Search-Keys($root, $depth, $pattern) {
    if ($depth -le 0) { return }
    Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.PSChildName -match $pattern) { $_.PSPath }
        if ($_.PSIsContainer) { Search-Keys $_.PSPath ($depth - 1) $pattern }
    }
}

$hits = @()
$hits += Search-Keys 'HKCU:\Software\Microsoft\Windows\CurrentVersion' 4 'Display|Monitor|Connected'
$hits += Search-Keys 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' 3 'Display|Connectivity'
$hits | Select-Object -Unique | ForEach-Object { Write-Host $_ }

# Try EnumDisplayDevices order
Add-Type @"
using System; using System.Runtime.InteropServices; using System.Text;
public static class Ed {
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] public struct DISPLAY_DEVICE {
    public int cb; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceString;
    public int StateFlags; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string DeviceKey;
  }
  [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
}
"@
Write-Host "`nEnumDisplayDevices (all adapters):"
$n = 0
for ($i = 0; $i -lt 20; $i++) {
    $dev = New-Object Ed+DISPLAY_DEVICE
    $dev.cb = [Runtime.InteropServices.Marshal]::SizeOf([type]'Ed+DISPLAY_DEVICE')
    if (-not [Ed]::EnumDisplayDevices($null, $i, [ref]$dev, 0)) { break }
    if ($dev.StateFlags -band 0x1) {
        $n++
        Write-Host "  #$n $($dev.DeviceName) $($dev.DeviceString)"
    }
}
