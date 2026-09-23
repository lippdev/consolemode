#Requires -Version 5.1
# Baixa MultiMonitorTool e SoundVolumeView da NirSoft para a raiz do projeto

param(
    [string]$TargetDir = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$sources = @(
    @{
        Name = "MultiMonitorTool.exe"
        Url  = "https://www.nirsoft.net/utils/multimonitortool-x64.zip"
        Zip  = "multimonitortool-x64.zip"
    },
    @{
        Name = "SoundVolumeView.exe"
        Url  = "https://www.nirsoft.net/utils/soundvolumeview-x64.zip"
        Zip  = "soundvolumeview-x64.zip"
    }
)

$tempDir = Join-Path $env:TEMP "consolemode_tools_dl"
if (Test-Path -LiteralPath $tempDir) {
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

foreach ($tool in $sources) {
    $dest = Join-Path $TargetDir $tool.Name
    if (Test-Path -LiteralPath $dest) {
        Write-Host "OK: $($tool.Name) ja existe"
        continue
    }

    $zipPath = Join-Path $tempDir $tool.Zip
    Write-Host "Baixando $($tool.Name)..."
    $ua = @{ "User-Agent" = "Mozilla/5.0 ConsoleMode-build" }
    $downloaded = $false
    foreach ($attempt in 1..3) {
        try {
            Invoke-WebRequest -Uri $tool.Url -OutFile $zipPath -UseBasicParsing -Headers $ua
            $downloaded = $true
            break
        }
        catch {
            if ($attempt -eq 3) { throw }
            Start-Sleep -Seconds (2 * $attempt)
        }
    }
    if (-not $downloaded) {
        throw "Falha ao baixar $($tool.Name)"
    }

    $extractDir = Join-Path $tempDir ($tool.Name -replace '\.exe$', '')
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force

    $found = Get-ChildItem -LiteralPath $extractDir -Filter $tool.Name -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $found) {
        throw "Nao encontrei $($tool.Name) dentro do zip baixado"
    }

    Copy-Item -LiteralPath $found.FullName -Destination $dest -Force
    Write-Host "OK: $($tool.Name) -> $dest"
}

Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Ferramentas prontas em $TargetDir"
