#Requires -Version 5.1
# Console Mode - Resolucao de paths (dev, ps2exe e dados portateis)

$Script:ConsolePaths = @{
    ExeDir   = $null
    DataDir  = $null
    ToolsDir = $null
    DevMode  = $false
}

function Get-ConsoleExeDir {
    if ($Script:ConsolePaths.ExeDir) {
        return $Script:ConsolePaths.ExeDir
    }

    $candidates = [System.Collections.ArrayList]@()
    if ($Script:EntryRoot) { [void]$candidates.Add($Script:EntryRoot.ToString().TrimEnd('\', '/')) }
    if ($ScriptRoot) { [void]$candidates.Add($ScriptRoot.ToString().TrimEnd('\', '/')) }

    $baseDir = [AppDomain]::CurrentDomain.BaseDirectory
    if ($baseDir) { [void]$candidates.Add($baseDir.TrimEnd('\', '/')) }

    if ($global:PS2EXEpath) {
        [void]$candidates.Add((Split-Path -Parent $global:PS2EXEpath).TrimEnd('\', '/'))
    }

    if ($PSScriptRoot -and ((Split-Path -Leaf $PSScriptRoot) -ne 'lib')) {
        [void]$candidates.Add($PSScriptRoot.TrimEnd('\', '/'))
    }

    $seen = @{}
    foreach ($dir in $candidates) {
        if ([string]::IsNullOrWhiteSpace($dir)) { continue }
        if ($seen.ContainsKey($dir)) { continue }
        $seen[$dir] = $true

        $engineFile = Join-Path $dir "lib\Engine.ps1"
        if (Test-Path -LiteralPath $engineFile) {
            $Script:ConsolePaths.ExeDir = $dir
            return $Script:ConsolePaths.ExeDir
        }
    }

    if ($baseDir) {
        $Script:ConsolePaths.ExeDir = $baseDir.TrimEnd('\', '/')
        return $Script:ConsolePaths.ExeDir
    }

    $Script:ConsolePaths.ExeDir = (Get-Location).Path
    return $Script:ConsolePaths.ExeDir
}

function Get-ConsoleDataDir {
    if ($Script:ConsolePaths.DataDir) {
        return $Script:ConsolePaths.DataDir
    }

    $Script:ConsolePaths.DataDir = Join-Path (Get-ConsoleExeDir) "ConsoleMode_Data"
    return $Script:ConsolePaths.DataDir
}

function Get-ConsoleToolsDir {
    if ($Script:ConsolePaths.ToolsDir) {
        return $Script:ConsolePaths.ToolsDir
    }

    $Script:ConsolePaths.ToolsDir = Join-Path (Get-ConsoleDataDir) "tools"
    return $Script:ConsolePaths.ToolsDir
}

function Test-ConsoleDevMode {
    if ($global:PS2EXEpath) { return $false }

    $exeDir = Get-ConsoleExeDir
    $ps1Entry = Join-Path $exeDir "ConsoleMode.ps1"
    return Test-Path -LiteralPath $ps1Entry
}

function Move-ConsoleToolFile {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) { return $false }

    $destDir = Split-Path -Parent $DestPath
    if (-not (Test-Path -LiteralPath $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    if (Test-Path -LiteralPath $DestPath) {
        try {
            Remove-Item -LiteralPath $DestPath -Force -ErrorAction Stop
        }
        catch {
            return $true
        }
    }

    try {
        Move-Item -LiteralPath $SourcePath -Destination $DestPath -Force -ErrorAction Stop
        return $true
    }
    catch {
        try {
            Copy-Item -LiteralPath $SourcePath -Destination $DestPath -Force -ErrorAction Stop
            return $true
        }
        catch {
            return $false
        }
    }
}

function Initialize-ConsoleAppLayout {
    $exeDir = Get-ConsoleExeDir
    $dataDir = Get-ConsoleDataDir
    $toolsDir = Get-ConsoleToolsDir
    $devMode = Test-ConsoleDevMode
    $Script:ConsolePaths.DevMode = $devMode

    if (-not (Test-Path -LiteralPath $dataDir)) {
        New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    }
    if (-not (Test-Path -LiteralPath $toolsDir)) {
        New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
    }

    $toolNames = @("MultiMonitorTool.exe", "SoundVolumeView.exe")
    foreach ($toolName in $toolNames) {
        $destPath = Join-Path $toolsDir $toolName
        if (Test-Path -LiteralPath $destPath) { continue }

        $besideExe = Join-Path $exeDir $toolName
        if (Test-Path -LiteralPath $besideExe) {
            Move-ConsoleToolFile -SourcePath $besideExe -DestPath $destPath | Out-Null
            continue
        }

        if ($devMode) {
            $rootPath = Join-Path $exeDir $toolName
            if (Test-Path -LiteralPath $rootPath) {
                continue
            }
        }
    }

    $legacyConfig = Join-Path $exeDir "config.json"
    $dataConfig = Join-Path $dataDir "config.json"
    if ((Test-Path -LiteralPath $legacyConfig) -and -not (Test-Path -LiteralPath $dataConfig)) {
        Move-Item -LiteralPath $legacyConfig -Destination $dataConfig -Force -ErrorAction SilentlyContinue
    }

    foreach ($legacyFile in @("backup_monitores.cfg", "backup_monitores_meta.json", "backup_audio.txt")) {
        $legacyPath = Join-Path $exeDir $legacyFile
        $dataPath = Join-Path $dataDir $legacyFile
        if ((Test-Path -LiteralPath $legacyPath) -and -not (Test-Path -LiteralPath $dataPath)) {
            Move-Item -LiteralPath $legacyPath -Destination $dataPath -Force -ErrorAction SilentlyContinue
        }
    }

    Set-ConsoleEnginePaths
}

function Set-ConsoleEnginePaths {
    $exeDir = Get-ConsoleExeDir
    $dataDir = Get-ConsoleDataDir
    $toolsDir = Get-ConsoleToolsDir
    $devMode = $Script:ConsolePaths.DevMode

    if ($devMode) {
        $rootMmt = Join-Path $exeDir "MultiMonitorTool.exe"
        $rootSvv = Join-Path $exeDir "SoundVolumeView.exe"
        $toolsMmt = Join-Path $toolsDir "MultiMonitorTool.exe"
        $toolsSvv = Join-Path $toolsDir "SoundVolumeView.exe"

        if (Test-Path -LiteralPath $rootMmt) {
            $Script:MmtPath = $rootMmt
        }
        elseif (Test-Path -LiteralPath $toolsMmt) {
            $Script:MmtPath = $toolsMmt
        }
        else {
            $Script:MmtPath = $rootMmt
        }

        if (Test-Path -LiteralPath $rootSvv) {
            $Script:SvvPath = $rootSvv
        }
        elseif (Test-Path -LiteralPath $toolsSvv) {
            $Script:SvvPath = $toolsSvv
        }
        else {
            $Script:SvvPath = $rootSvv
        }
    }
    else {
        $Script:MmtPath = Join-Path $toolsDir "MultiMonitorTool.exe"
        $Script:SvvPath = Join-Path $toolsDir "SoundVolumeView.exe"
    }

    $Script:ConfigPath = Join-Path $dataDir "config.json"
    $Script:BackupMonitorConfig = Join-Path $dataDir "backup_monitores.cfg"
    $Script:BackupMonitorMeta = Join-Path $dataDir "backup_monitores_meta.json"
    $Script:BackupAudioFile = Join-Path $dataDir "backup_audio.txt"
    $Script:AppRoot = $dataDir
}
