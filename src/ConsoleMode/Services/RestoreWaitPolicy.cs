namespace ConsoleMode.Services;

/// <summary>Bounds how long an explicit restore waits for another restore already in progress.</summary>
public static class RestoreWaitPolicy
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public static bool ShouldWait(bool restoreInProgress, TimeSpan elapsed) =>
        restoreInProgress && elapsed < Timeout;
}
