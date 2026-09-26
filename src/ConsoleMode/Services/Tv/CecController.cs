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
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetEnvironmentVariable("PATH"))
            .FirstOrDefault(File.Exists);

    private static string FindClient(TvControlConfig config) =>
        Locate(config.CecClientPath) ?? throw new TvControlException(LocalizationService.Get("TvCecClientMissing"));

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
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.StandardInput.WriteLineAsync(command.AsMemory(), ct);
        process.StandardInput.Close();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(CommandTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { /* already gone */ }
            if (ct.IsCancellationRequested) throw;
            throw new TvControlException(LocalizationService.Get("TvCecNoAnswer"));
        }

        var output = await stdout + await stderr;
        AppLog.Write($"TV: cec-client \"{command}\" → {process.ExitCode}");
        if (CecCommands.NoAdapter(output))
            throw new TvControlException(LocalizationService.Get("TvCecNoAdapter"));
        if (process.ExitCode != 0)
            throw new TvControlException(LocalizationService.Get("TvCecFailed", process.ExitCode));
    }
}
