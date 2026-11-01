using ConsoleMode.Models;

namespace ConsoleMode.Services;

/// <summary>
/// The "custom app" launch mode: any .exe as the console launcher (issue #111). Validation, names and
/// the decision of when the session is over. Pure, so it's tested.
/// </summary>
public static class CustomApp
{
    public const string Mode = "customApp";

    /// <summary>A launcher value for one custom app (Home tiles, the launcher picker): "customApp:&lt;id&gt;".</summary>
    public static string EntryMode(string id) => Mode + ":" + id;

    public static bool IsEntryMode(string? value) => value?.StartsWith(Mode + ":", StringComparison.Ordinal) == true;

    /// <summary>The custom app's id in a launcher value, or null for anything else.</summary>
    public static string? EntryId(string? value) => IsEntryMode(value) ? value![(Mode.Length + 1)..] : null;

    /// <summary>The smallest positive number not in use, as the new custom app's id.</summary>
    public static string NextId(IEnumerable<string> ids)
    {
        var used = ids.ToHashSet(StringComparer.Ordinal);
        var next = 1;
        while (used.Contains(next.ToString())) next++;
        return next.ToString();
    }

    /// <summary>
    /// The custom apps of a config: its list, or (a config from before the list) the one custom app it had, as id "1".
    /// Entries without an exe are dropped.
    /// </summary>
    public static List<CustomAppEntry> Entries(AppConfig config)
    {
        var list = (config.CustomApps ?? []).Where(e => CleanPath(e.Path).Length > 0 && e.Id.Length > 0).ToList();
        if (list.Count == 0 && CleanPath(config.CustomAppPath).Length > 0)
            list.Add(new CustomAppEntry { Id = "1", Path = CleanPath(config.CustomAppPath), Arguments = config.CustomAppArguments ?? "", WatchProcess = config.CustomAppWatchProcess ?? "" });
        return list;
    }

    /// <summary>The selected custom app: the one named by CustomAppId, else the first.</summary>
    public static CustomAppEntry? Selected(IReadOnlyList<CustomAppEntry> entries, string? id) =>
        entries.FirstOrDefault(e => e.Id == id) ?? entries.FirstOrDefault();

    /// <summary>
    /// A launched exe that is gone after handing off to another program (a launcher stub, a script): the program it
    /// started that is still running, followed from parent to child. Console helpers that come with any program
    /// don't count. Null when it started nothing that still runs.
    /// </summary>
    public static string? HandOffTarget(IEnumerable<(int Pid, int ParentPid, string Name)> processes, int launchedPid)
    {
        var all = processes.ToList();
        var frontier = new Queue<int>([launchedPid]);
        var seen = new HashSet<int> { launchedPid };
        while (frontier.Count > 0)
        {
            var parent = frontier.Dequeue();
            foreach (var child in all.Where(p => p.ParentPid == parent && p.Pid != parent && seen.Add(p.Pid)))
            {
                var name = NormalizeProcessName(child.Name);
                if (!Helpers.Contains(name)) return name;
                frontier.Enqueue(child.Pid);
            }
        }
        return null;
    }

    private static readonly HashSet<string> Helpers = new(StringComparer.OrdinalIgnoreCase) { "conhost", "cmd", "WerFault" };

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
