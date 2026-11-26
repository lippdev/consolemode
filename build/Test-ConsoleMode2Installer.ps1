#Requires -Version 5.1
# End-to-end installation check on a fresh, disposable GitHub-hosted Windows runner.
$ErrorActionPreference = 'Stop'
if ($env:RUNNER_ENVIRONMENT -ne 'github-hosted' -or $env:RUNNER_OS -ne 'Windows') {
    throw 'Run the installer smoke test on a disposable GitHub-hosted Windows runner.'
}

$installRoot = Join-Path $env:LOCALAPPDATA 'Programs'
$stableDir = Join-Path $installRoot 'Console Mode'
$alphaDir = Join-Path $installRoot 'Console Mode 2 Alpha'
$stableData = Join-Path $env:LOCALAPPDATA 'ConsoleMode'
$alphaData = Join-Path $env:LOCALAPPDATA 'ConsoleMode2'
$uninstallKeys = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall'
$stableEntry = Join-Path $uninstallKeys '{6C1C3E8B-4F0E-4E0B-9C62-3A7E1D0C5B21}_is1'
$alphaEntry = Join-Path $uninstallKeys '{388223BE-BE29-42B4-AC17-4394D1A5F7FF}_is1'
foreach ($path in $stableDir, $alphaDir, $stableData, $alphaData, $stableEntry, $alphaEntry) {
    if (Test-Path -LiteralPath $path) { throw "Expected a clean runner. Found $path" }
}

function Invoke-Setup($path, $arguments) {
    $process = Start-Process -FilePath $path -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(120000)) { throw "Setup timed out: $path" }
    if ($process.ExitCode -ne 0) { throw "Setup failed ($($process.ExitCode)): $path" }
}

function Stop-TestInstance($path) {
    Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($path)) -ErrorAction SilentlyContinue |
        Where-Object Path -eq $path | Stop-Process -Force
}

function Get-StableState {
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    $protocolKey = 'HKCU:\Software\Classes\consolemode\shell\open\command'
    $shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Console Mode.lnk'
    $stableConfig = Join-Path $stableData 'config.json'
    [ordered]@{
        exe = (Get-FileHash -LiteralPath $stableExe).Hash
        shortcut = (Get-FileHash -LiteralPath $shortcut).Hash
        version = (Get-ItemProperty -LiteralPath $stableEntry).DisplayVersion
        language = (Get-ItemProperty -LiteralPath 'HKCU:\Software\ConsoleMode').Language
        startup = (Get-ItemProperty -LiteralPath $runKey -Name ConsoleMode).ConsoleMode
        protocol = (Get-Item -LiteralPath $protocolKey).GetValue('')
        data = (Get-FileHash -LiteralPath $marker).Hash
        config = if (Test-Path -LiteralPath $stableConfig) { (Get-FileHash -LiteralPath $stableConfig).Hash }
    } | ConvertTo-Json -Compress
}

$dist = Join-Path $PSScriptRoot '..\dist\v2'
$installer = Join-Path $dist 'ConsoleMode2Alpha-Setup-x64.exe'
$expected = ((Get-Content -LiteralPath "$installer.sha256" -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $installer).Hash -ne $expected) { throw 'Alpha checksum mismatch' }
$stableInstaller = Join-Path $env:RUNNER_TEMP 'ConsoleMode-1.7.1-Setup-x64.exe'
Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/lippdev/consolemode/releases/download/v1.7.1/ConsoleMode-Setup-x64.exe' -OutFile $stableInstaller
if ((Get-FileHash -LiteralPath $stableInstaller).Hash -ne 'e814378cb9d672a61237e2ecd9532fbdc450f03335d9f6a8e643510b3fa333de') {
    throw 'Unexpected stable installer digest'
}
$silent = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
Invoke-Setup $stableInstaller ($silent + '/TASKS=startup')
$stableExe = Join-Path $stableDir 'ConsoleMode.exe'
Stop-TestInstance $stableExe
$null = New-Item -Path $stableData -ItemType Directory -Force
$marker = Join-Path $stableData 'ci-preserve.txt'
Set-Content -LiteralPath $marker -Value 'Stable profile'
$before = Get-StableState

Invoke-Setup $installer $silent
$alphaExe = Join-Path $alphaDir 'ConsoleMode2.exe'
foreach ($name in 'ConsoleMode2.exe','ConsoleMode2Home.exe','AppxManifest.xml','ConsoleMode2Home.Dev.sccd') {
    if ((Get-FileHash (Join-Path $alphaDir $name)).Hash -ne (Get-FileHash (Join-Path $dist $name)).Hash) {
        throw "Installed payload mismatch: $name"
    }
}
if (-not (Test-Path -LiteralPath $alphaEntry)) { throw 'Alpha uninstall entry missing' }
# Silent updates auto-launch the app. A fresh profile must reach the GPUI window.
$log = Join-Path $alphaData 'consolemode2.log'
$deadline = (Get-Date).AddSeconds(45)
while (-not ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -Pattern 'window opened at' -Quiet))) {
    if ((Get-Date) -gt $deadline) { throw 'Installed alpha did not open its window' }
    Start-Sleep -Milliseconds 500
}
if ((Get-StableState) -ne $before) { throw 'Installing or launching the alpha changed stable 1.7.1' }
Stop-TestInstance $alphaExe
Invoke-Setup (Join-Path $alphaDir 'unins000.exe') $silent
if ((Test-Path -LiteralPath $alphaExe) -or (Test-Path -LiteralPath $alphaEntry)) { throw 'Alpha uninstall incomplete' }
if ((Get-StableState) -ne $before) { throw 'Uninstalling the alpha changed stable 1.7.1' }
