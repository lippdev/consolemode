using System.ComponentModel;
using System.Diagnostics;

namespace ConsoleMode.Services;

/// <summary>
/// Reads and turns on "Allow this device to wake the computer" for the controller's adapters
/// (issue #155). Reading needs no rights; turning it on runs an elevated PowerShell, so Windows asks first.
/// </summary>
public static class ControllerWakeService
{
    private const string PowerShell = "powershell.exe";

    /// <summary>The controller-related devices that can wake the PC, or null when they couldn't be read.</summary>
    public static List<WakeDevice>? Read()
    {
        try
        {
            var json = ProcessRunner.RunCapture(PowerShell,
                ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", ControllerWake.Encode(ControllerWake.QueryScript)],
                timeoutMs: 20000);
            return ControllerWake.Relevant(ControllerWake.Parse(json));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Acordar pelo controle: leitura falhou: {ex.Message}");
            return null;
        }
    }

    public enum EnableResult { Done, Canceled, Failed }

    /// <summary>Lets the given devices wake the PC. Shows the UAC prompt; blocks until PowerShell exits.</summary>
    public static EnableResult Enable(IReadOnlyCollection<WakeDevice> devices)
    {
        if (devices.Count == 0) return EnableResult.Done;
        var script = ControllerWake.EnableScript(devices.Select(d => d.InstanceName));
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = PowerShell,
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand {ControllerWake.Encode(script)}",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (process is null) return EnableResult.Failed;
            if (!process.WaitForExit(30000))
            {
                AppLog.Write("Acordar pelo controle: o PowerShell não terminou em 30 s");
                return EnableResult.Failed;
            }
            AppLog.Write($"Acordar pelo controle: {string.Join(", ", devices.Select(d => d.Name))} => {process.ExitCode}");
            return process.ExitCode == 0 ? EnableResult.Done : EnableResult.Failed;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            AppLog.Write("Acordar pelo controle: permissão de administrador recusada");
            return EnableResult.Canceled;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Acordar pelo controle: {ex.Message}");
            return EnableResult.Failed;
        }
    }
}
