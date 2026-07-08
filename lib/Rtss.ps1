#Requires -Version 5.1
# Console Mode - Integracao com RivaTuner Statistics Server (RTSS) via rtss-cli

$Script:RtssInstallCandidates = @(
    "${env:ProgramFiles(x86)}\RivaTuner Statistics Server\RTSS.exe",
    "$env:ProgramFiles\RivaTuner Statistics Server\RTSS.exe"
)

function Get-RtssInstallPath {
    foreach ($path in $Script:RtssInstallCandidates) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            return $path
        }
    }
    return $null
}

function Test-RtssInstalled {
    return [bool](Get-RtssInstallPath)
}

function Test-RtssRunning {
    foreach ($name in @('RTSS', 'RTSSHooksLoader64', 'RTSSHooksLoader')) {
        if (Get-Process -Name $name -ErrorAction SilentlyContinue) {
            return $true
        }
    }
    return $false
}

function Ensure-RtssRunning {
    if (Test-RtssRunning) { return $true }

    $rtssExe = Get-RtssInstallPath
    if (-not $rtssExe) { return $false }

    try {
        Start-Process -FilePath $rtssExe -WindowStyle Hidden -ErrorAction Stop
        Start-Sleep -Milliseconds 1500
        return (Test-RtssRunning)
    }
    catch {
        return $false
    }
}

function Test-RtssCliAvailable {
    if (-not $Script:RtssCliPath) { return $false }
    return (Test-Path -LiteralPath $Script:RtssCliPath)
}

function Invoke-RtssCli {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [int]$TimeoutMs = 8000
    )

    if (-not (Test-RtssCliAvailable)) {
        throw "rtss-cli.exe nao encontrado em $Script:RtssCliPath"
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Script:RtssCliPath
    $psi.Arguments = ($Arguments -join ' ')
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    [void]$process.Start()

    if (-not $process.WaitForExit($TimeoutMs)) {
        try { $process.Kill() } catch { }
        throw "rtss-cli expirou: $($Arguments -join ' ')"
    }

    $stdout = $process.StandardOutput.ReadToEnd().Trim()
    $stderr = $process.StandardError.ReadToEnd().Trim()
    if ($process.ExitCode -ne 0 -and $stdout -ne 'OK') {
        $msg = if ($stderr) { $stderr } elseif ($stdout) { $stdout } else { "exit $($process.ExitCode)" }
        throw "rtss-cli falhou ($($Arguments -join ' ')): $msg"
    }

    return $stdout
}

function Get-RtssGlobalFramerateLimit {
    try {
        $raw = Invoke-RtssCli -Arguments @('limit:get')
        $value = 0
        if ([int]::TryParse($raw, [ref]$value)) {
            return $value
        }
        return 0
    }
    catch {
        return $null
    }
}

function Get-RtssLimiterEnabled {
    try {
        $raw = Invoke-RtssCli -Arguments @('limiter:get')
        if ($raw -eq '1') { return $true }
        if ($raw -eq '0') { return $false }
        $value = 0
        if ([int]::TryParse($raw, [ref]$value)) {
            return ($value -ne 0)
        }
        return $null
    }
    catch {
        return $null
    }
}

function Backup-RtssFpsSettings {
    if (-not (Test-RtssCliAvailable)) { return $false }
    if (-not (Ensure-RtssRunning)) { return $false }

    $limit = Get-RtssGlobalFramerateLimit
    $limiter = Get-RtssLimiterEnabled
    if ($null -eq $limit -or $null -eq $limiter) { return $false }

    $backup = [ordered]@{
        FramerateLimit  = [int]$limit
        LimiterEnabled  = [bool]$limiter
        SavedAt         = (Get-Date).ToString('o')
    }

    $Script:ConsoleState.RtssBackup = $backup
    if ($Script:BackupRtssFpsFile) {
        $backup | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $Script:BackupRtssFpsFile -Encoding UTF8
    }
    return $true
}

