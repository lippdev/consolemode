#Requires -Version 5.1
# Console Mode - Ponto de entrada

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Resolve-ConsoleEntryRoot {
    $candidates = [System.Collections.ArrayList]@()

    $baseDir = [AppDomain]::CurrentDomain.BaseDirectory
    if ($baseDir) {
        [void]$candidates.Add($baseDir.TrimEnd('\', '/'))
    }
    if ($ScriptRoot) {
        [void]$candidates.Add($ScriptRoot.ToString().TrimEnd('\', '/'))
    }
    if ($PSScriptRoot) {
        [void]$candidates.Add($PSScriptRoot.TrimEnd('\', '/'))
    }
    if ($MyInvocation.MyCommand.Path) {
        [void]$candidates.Add((Split-Path -Parent $MyInvocation.MyCommand.Path).TrimEnd('\', '/'))
    }

    $seen = @{}
    foreach ($dir in $candidates) {
        if ([string]::IsNullOrWhiteSpace($dir)) { continue }
        if ($seen.ContainsKey($dir)) { continue }
        $seen[$dir] = $true

        $pathsFile = Join-Path $dir "lib\Paths.ps1"
        if (Test-Path -LiteralPath $pathsFile) {
            return $dir
        }
    }

    if ($baseDir) {
        return $baseDir.TrimEnd('\', '/')
    }
    return (Get-Location).Path
}

$ScriptRoot = Resolve-ConsoleEntryRoot
$Script:EntryRoot = $ScriptRoot

. (Join-Path $ScriptRoot "lib\Paths.ps1")
. (Join-Path $ScriptRoot "lib\Engine.ps1")
Initialize-ConsoleAppLayout
. (Join-Path $ScriptRoot "lib\Gui.ps1")
Show-ConsoleModeGui
