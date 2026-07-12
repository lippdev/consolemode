#Requires -Version 5.1
# Gera ConsoleMode.exe portatil via ps2exe

param(
    [switch]$SkipInstall,
    [switch]$TestDev,
    [switch]$TestExe
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $root "dist"
$assetsDir = Join-Path $root "assets"
$entryPs1 = Join-Path $root "ConsoleMode.ps1"
$mmt = Join-Path $root "MultiMonitorTool.exe"
$svv = Join-Path $root "SoundVolumeView.exe"
$rtssCli = Join-Path $root "ConsoleMode_Data\tools\rtss-cli.exe"
$outputExe = Join-Path $distDir "ConsoleMode.exe"

function Get-BuildIconPath {
    $path = Join-Path $assetsDir "icon.ico"
    if (Test-Path -LiteralPath $path) {
        return $path
    }
    return $null
}

function Get-BuildEmbedFiles {
    $rtssResolved = Resolve-BuildToolPath -FileName "rtss-cli.exe"
    $embed = @{
        '.\lib\Encoding.ps1'    = (Join-Path $root "lib\Encoding.ps1")
        '.\lib\Paths.ps1'        = (Join-Path $root "lib\Paths.ps1")
        '.\lib\Rtss.ps1'         = (Join-Path $root "lib\Rtss.ps1")
        '.\lib\Engine.ps1'       = (Join-Path $root "lib\Engine.ps1")
        '.\lib\Gui.ps1'          = (Join-Path $root "lib\Gui.ps1")
        '.\MultiMonitorTool.exe' = $mmt
        '.\SoundVolumeView.exe'  = $svv
    }

    if ($rtssResolved) {
        $embed['.\rtss-cli.exe'] = $rtssResolved
    }

    $iconFile = Join-Path $assetsDir "icon.ico"
    if (Test-Path -LiteralPath $iconFile) {
        $embed['.\assets\icon.ico'] = $iconFile
    }

    return $embed
}

function Ensure-Ps2Exe {
    if (Get-Command Invoke-ps2exe -ErrorAction SilentlyContinue) { return $null }

    $localPs2Exe = Join-Path $PSScriptRoot "ps2exe.ps1"
    if (-not (Test-Path -LiteralPath $localPs2Exe)) {
        if ($SkipInstall) {
            throw "Invoke-ps2exe nao encontrado. Rode sem -SkipInstall ou coloque ps2exe.ps1 em build\"
        }
        Write-Host "Baixando ps2exe.ps1..."
        try {
            Invoke-WebRequest -Uri "https://raw.githubusercontent.com/MScholtes/PS2EXE/master/Module/ps2exe.ps1" `
                -OutFile $localPs2Exe -UseBasicParsing
        }
        catch {
            Write-Host "Tentando instalar modulo ps2exe via PSGallery..."
            Install-Module ps2exe -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
            return $null
        }
    }
    return $localPs2Exe
}

function Resolve-BuildToolPath {
    param(
        [Parameter(Mandatory)][string]$FileName
    )

    $candidates = @(
        (Join-Path $root $FileName),
        (Join-Path $root "ConsoleMode_Data\tools\$FileName")
    )

    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }
    return $null
}

function Ensure-RtssCli {
    $resolved = Resolve-BuildToolPath -FileName "rtss-cli.exe"
    if ($resolved) {
        $script:BuildRtssCliPath = $resolved
        return
    }

    Write-Host "rtss-cli ausente. Baixando..."
    & (Join-Path $PSScriptRoot "Get-RtssCli.ps1")

    $script:BuildRtssCliPath = Resolve-BuildToolPath -FileName "rtss-cli.exe"
    if (-not $script:BuildRtssCliPath) {
        Write-Warning "rtss-cli nao encontrado. Limite de FPS RTSS ficara indisponivel no exe ate baixar com build\Get-RtssCli.ps1"
    }
}

function Ensure-NirSoftTools {
    $mmtResolved = Resolve-BuildToolPath -FileName "MultiMonitorTool.exe"
    $svvResolved = Resolve-BuildToolPath -FileName "SoundVolumeView.exe"

    if ($mmtResolved -and $svvResolved) {
        $script:BuildMmtPath = $mmtResolved
        $script:BuildSvvPath = $svvResolved
        return
    }

    Write-Host "Ferramentas NirSoft ausentes. Baixando..."
    & (Join-Path $PSScriptRoot "Get-NirSoftTools.ps1") -TargetDir $root

    $script:BuildMmtPath = Resolve-BuildToolPath -FileName "MultiMonitorTool.exe"
    $script:BuildSvvPath = Resolve-BuildToolPath -FileName "SoundVolumeView.exe"

    if (-not $script:BuildMmtPath -or -not $script:BuildSvvPath) {
        throw @"
Nao foi possivel obter MultiMonitorTool.exe e SoundVolumeView.exe.

Baixe manualmente:
  https://www.nirsoft.net/utils/multi_monitor_tool.html
  https://www.nirsoft.net/utils/sound_volume_view.html

E coloque os .exe na pasta: $root
"@
    }
}

function Test-BuildInputs {
    if (-not (Test-Path -LiteralPath $entryPs1)) {
        throw "ConsoleMode.ps1 nao encontrado em $root"
    }

    Ensure-NirSoftTools
    Ensure-RtssCli
    $script:mmt = $script:BuildMmtPath
    $script:svv = $script:BuildSvvPath

    if (-not (Get-BuildIconPath)) {
        throw "assets\icon.ico nao encontrado. Coloque seu icone em assets\icon.ico"
    }
}

function Ensure-SourceUtf8Bom {
    $utf8Bom = New-Object System.Text.UTF8Encoding $true
    $files = @(
        (Join-Path $root "ConsoleMode.ps1")
    ) + @(Get-ChildItem -LiteralPath (Join-Path $root "lib") -Filter "*.ps1" | ForEach-Object { $_.FullName })

    foreach ($path in $files) {
        $text = [System.IO.File]::ReadAllText($path)
        [System.IO.File]::WriteAllText($path, $text, $utf8Bom)
    }
}

function Build-ConsoleModeExe {
    $ps2exeScript = Ensure-Ps2Exe
    Test-BuildInputs
    Ensure-SourceUtf8Bom

    if (-not (Test-Path -LiteralPath $distDir)) {
        New-Item -ItemType Directory -Path $distDir -Force | Out-Null
    }

    $iconPath = Get-BuildIconPath
    if (-not $iconPath) {
        throw "assets\icon.ico nao encontrado"
    }

    $embedFiles = Get-BuildEmbedFiles

    Write-Host "Compilando $outputExe ..."
    Write-Host "Icone: $iconPath"
    Write-Host "Assets embutidos: $($embedFiles.Keys | Where-Object { $_ -like '.\assets\*' } | Measure-Object | Select-Object -ExpandProperty Count)"

    $ps2exeArgs = @{
        inputFile  = $entryPs1
        outputFile = $outputExe
        iconFile   = $iconPath
        title      = "Console Mode"
        description = "Console Mode - Big Picture / Modo Xbox"
        company    = "Console Mode"
        product    = "Console Mode"
        version    = "1.2.0.0"
        noConsole  = $true
        embedFiles = $embedFiles
        sta        = $true
    }

    if ($ps2exeScript) {
        . $ps2exeScript
        Invoke-ps2exe @ps2exeArgs
    }
    else {
        Invoke-ps2exe @ps2exeArgs
    }

    if (-not (Test-Path -LiteralPath $outputExe)) {
        throw "Falha ao gerar $outputExe"
    }

    $sizeMb = [Math]::Round((Get-Item -LiteralPath $outputExe).Length / 1MB, 2)
    Write-Host "OK: $outputExe ($sizeMb MB)"
}

function Test-DevLaunch {
    Write-Host "Teste dev: carregando modulos..."
    $Script:EntryRoot = $root
    $ScriptRoot = $root
    . (Join-Path $root "lib\Encoding.ps1")
    Initialize-ConsoleEncoding
    . (Join-Path $root "lib\Paths.ps1")
    . (Join-Path $root "lib\Rtss.ps1")
    . (Join-Path $root "lib\Engine.ps1")
    Initialize-ConsoleAppLayout
    if (-not $Script:MmtPath) { throw "MmtPath nao definido" }
    if (-not $Script:ConfigPath) { throw "ConfigPath nao definido" }
    Write-Host "OK: paths dev - Mmt=$Script:MmtPath Data=$Script:AppRoot"
    if (-not (Test-MultiMonitorToolAvailable)) {
        Write-Warning "MultiMonitorTool.exe ausente (necessario para executar/build completo)"
    }
}

function Test-ExeLayout {
    if (-not (Test-Path -LiteralPath $outputExe)) {
        Build-ConsoleModeExe
    }
    Write-Host "Teste exe: $outputExe existe"
    $global:PS2EXEpath = $outputExe
    . (Join-Path $root "lib\Paths.ps1")
    Initialize-ConsoleAppLayout
    if (-not (Test-Path -LiteralPath (Get-ConsoleDataDir))) {
        Write-Host "ConsoleMode_Data sera criado na primeira execucao do exe"
    }
    Write-Host "OK: layout exe preparado"
}

if ($TestDev) {
    Test-DevLaunch
    exit 0
}

if ($TestExe) {
    Test-ExeLayout
    exit 0
}

Build-ConsoleModeExe
