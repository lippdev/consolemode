#Requires -Version 5.1
# Adds the Home package files to an Alpha app directory without registering them.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallRoot
)

$ErrorActionPreference = "Stop"
$root = (Get-Item -LiteralPath $InstallRoot).FullName
$exe = Join-Path $root "ConsoleMode2.exe"
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "ConsoleMode2.exe ausente: $root" }
$homeExe = Join-Path $root "ConsoleMode2Home.exe"
if (-not (Test-Path -LiteralPath $homeExe -PathType Leaf)) { throw "ConsoleMode2Home.exe ausente: $root" }

$mt = (Get-Command mt.exe -ErrorAction SilentlyContinue | Select-Object -First 1).Source
if (-not $mt) {
    $kits = Get-ItemProperty -LiteralPath "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots"
    $mt = Get-ChildItem -LiteralPath (Join-Path $kits.KitsRoot10 "bin") -Directory |
        Where-Object { $_.Name -as [version] } | Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\mt.exe" } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $mt) { throw "mt.exe ausente. Instale o Windows SDK." }

$homeFiles = Join-Path $PSScriptRoot "FseHome"
$identity = Join-Path $homeFiles "ConsoleMode2.manifest"
# The desktop executable stays unpackaged so it can remove the Home registration.
& $mt -nologo -manifest $identity "-outputresource:$homeExe;#1"
if ($LASTEXITCODE -ne 0) { throw "Falha ao incluir a identidade do pacote no ConsoleMode2Home.exe." }

foreach ($file in "AppxManifest.xml", "ConsoleMode2Home.Dev.sccd", "FseAssets") {
    Copy-Item -LiteralPath (Join-Path $homeFiles $file) -Destination $root -Recurse -Force
}
