namespace ConsoleMode.Services;

/// <summary>
/// Names and IDs of audio outputs. The format is the one 1.5 saved (<c>AudioDeviceId</c> in
/// config.json and the restore backup), so both keep working after an update.
/// Pure, so it's tested.
/// </summary>
public static class AudioNaming
{
    /// <summary>
    /// The "friendly ID" of an output, as saved since 1.5:
    /// <c>{adapter}\Device\{endpoint}\Render</c>, e.g. <c>Realtek(R) Audio\Device\Speakers\Render</c>.
    /// </summary>
    public static string FriendlyId(string adapterName, string endpointName) =>
        $"{adapterName}\\Device\\{endpointName}\\Render";

    /// <summary>"Speakers (Realtek(R) Audio)"; just one of them when the other is empty or the same.</summary>
    public static string DisplayName(string endpointName, string adapterName)
    {
        if (string.IsNullOrWhiteSpace(endpointName)) return adapterName;
        if (!string.IsNullOrWhiteSpace(adapterName) && adapterName != endpointName) return $"{endpointName} ({adapterName})";
        return endpointName;
    }

    /// <summary>
    /// "The one that shows up when connecting" waits this long for a new output before settling for the game
    /// screen's own output that was already on (issue #149): a soundbar or receiver that wakes up later still wins.
    /// </summary>
    public static readonly TimeSpan ActiveOutputFallbackAfter = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The game screen's own HDMI/DisplayPort output: Windows names it after the monitor, as in
    /// "LG TV SSCR2 (NVIDIA High Definition Audio)" for the screen "LG TV SSCR2". Generic monitor names never match.
    /// </summary>
    public static bool BelongsToScreen(string? deviceName, string? monitorName)
    {
        var monitor = (monitorName ?? "").Trim();
        if (monitor.Length < 3 || IsGenericMonitorName(monitor)) return false;
        var device = (deviceName ?? "").Trim();
        return device.Equals(monitor, StringComparison.OrdinalIgnoreCase)
               || device.StartsWith(monitor + " (", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericMonitorName(string name) =>
        name.Contains("Generic", StringComparison.OrdinalIgnoreCase)
        || name.Contains("genérico", StringComparison.OrdinalIgnoreCase)
        || name.Contains("PnP", StringComparison.OrdinalIgnoreCase);
}
