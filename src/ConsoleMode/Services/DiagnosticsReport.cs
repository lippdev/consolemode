using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

public sealed record DiagnosticsSystem(string AppVersion, string Channel, string Windows, string Runtime);

/// <param name="Mode">"3840 × 2160 @ 120 Hz", or empty when unknown.</param>
/// <param name="Hdr">"on" | "off" | "unsupported" | "" (unknown).</param>
public sealed record DiagnosticsDisplay(string Name, string Mode, string Hdr, bool IsActive, bool IsPrimary, bool IsFocus, bool IsHidden);

public sealed record DiagnosticsAudio(string Name, bool IsDefault, bool IsConfigured);

/// <summary>
/// One connected controller. <paramref name="Detail"/> is free text printed after the type
/// (e.g. a battery level); the collector leaves it empty for now.
/// </summary>
public sealed record DiagnosticsController(string Name, string Type, string? Detail = null);

/// <param name="Path">Null when the program wasn't found.</param>
public sealed record DiagnosticsApp(string Name, string? Path, string? Version = null);

/// <summary>Everything the report shows. A null section is one that failed to load ("unavailable").</summary>
public sealed class DiagnosticsData
{
    public DiagnosticsSystem? System { get; init; }
    public IReadOnlyList<DiagnosticsDisplay>? Displays { get; init; }
    public IReadOnlyList<DiagnosticsAudio>? Audio { get; init; }
    public IReadOnlyList<DiagnosticsController>? Controllers { get; init; }
    public IReadOnlyList<DiagnosticsApp>? Apps { get; init; }
    public string? FullscreenMode { get; init; }
    public IReadOnlyList<string>? Log { get; init; }
}

/// <summary>
/// Settings → Diagnostics: the plain-text report users paste into an issue. English on purpose
/// (it's read by whoever answers the issue), and redacted: no user name in paths, no secrets.
/// </summary>
public static class DiagnosticsReport
{
    public const string Unavailable = "unavailable";
    public const int LogLines = 50;

    public static string Format(DiagnosticsData data)
    {
        var sb = new StringBuilder();

        Section(sb, "Console Mode", data.System is null ? null : [
            $"Version: {data.System.AppVersion} ({data.System.Channel})",
            $"Windows: {data.System.Windows}",
            $".NET: {data.System.Runtime}"
        ]);

        Section(sb, "Displays", data.Displays?.Select(d =>
        {
            var tags = new List<string>();
            if (d.IsFocus) tags.Add("game screen");
            if (d.IsHidden) tags.Add("hidden in console mode");
            if (d.IsPrimary) tags.Add("primary");
            if (!d.IsActive) tags.Add("off");
            var mode = d.Mode.Length > 0 ? d.Mode : "mode unknown";
            var hdr = d.Hdr.Length > 0 ? $", HDR {d.Hdr}" : "";
            var tagText = tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "";
            return $"- {d.Name}: {mode}{hdr}{tagText}";
        }));

        Section(sb, "Audio outputs", data.Audio?.Select(a =>
        {
            var tags = new List<string>();
            if (a.IsDefault) tags.Add("Windows default");
            if (a.IsConfigured) tags.Add("chosen in Console Mode");
            return $"- {a.Name}{(tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "")}";
        }));

        Section(sb, "Controllers", data.Controllers?.Select(c =>
            $"- {c.Name}: {c.Type}{(string.IsNullOrWhiteSpace(c.Detail) ? "" : $", {c.Detail}")}"));

        IEnumerable<string>? apps = data.Apps?.Select(a =>
            a.Path is null ? $"- {a.Name}: not found"
            : $"- {a.Name}: {a.Path}{(string.IsNullOrWhiteSpace(a.Version) ? "" : $" ({a.Version})")}");
        if (apps is not null && !string.IsNullOrEmpty(data.FullscreenMode))
            apps = apps.Prepend($"Launches: {data.FullscreenMode}");
        Section(sb, "Programs", apps);

        Section(sb, $"Log (last {LogLines} lines)", data.Log?.TakeLast(LogLines));

        return Redact(sb.ToString().TrimEnd() + Environment.NewLine);
    }

    private static void Section(StringBuilder sb, string title, IEnumerable<string>? lines)
    {
        if (sb.Length > 0) sb.AppendLine();
        sb.AppendLine($"== {title} ==");
        if (lines is null)
        {
            sb.AppendLine(Unavailable);
            return;
        }
        var any = false;
        foreach (var line in lines)
        {
            sb.AppendLine(line);
            any = true;
        }
        if (!any) sb.AppendLine("(none)");
    }

    private static readonly Regex UserPath = new(@"(?i)\b([a-z]:[\\/]+(?:users|documents and settings)[\\/]+)[^\\/\r\n""']+", RegexOptions.Compiled);
    private static readonly Regex Pem = new(@"-----BEGIN [A-Z ]+-----.*?(-----END [A-Z ]+-----|$)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Dpapi = new(SecretPrefix + @"[A-Za-z0-9+/=]+", RegexOptions.Compiled);
    private static readonly Regex NamedSecret = new(
        @"(?i)\b(token|password|passwd|secret|api[_-]?key|private[_-]?key|pairing[_-]?key|auth[_-]?key|authorization)(""?\s*[:=]\s*""?)(?:bearer\s+)?[^\s"",;]+",
        RegexOptions.Compiled);

    // SecretProtector.Prefix, repeated so this file stays free of DPAPI (and testable anywhere).
    private const string SecretPrefix = "dpapi:";

    /// <summary>Hides the Windows user name in profile paths and anything that looks like a key or token.</summary>
    public static string Redact(string text)
    {
        text = Pem.Replace(text, "[redacted key]");
        text = Dpapi.Replace(text, SecretPrefix + "[redacted]");
        text = NamedSecret.Replace(text, "$1$2[redacted]");
        return UserPath.Replace(text, "$1…");
    }
}
