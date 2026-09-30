namespace ConsoleMode.Services;

/// <summary>
/// The rule for closing Steam when the user goes back to the PC, kept pure so it is tested: Steam is
/// asked to quit (its own "Exit", never a kill) only when Big Picture was the launcher, the setting is
/// on, Steam is actually running and no game is running. A running game means the user may be in the
/// middle of it, so Steam stays.
/// </summary>
public static class SteamShutdown
{
    public enum Decision
    {
        /// <summary>Ask Steam to exit.</summary>
        Shutdown,
        /// <summary>The launcher is not Big Picture: Steam was never ours to close.</summary>
        NotBigPicture,
        /// <summary>Turned off in Settings.</summary>
        SettingOff,
        /// <summary>Steam is not running: nothing to close (and asking would start it).</summary>
        NotRunning,
        /// <summary>A game launched through Steam is running (RunningAppID is not 0).</summary>
        GameRunning,
        /// <summary>The game state could not be read, so Steam must be left open.</summary>
        GameStateUnknown
    }

    public static Decision Decide(string launcherMode, bool closeSteamSetting, bool steamRunning, int? runningAppId)
    {
        if (!string.Equals(launcherMode, "bigPicture", StringComparison.Ordinal)) return Decision.NotBigPicture;
        if (!closeSteamSetting) return Decision.SettingOff;
        if (!steamRunning) return Decision.NotRunning;
        if (runningAppId is null) return Decision.GameStateUnknown;
        if (runningAppId > 0) return Decision.GameRunning;
        return Decision.Shutdown;
    }

    /// <summary>The line for the log.</summary>
    public static string Describe(Decision decision, int runningAppId) => decision switch
    {
        Decision.Shutdown => "Steam: pedindo para fechar (sair normal)",
        Decision.SettingOff => "Steam: mantida aberta (desligado nos ajustes)",
        Decision.NotRunning => "Steam: não estava aberta",
        Decision.GameRunning => $"Steam: mantida aberta (jogo em execução, id {runningAppId})",
        Decision.GameStateUnknown => "Steam: mantida aberta (não foi possível verificar se há jogo em execução)",
        _ => "Steam: não é o lançador desta sessão"
    };

    /// <summary>
    /// Steam keeps the running game's id in the registry (HKCU\Software\Valve\Steam\RunningAppID, a DWORD).
    /// Zero means the registry was read successfully and no game is running; null means the value is
    /// missing, malformed, out of range, or could not be read, so the state is unknown.
    /// </summary>
    public static int? ParseRunningAppId(object? registryValue) => registryValue switch
    {
        int id when id >= 0 => id,
        uint id when id <= int.MaxValue => (int)id,
        string text when int.TryParse(text, out var id) && id >= 0 => id,
        _ => null
    };
}
