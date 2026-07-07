Add-Type -AssemblyName System.Drawing

$dir = Join-Path (Split-Path -Parent $PSScriptRoot) "assets"
if (-not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

foreach ($name in @('icon.ico', 'ConsoleMode.ico')) {
    if (Test-Path -LiteralPath (Join-Path $dir $name)) {
        Write-Output "Icone ja existe: $name"
        exit 0
    }
}

$bmp = New-Object System.Drawing.Bitmap 64, 64
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(62, 207, 160))
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$g.FillRectangle($brush, 8, 18, 48, 28)
$g.FillEllipse($brush, 22, 8, 20, 20)
$g.Dispose()
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$icoPath = Join-Path $dir "ConsoleMode.ico"
$fs = [System.IO.File]::Create($icoPath)
$icon.Save($fs)
$fs.Close()
$bmp.Dispose()
Write-Output "Created $icoPath"
