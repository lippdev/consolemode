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
}