function Set-RtssGlobalFpsLimit {
    param([Parameter(Mandatory)][int]$FpsLimit)

    if ($FpsLimit -le 0) { return $false }
    Invoke-RtssCli -Arguments @('limit:set', $FpsLimit.ToString()) | Out-Null
    Invoke-RtssCli -Arguments @('limiter:set', '1') | Out-Null
    return $true
}

function Restore-RtssFpsSettings {
    $hasBackupFile = ($Script:BackupRtssFpsFile -and (Test-Path -LiteralPath $Script:BackupRtssFpsFile))
    if (-not $Script:ConsoleState.RtssLimitApplied -and -not $hasBackupFile) { return }

    $backup = $Script:ConsoleState.RtssBackup
    if (-not $backup -and $hasBackupFile) {
        try {
            $backup = Get-Content -LiteralPath $Script:BackupRtssFpsFile -Raw -Encoding UTF8 | ConvertFrom-Json
        }
        catch { }
    }

    if (-not (Test-RtssCliAvailable)) {
        $Script:ConsoleState.RtssLimitApplied = $false
        $Script:ConsoleState.RtssBackup = $null
        return
    }

    if (-not (Ensure-RtssRunning)) {
        $Script:ConsoleState.RtssLimitApplied = $false
        $Script:ConsoleState.RtssBackup = $null
        return
    }

    try {
        if ($backup) {
            $limit = [int]$backup.FramerateLimit
            $limiterOn = [bool]$backup.LimiterEnabled
            Invoke-RtssCli -Arguments @('limit:set', $limit.ToString()) | Out-Null
            Invoke-RtssCli -Arguments @('limiter:set', $(if ($limiterOn) { '1' } else { '0' })) | Out-Null
        }
        else {
            Invoke-RtssCli -Arguments @('limiter:set', '0') | Out-Null
        }
    }
    catch {
        Write-Warning "Nao foi possivel restaurar configuracoes do RTSS: $_"
    }
    finally {
        $Script:ConsoleState.RtssLimitApplied = $false
        $Script:ConsoleState.RtssBackup = $null
        if ($hasBackupFile) {
            Remove-Item -LiteralPath $Script:BackupRtssFpsFile -Force -ErrorAction SilentlyContinue
        }
    }
}

function Enable-ConsoleFpsLimit {
    param([Parameter(Mandatory)][int]$FpsLimit)

    if ($FpsLimit -le 0) { return @{ Success = $false; Message = "" } }

    if (-not (Test-RtssInstalled)) {
        return @{
            Success = $false
            Message = "RivaTuner Statistics Server nao encontrado. Instale via MSI Afterburner para usar limite de FPS."
        }
    }

    if (-not (Test-RtssCliAvailable)) {
        return @{
            Success = $false
            Message = "rtss-cli.exe nao encontrado. Execute build\Get-RtssCli.ps1 ou gere o ConsoleMode.exe novamente."
        }
    }

    if (-not (Ensure-RtssRunning)) {
        return @{
            Success = $false
            Message = "Nao foi possivel iniciar o RivaTuner Statistics Server."
        }
    }

    if (-not (Backup-RtssFpsSettings)) {
        return @{
            Success = $false
            Message = "Nao foi possivel ler as configuracoes atuais do RTSS."
        }
    }

    try {
        Set-RtssGlobalFpsLimit -FpsLimit $FpsLimit
        $Script:ConsoleState.FpsLimit = $FpsLimit
        $Script:ConsoleState.RtssLimitApplied = $true
        return @{ Success = $true; Message = "Limite de FPS global definido para $FpsLimit via RTSS." }
    }
    catch {
        $Script:ConsoleState.RtssBackup = $null
        return @{
            Success = $false
            Message = "Falha ao aplicar limite no RTSS: $_"
        }
    }
}

function Disable-ConsoleFpsLimit {
    Restore-RtssFpsSettings
    $Script:ConsoleState.FpsLimit = 0
}

function Test-ConsoleRtssReady {
    return ((Test-RtssInstalled) -and (Test-RtssCliAvailable))
}

function Get-FpsLimitLabel {
    param([int]$FpsLimit)
    if ($FpsLimit -le 0) { return "(nao limitar)" }
    return "$FpsLimit FPS"
}
