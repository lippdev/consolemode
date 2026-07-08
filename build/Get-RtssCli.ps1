#Requires -Version 5.1
# Baixa rtss-cli para ConsoleMode_Data/tools (dev) ou pasta tools informada

param(
    [string]$TargetDir = $null
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $dataTools = Join-Path $root "ConsoleMode_Data\tools"
    if (-not (Test-Path -LiteralPath $dataTools)) {
        New-Item -ItemType Directory -Path $dataTools -Force | Out-Null
    }
    $TargetDir = $dataTools
}

$dest = Join-Path $TargetDir "rtss-cli.exe"
if (Test-Path -LiteralPath $dest) {
    Write-Host "OK: rtss-cli.exe ja existe em $dest"
    exit 0
}

$url = "https://github.com/xanderfrangos/rtss-cli/releases/download/v1.0.0/rtss-cli.exe"
$tempFile = Join-Path $env:TEMP "rtss-cli-download.exe"

Write-Host "Baixando rtss-cli..."
Invoke-WebRequest -Uri $url -OutFile $tempFile -UseBasicParsing
Copy-Item -LiteralPath $tempFile -Destination $dest -Force
Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
Write-Host "OK: rtss-cli.exe -> $dest"
