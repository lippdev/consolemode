namespace ConsoleMode.Services.Tv;

/// <summary>
/// HDMI-CEC through a Pulse-Eight USB-CEC adapter and libCEC's <c>cec-client</c>: arguments,
/// commands and how to read its output. Pure logic, so it's unit tested.
/// </summary>
public static class CecCommands
{
    public const string ExeName = "cec-client.exe";

    /// <summary>Wake the TV (logical address 0).</summary>
    public const string PowerOn = "on 0";

    /// <summary>"Active Source": the TV switches to the HDMI port the adapter sits on.</summary>
    public const string ActiveSource = "as";

    public const string Standby = "standby 0";

    /// <summary>
    /// One command per run (-s), quiet log (-d 1), as a playback device (-t p) on the TV's
    /// HDMI port the PC is plugged into (-p n), so "as" points the TV at the right input.
    /// </summary>
    public static string[] Arguments(int hdmiInput) =>
        ["-s", "-d", "1", "-t", "p", "-p", Math.Clamp(hdmiInput, 1, 4).ToString()];

    /// <summary>cec-client prints this (and exits non-zero) when no adapter answers.</summary>
    public static bool NoAdapter(string output) =>
        output.Contains("autodetect FAILED", StringComparison.OrdinalIgnoreCase) ||
        output.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase) ||
        output.Contains("no serial port given", StringComparison.OrdinalIgnoreCase);

    /// <summary>Where libCEC's installer puts cec-client, then the PATH.</summary>
    public static IEnumerable<string> Candidates(string? customPath, string? programFilesX86, string? programFiles, string? pathVariable)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            var custom = customPath.Trim().Trim('"');
            yield return custom.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? custom : Path.Combine(custom, ExeName);
            yield break;
        }

        foreach (var root in new[] { programFilesX86, programFiles })
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            yield return Path.Combine(root, "Pulse-Eight", "USB-CEC Adapter", ExeName);
        }

        foreach (var dir in (pathVariable ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return Path.Combine(dir, ExeName);
    }
}
