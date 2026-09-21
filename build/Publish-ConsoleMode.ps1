#Requires -Version 5.1
# Publica um unico ConsoleMode.exe portatil (WinUI 3, unpackaged, self-contained, single-file)

param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Runtime = "x64",
    [switch]$SkipTools
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\ConsoleMode\ConsoleMode.csproj"
$rid = "win-$($Runtime.ToLowerInvariant())"
$embedDir = Join-Path $root "artifacts\tools"
$publishDir = Join-Path $root "artifacts\publish\$rid"
$distDir = Join-Path $root "dist"
$distExe = Join-Path $distDir "ConsoleMode.exe"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK nao encontrado. Instale o .NET 8 SDK no Windows."
}

New-Item -ItemType Directory -Path $embedDir -Force | Out-Null
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

if (-not $SkipTools) {
    $getNir = Join-Path $PSScriptRoot "Get-NirSoftTools.ps1"
    $getRtss = Join-Path $PSScriptRoot "Get-RtssCli.ps1"
    if (Test-Path -LiteralPath $getNir) {
        & $getNir -TargetDir $embedDir
    }
    if (Test-Path -LiteralPath $getRtss) {
        & $getRtss -TargetDir $embedDir
    }
}

Get-ChildItem -LiteralPath $publishDir -Force -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Publicando Console Mode ($rid) em um unico .exe..."
dotnet publish $project `
    -c Release `
    -r $rid `
    --self-contained true `
    -p:Platform=$Runtime `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -p:SelfContained=true `
    -p:PublishTrimmed=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou"
}

$publishedExe = Join-Path $publishDir "ConsoleMode.exe"
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Publish nao gerou ConsoleMode.exe em $publishDir"
}

Copy-Item -LiteralPath $publishedExe -Destination $distExe -Force

$extra = Get-ChildItem -LiteralPath $publishDir -File |
    Where-Object { $_.Name -ne "ConsoleMode.exe" -and $_.Extension -notin @(".pdb", ".xml") }
if ($extra) {
    Write-Warning ("Arquivos extras no publish (esperado so o .exe): " + ($extra.Name -join ", "))
}

Write-Host "OK: $distExe"
