namespace ConsoleMode.Services;

/// <summary>Small pure rules behind the session menu, kept apart so they're tested.</summary>
public static class SessionMenuMath
{
    public const int VolumeStep = 5;

    public static int StepVolume(int current, int direction) =>
        Math.Clamp(current + Math.Sign(direction) * VolumeStep, 0, 100);

    /// <summary>"12 min" under an hour, "1 h 05 min" after.</summary>
    public static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        return elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours} h {elapsed.Minutes:00} min"
            : $"{(int)elapsed.TotalMinutes} min";
    }

    /// <summary>RTSS: back up the user's settings only the first time we touch them in a session.</summary>
    public static bool NeedsRtssBackup(bool limitAlreadyApplied) => !limitAlreadyApplied;
}
