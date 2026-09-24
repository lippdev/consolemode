namespace ConsoleMode.Services;

/// <summary>When a controller connecting should start a session. Pure, so it's tested.</summary>
public static class ControllerConnectPolicy
{
    /// <summary>Pads already connected report themselves right after startup.</summary>
    public static readonly TimeSpan StartupGrace = TimeSpan.FromSeconds(15);

    /// <summary>Wireless pads reconnect on their own after a session ends.</summary>
    public static readonly TimeSpan RestoreGrace = TimeSpan.FromSeconds(30);

    public static bool ShouldTrigger(DateTime now, DateTime startedAt, DateTime quietUntil, bool canTrigger) =>
        canTrigger && now - startedAt >= StartupGrace && now >= quietUntil;
}
