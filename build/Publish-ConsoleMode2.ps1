#Requires -Version 5.1
# Gera o Console Mode 2 Alpha (Rust + GPUI), separado da 1.7:
#   dist\v2\ConsoleMode2.exe                       exe de release (icone e versao embutidos)
#   dist\v2\ConsoleMode2Alpha-Setup-x64.exe        instalador por usuario (Inno Setup)
#   dist\v2\ConsoleMode2Alpha-Setup-x64.exe.sha256 SHA-256 do instalador (o que o app confere ao atualizar)
# A versao vem de [workspace.package] version no Cargo.toml da raiz (ex.: 2.0.0-alpha.1).
# Os nomes seguem o que o updater procura nas releases (crates\cm-core\src\update.rs, docs\v2\RELEASE.md).
# Nao publica nada: so gera arquivos em dist\v2.

param(
    [ValidateSet("All", "Exe", "Installer")]
    [string]$Target = "All",
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$distDir = if ($OutputDirectory) { $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory) } else { Join-Path $root "dist\v2" }
$setupName = "ConsoleMode2Alpha-Setup-x64.exe"

if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) { throw "cargo nao encontrado. Instale o Rust (rustup)." }

# Version from the workspace manifest.
$cargoToml = Get-Content -LiteralPath (Join-Path $root "Cargo.toml") -Raw
if ($cargoToml -notmatch '(?ms)\[workspace\.package\].*?^\s*version\s*=\s*"([^"]+)"') { throw "version nao encontrada em Cargo.toml" }
$version = $Matches[1]
if ($version -notmatch '^2\.(\d+)\.(\d+)(-[0-9A-Za-z.\-]+)?$') { throw "Versao invalida para a 2.0: '$version' (use 2.x.y ou 2.x.y-alpha.N)" }
$numericVersion = "2.$($Matches[1]).$($Matches[2]).0"
Write-Host "Console Mode 2 Alpha $version"

# SDL3 is built from source (static): it needs CMake. Visual Studio ships one.
if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    $vs = @("${env:ProgramFiles(x86)}", "$env:ProgramFiles") | ForEach-Object { Join-Path $_ "Microsoft Visual Studio\2022" } | Where-Object { Test-Path -LiteralPath $_ }
    $cmakeBin = $vs | ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory } |
        ForEach-Object { Join-Path $_.FullName "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin" } |
        Where-Object { Test-Path -LiteralPath (Join-Path $_ "cmake.exe") } | Select-Object -First 1
    if (-not $cmakeBin) { throw "CMake nao encontrado. Instale o CMake ou o componente 'CMake tools for Windows' do Visual Studio." }
    $env:PATH = "$cmakeBin;$env:PATH"
    Write-Host "CMake: $cmakeBin"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Push-Location $root
try {
    # The notices table must match the dependencies being shipped.
    & (Join-Path $PSScriptRoot "Generate-Notices2.ps1") -Check

    Write-Host "Compilando (release)..."
    # cargo reports progress on stderr; Windows PowerShell 5.1 would turn that into an error
    $ErrorActionPreference = "Continue"
    & cargo build -p cm-app --release --locked
    $code = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    if ($code -ne 0) { throw "cargo build falhou" }
}
finally { Pop-Location }

$built = Join-Path $root "target\release\ConsoleMode2.exe"
if (-not (Test-Path -LiteralPath $built)) { throw "Build nao gerou $built" }
$exe = Join-Path $distDir "ConsoleMode2.exe"
Copy-Item -LiteralPath $built -Destination $exe -Force
$homeBuilt = Join-Path $root "target\release\ConsoleMode2Home.exe"
Copy-Item -LiteralPath $homeBuilt -Destination $distDir -Force
& (Join-Path $PSScriptRoot "Stage-ConsoleMode2Home.ps1") -InstallRoot $distDir
Write-Host "OK: $exe"

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
        & $iscc "/Q" "/DAppVersion=$version" "/DNumericVersion=$numericVersion" "/DSourceDir=$distDir" "/DOutputDir=$distDir" "/DArch=x64" (Join-Path $PSScriptRoot "ConsoleMode2.iss")
        if ($LASTEXITCODE -ne 0) { throw "ISCC falhou" }
        $setup = Join-Path $distDir $setupName
        $hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
        # sha256sum format ("<hash>  <name>"), LF, no BOM
        [System.IO.File]::WriteAllText("$setup.sha256", "$hash  $setupName`n", [System.Text.UTF8Encoding]::new($false))
        Write-Host "OK: $setup"
        Write-Host "OK: $setup.sha256 ($hash)"
    }
}
