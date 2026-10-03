using System.Diagnostics;

namespace ConsoleMode.Services;

/// <summary>
/// Installs ControlFS from the session menu, when the user asks for it there: downloads the setup of its newest
/// release, checks it against GitHub's SHA-256 and runs it silently. The setup is per user (no admin prompt) and
/// does not start ControlFS when silent. Updating it afterwards is ControlFS's own business.
/// </summary>
public static class ControlFsInstaller
{
    private static readonly TimeSpan SetupTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns the installed exe. Throws with the reason when it can't be installed. <paramref name="progress"/> gets
    /// the download from 0 to 1, then <see cref="double.PositiveInfinity"/> once the setup is running.
    /// </summary>
    public static async Task<string> InstallAsync(IProgress<double>? progress, CancellationToken ct = default)
    {
        if (!Environment.Is64BitOperatingSystem) throw new PlatformNotSupportedException("ControlFS só tem instalador x64.");
        // A setup from an earlier attempt (or its Inno child, "ControlFS-Setup-x64.tmp") may still be at work:
        // never start a second one next to it.
        if (IsSetupRunning()) throw new InvalidOperationException("outro instalador do ControlFS ainda está rodando");

        var setup = await UpdateService.FindControlFsSetupAsync(ct)
            ?? throw new InvalidOperationException("nenhuma release com instalador verificável");
        var dir = Path.Combine(Path.GetTempPath(), "ConsoleModeUpdate");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, setup.Name);
        try
        {
            await UpdateService.DownloadAsync(setup.Url, file, progress, ct);
            if (!await UpdateIntegrity.VerifyFileAsync(file, setup.Digest, ct))
                throw new InvalidDataException("o SHA-256 do instalador não confere");
            AppLog.Write($"ControlFS: instalador {setup.Version} baixado e verificado");

            progress?.Report(double.PositiveInfinity);
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
                UseShellExecute = false
            }) ?? throw new InvalidOperationException("o instalador não iniciou");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(SetupTimeout);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                // Given up on: stop it (Inno runs a second, child process), or it could finish later and a new
                // attempt from the menu would start a second installer next to it.
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                await process.WaitForExitAsync(CancellationToken.None);
                throw new TimeoutException($"o instalador não terminou em {SetupTimeout.TotalMinutes:0} min e foi encerrado");
            }
            if (process.ExitCode != 0) throw new InvalidOperationException($"o instalador saiu com o código {process.ExitCode}");
        }
        finally
        {
            try { File.Delete(file); } catch { /* the temp folder is cleaned by Windows */ }
        }

        return ControlFsService.FindExe() ?? throw new InvalidOperationException("instalado, mas o ControlFS.exe não foi encontrado");
    }

    private static bool IsSetupRunning()
    {
        var running = false;
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try { running |= process.ProcessName.StartsWith("ControlFS-Setup", StringComparison.OrdinalIgnoreCase); }
                catch { /* exited while listing */ }
            }
        }
        return running;
    }
}
