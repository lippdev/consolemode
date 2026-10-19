namespace ConsoleMode.Services;

/// <summary>
/// The "custom app" launch mode: any .exe as the console launcher (issue #111). Validation, names and
/// the decision of when the session is over. Pure, so it's tested.
/// </summary>
public static class CustomApp
{
    public const string Mode = "customApp";

    /// <summary>How long the watched process may be gone before the session ends (covers restarts and hand-offs).</summary>
    public static readonly TimeSpan ExitGrace = TimeSpan.FromSeconds(8);

    /// <summary>How long to wait for the watched process to show up before giving up on watching it.</summary>
    public static readonly TimeSpan AppearTimeout = TimeSpan.FromSeconds(90);

    public enum Problem { None, Missing, NotExe, NotFound }

    public static string CleanPath(string? path) => (path ?? "").Trim().Trim('"').Trim();

    public static Problem Validate(string? path)
    {
        var clean = CleanPath(path);
        if (clean.Length == 0) return Problem.Missing;
        if (!clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return Problem.NotExe;
        return File.Exists(clean) ? Problem.None : Problem.NotFound;
    }

    /// <summary>The message that blocks starting, or null when the exe can be launched.</summary>
    public static string? ProblemText(string? path) => Validate(path) switch
    {
        Problem.Missing => LocalizationService.Get("CustomAppMissing"),
        Problem.NotExe => LocalizationService.Get("CustomAppNotExe", CleanPath(path)),
        Problem.NotFound => LocalizationService.Get("CustomAppNotFound", CleanPath(path)),
        _ => null
    };

    /// <summary>A process name as Process.GetProcessesByName wants it: no folder, no ".exe".</summary>
    public static string NormalizeProcessName(string? name)
    {
        var clean = CleanPath(name);
        var slash = clean.LastIndexOfAny(['\\', '/']);
        if (slash >= 0) clean = clean[(slash + 1)..];
        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) clean = clean[..^4];
        return clean.Trim();
    }

    /// <summary>The process that keeps the session going: the one the user named, else the launched exe.</summary>
    public static string WatchedProcess(string? exePath, string? watchProcess)
    {
        var watch = NormalizeProcessName(watchProcess);
        return watch.Length > 0 ? watch : NormalizeProcessName(exePath);
    }

    /// <summary>The name shown for the mode: the exe's description, else its file name.</summary>
    public static string DisplayName(string? exePath, string? fileDescription)
    {
        var description = fileDescription?.Trim() ?? "";
        return description.Length > 0 ? description : NormalizeProcessName(exePath);
    }

    /// <param name="goneSince">When the watched process was last seen missing after it appeared, or null while it runs.</param>
    public static bool IsExit(bool hasAppeared, bool running, DateTime? goneSince, DateTime now)
    {
        if (!hasAppeared || running || goneSince is null) return false;
        return now - goneSince.Value >= ExitGrace;
    }

    /// <summary>The process never showed up in time: stop waiting (the session stays until ended by hand).</summary>
    public static bool HasTimedOut(bool hasAppeared, DateTime? launchTime, DateTime now) =>
        !hasAppeared && launchTime is not null && now - launchTime.Value >= AppearTimeout;
}
