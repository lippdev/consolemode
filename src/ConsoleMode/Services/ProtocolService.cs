using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// consolemode:// links (consolemode://start, consolemode://stop, consolemode://show) so
/// shortcuts, Stream Deck buttons, launchers and future widgets can drive the app.
/// Windows hands the URI to the exe as its first argument; the running instance is then
/// signalled the same way "--start" is. Registered per user, like StartupService.
/// </summary>
public static class ProtocolService
{
    public const string Scheme = "consolemode";
    public const string StartAction = "start";
    public const string StopAction = "stop";
    public const string ShowAction = "show";

    private const string ClassKey = @"Software\Classes\" + Scheme;

    /// <summary>The action named by a consolemode:// argument, or null when it isn't one.</summary>
    public static string? ParseAction(string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument) ||
            !argument.StartsWith(Scheme + ":", StringComparison.OrdinalIgnoreCase))
            return null;

        // "consolemode://start/", "consolemode:start" and "consolemode://START?x=1" all mean start.
        var rest = argument[(Scheme.Length + 1)..].TrimStart('/');
        var end = rest.IndexOfAny(['/', '?', '#']);
        var action = (end < 0 ? rest : rest[..end]).Trim().ToLowerInvariant();
        return action is StartAction or StopAction or ShowAction ? action : null;
    }

    /// <summary>Points the scheme at this exe; a no-op when it already does (portable moves included).</summary>
    public static void EnsureRegistered()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return;
            var command = $"\"{exe}\" \"%1\"";

            using var existing = Registry.CurrentUser.OpenSubKey(ClassKey + @"\shell\open\command");
            if (existing?.GetValue(null) is string current && string.Equals(current, command, StringComparison.OrdinalIgnoreCase))
                return;

            using var key = Registry.CurrentUser.CreateSubKey(ClassKey, writable: true);
            key.SetValue(null, "URL:Console Mode");
            key.SetValue("URL Protocol", "");
            using (var icon = key.CreateSubKey("DefaultIcon")) icon.SetValue(null, $"\"{exe}\",0");
            using (var open = key.CreateSubKey(@"shell\open\command")) open.SetValue(null, command);
            AppLog.Write($"Protocolo: {Scheme}:// registrado para {exe}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Protocolo: não foi possível registrar {Scheme}://: {ex.Message}");
        }
    }
}
