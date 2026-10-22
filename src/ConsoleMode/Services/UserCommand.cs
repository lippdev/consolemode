namespace ConsoleMode.Services;

/// <summary>
/// Pure helpers for the user's own commands: one that runs when a session starts and one that runs after
/// the desk is back (a .bat, a PowerShell line, an exe with arguments). Pure, so it's tested.
/// </summary>
public static class UserCommand
{
    /// <summary>How long a command may run before the session carries on without waiting for it.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    public static string Clean(string? command) => (command ?? "").Trim();

    public static bool IsSet(string? command) => Clean(command).Length > 0;

    /// <summary>
    /// Arguments for cmd.exe that run the command the way the Run box would. /s makes cmd drop only the outer
    /// pair of quotes, so a quoted path followed by arguments ("C:\My Scripts\mods.bat" off) stays intact.
    /// </summary>
    public static string CmdArguments(string? command) => $"/d /s /c \"{Clean(command)}\"";

    /// <summary>The command as the log shows it: one line, never long.</summary>
    public static string ForLog(string? command)
    {
        var line = Clean(command).Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        return line.Length <= 120 ? line : line[..117] + "...";
    }
}
