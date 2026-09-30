using System.Diagnostics;
using System.Text;
using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// HDMI-CEC through a Pulse-Eight USB-CEC adapter (inline on the HDMI cable): works with any
/// CEC TV. Needs libCEC installed (it brings cec-client.exe); no network involved.
/// </summary>
public sealed class CecController : ITvController
{
    /// <summary>Opening the adapter takes a few seconds per cec-client run.</summary>
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    public async Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var exe = FindClient(config);
        await RunAsync(exe, config.HdmiInput, CecCommands.PowerOn, ct);
        await RunAsync(exe, config.HdmiInput, CecCommands.ActiveSource, ct);
    }

    public Task TurnOffAsync(TvControlConfig config, CancellationToken ct) =>
        RunAsync(FindClient(config), config.HdmiInput, CecCommands.Standby, ct);

    /// <summary>The cec-client that will be used, or null (for the Settings description).</summary>
    public static string? Locate(string? customPath) =>
        CecCommands.Candidates(customPath,
                AppPaths.ExeDir,
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetEnvironmentVariable("PATH"))
            .FirstOrDefault(File.Exists);

    private static string FindClient(TvControlConfig config) =>
        Locate(config.CecClientPath) ?? throw new TvControlException(LocalizationService.Get("TvCecClientMissing"));

    /// <summary>
    /// One cec-client run. Nothing from the user's settings reaches its command line except the
    /// executable path (<see cref="ProcessStartInfo.FileName"/>, no shell): the arguments are fixed
    /// flags plus an HDMI port clamped to 1-4, and the CEC command is a constant written to stdin.
    /// </summary>
    private static async Task RunAsync(string exe, int hdmiInput, string command, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var arg in CecCommands.Arguments(hdmiInput)) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(CommandTimeout);
        process.Start();
        Task<string>? stdout = null;
        Task<string>? stderr = null;
        try
        {
            stdout = CecOutputCapture.ReadBoundedAsync(process.StandardOutput, CecOutputCapture.MaxCharacters, timeout.Token);
            stderr = CecOutputCapture.ReadBoundedAsync(process.StandardError, CecOutputCapture.MaxCharacters, timeout.Token);
            await process.StandardInput.WriteLineAsync(command.AsMemory(), timeout.Token);
            process.StandardInput.Close();

            await process.WaitForExitAsync(timeout.Token);
            var capturedOutput = await Task.WhenAll(stdout, stderr);
            var output = string.Concat(capturedOutput);
            AppLog.Write($"TV: cec-client \"{command}\" → {process.ExitCode}");
            if (CecCommands.NoAdapter(output))
                throw new TvControlException(LocalizationService.Get("TvCecNoAdapter"));
            if (process.ExitCode != 0)
                throw new TvControlException(LocalizationService.Get("TvCecFailed", process.ExitCode));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            AppLog.Write($"TV: cec-client \"{command}\" sem resposta em {CommandTimeout.TotalSeconds:0}s; encerrando");
            throw new TvControlException(LocalizationService.Get("TvCecNoAnswer"));
        }
        finally
        {
            timeout.Cancel();
            // Every way out (timeout, cancel, error): never leave a cec-client running, it holds the adapter.
            KillIfRunning(process);
            if (stdout is not null && stderr is not null)
            {
                try { await Task.WhenAll(stdout, stderr); }
                catch (Exception) { /* Preserve the original process error; both drains have now been observed. */ }
            }
        }
    }

    private static void KillIfRunning(Process process)
    {
        try
        {
            if (process.HasExited) return;
            process.Kill(entireProcessTree: true);
            process.WaitForExit(2000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Already gone.
        }
    }
}
