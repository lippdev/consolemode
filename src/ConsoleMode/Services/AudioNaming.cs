namespace ConsoleMode.Services;

/// <summary>
/// Names and IDs of audio outputs, shared by the SoundVolumeView and the native (Core Audio)
/// backends so the saved setting (<c>AudioDeviceId</c>) and the restore backup work with either.
/// Pure, so it's tested.
/// </summary>
public static class AudioNaming
{
    /// <summary>
    /// SoundVolumeView's "Command-Line Friendly ID" for an output:
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
