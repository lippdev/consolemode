using System.Diagnostics;
using System.Text;

namespace ConsoleMode.Services;

public static class ProcessRunner
{
    public static int Run(string fileName, IEnumerable<string> arguments, int timeoutMs = 20000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            throw new TimeoutException(LocalizationService.Get("ProcessTimeout", Path.GetFileName(fileName), string.Join(' ', arguments)));
        }
        return process.ExitCode;
    }

    public static string RunCapture(string fileName, IEnumerable<string> arguments, int timeoutMs = 8000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* ignore */ }
            throw new TimeoutException(LocalizationService.Get("ProcessTimeout", Path.GetFileName(fileName), string.Join(' ', arguments)));
        }

        stdout = stdout.Trim();
        if (process.ExitCode != 0 && stdout != "OK")
        {
            var msg = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout;
            if (string.IsNullOrWhiteSpace(msg)) msg = $"exit {process.ExitCode}";
            throw new InvalidOperationException(LocalizationService.Get("ProcessFailure", Path.GetFileName(fileName), string.Join(' ', arguments), msg));
        }

        return stdout;
    }

    public static void StartDetached(string fileName, string? arguments = null)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments ?? "",
            UseShellExecute = true
        });
    }
}
