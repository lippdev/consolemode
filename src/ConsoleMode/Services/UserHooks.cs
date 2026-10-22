using System.Diagnostics;

namespace ConsoleMode.Services;

/// <summary>
/// Runs the user's own commands around a session. A hook never stops the session or the restore of the desk:
/// every failure is only logged.
/// </summary>
public static class UserHooks
{
    /// <summary>Runs the command hidden and waits for it, up to <see cref="UserCommand.Timeout"/>.</summary>
    /// <param name="label">Where in the session it runs, for the log.</param>
    public static void Run(string label, string? command)
    {
        if (!UserCommand.IsSet(command)) return;
        var text = UserCommand.ForLog(command);
        try
        {
            AppLog.Write($"Script ({label}): {text}");
            // ShellExecute with a hidden window, not CreateNoWindow: cmd then gets a real (hidden) console, so
            // "timeout" and "pause" in a .bat behave; with redirected handles "timeout" fails on the spot.
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = UserCommand.CmdArguments(command),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (process is null)
            {
                AppLog.Write($"Script ({label}): não foi possível iniciar");
                return;
            }
            if (!process.WaitForExit((int)UserCommand.Timeout.TotalMilliseconds))
            {
                AppLog.Write($"Script ({label}): passou de {UserCommand.Timeout.TotalSeconds:0} s; seguindo sem esperar");
                return;
            }
            AppLog.Write($"Script ({label}): terminou com código {process.ExitCode}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Script ({label}): {ex.Message}");
        }
    }
}
