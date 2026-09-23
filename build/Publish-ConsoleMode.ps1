#Requires -Version 5.1
# Publica o Console Mode (WinUI 3, unpackaged, self-contained):
#   dist\ConsoleMode-Portable-<arch>.exe  um unico .exe portatil (dados ao lado do exe)
#   dist\ConsoleMode-Setup-<arch>.exe     instalador por usuario (Inno Setup; dados em %LOCALAPPDATA%)
# Os nomes seguem o que o UpdateService procura nas releases do GitHub.

param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Runtime = "x64",
    [ValidateSet("All", "Portable", "Installer")]
    [string]$Target = "All",
    # Ex.: 1.3.0 ou 1.3.0-beta.2 (sem o "v"). Vazio = versao do .csproj.
    [string]$Version = "",
    [switch]$SkipTools
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\ConsoleMode\ConsoleMode.csproj"
$arch = $Runtime.ToLowerInvariant()
$rid = "win-$arch"
$embedDir = Join-Path $root "artifacts\tools"
$singleDir = Join-Path $root "artifacts\publish\$rid-single"
$appDir = Join-Path $root "artifacts\publish\$rid-app"
$distDir = Join-Path $root "dist"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK nao encontrado. Instale o .NET 8 SDK no Windows."
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
$Version = $Version.TrimStart("v")
if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z.\-]+)?$') {
    throw "Versao invalida: '$Version' (use 1.2.3 ou 1.2.3-beta.1)"
}
$numericVersion = "$($Matches[1]).$($Matches[2]).$($Matches[3]).0"
Write-Host "Versao: $Version"

New-Item -ItemType Directory -Path $embedDir, $distDir -Force | Out-Null

if (-not $SkipTools) {
    & (Join-Path $PSScriptRoot "Get-NirSoftTools.ps1") -TargetDir $embedDir
    & (Join-Path $PSScriptRoot "Get-RtssCli.ps1") -TargetDir $embedDir
}

function Invoke-Publish([string]$OutDir, [bool]$SingleFile) {
    if (Test-Path -LiteralPath $OutDir) { Remove-Item -LiteralPath $OutDir -Recurse -Force }
    $pubArgs = @(
        "publish", $project,
        "-c", "Release",
        "-r", $rid,
        "--self-contained", "true",
        "-p:Platform=$Runtime",
        "-p:PublishSingleFile=$($SingleFile.ToString().ToLowerInvariant())",
        "-p:WindowsPackageType=None",
        "-p:WindowsAppSDKSelfContained=true",
        "-p:PublishTrimmed=false",
        "-p:Version=$Version",
        "-p:InformationalVersion=$Version",
        "-p:AssemblyVersion=$numericVersion",
        "-p:FileVersion=$numericVersion",
        "-o", $OutDir
    )
    if ($SingleFile) { $pubArgs += "-p:IncludeAllContentForSelfExtract=true" }
    & dotnet @pubArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou ($OutDir)" }
    if (-not (Test-Path -LiteralPath (Join-Path $OutDir "ConsoleMode.exe"))) {
        throw "Publish nao gerou ConsoleMode.exe em $OutDir"
    }
}

if ($Target -in "All", "Portable") {
    Write-Host "Publicando versao portatil (um unico .exe)..."
    Invoke-Publish $singleDir $true
    $portable = Join-Path $distDir "ConsoleMode-Portable-$arch.exe"
    Copy-Item -LiteralPath (Join-Path $singleDir "ConsoleMode.exe") -Destination $portable -Force
    Write-Host "OK: $portable"
}

if ($Target -in "All", "Installer") {
    $iscc = @(
        (Get-Command iscc -ErrorAction SilentlyContinue).Source,
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1

    if (-not $iscc) {
        $msg = "Inno Setup 6 (ISCC.exe) nao encontrado; instalador nao gerado. Instale com: winget install JRSoftware.InnoSetup"
        if ($Target -eq "Installer") { throw $msg }
        Write-Warning $msg
    }
    else {
        Write-Host "Publicando versao para o instalador (pasta)..."
        Invoke-Publish $appDir $false
        & $iscc "/Q" "/DAppVersion=$Version" "/DSourceDir=$appDir" "/DOutputDir=$distDir" "/DArch=$arch" (Join-Path $PSScriptRoot "ConsoleMode.iss")
        if ($LASTEXITCODE -ne 0) { throw "ISCC falhou" }
        Write-Host "OK: $(Join-Path $distDir "ConsoleMode-Setup-$arch.exe")"
    }
}
