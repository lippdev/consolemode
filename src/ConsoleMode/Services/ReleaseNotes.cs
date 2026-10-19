using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>
/// Reads the GitHub release body written by .github/workflows/release.yml.
/// Since 1.5.0 the body has one section per language, each opened by a fixed
/// "## …" heading; older releases are pt-BR only, with no language headings.
/// Since 1.7.0 the body ends with one hidden summary per translated changelog
/// (<see cref="SummaryPrefix"/>), which wins over the English notes.
/// </summary>
public static class ReleaseNotes
{
    public const string PortugueseHeading = "## Português (Brasil)";
    public const string EnglishHeading = "## English (US)";

    /// <summary>"&lt;!-- summary pt-BR: first item --&gt;": an HTML comment, so GitHub doesn't show it.</summary>
    public const string SummaryPrefix = "<!-- summary ";

    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\([^)]*\)", RegexOptions.Compiled);

    /// <summary>
    /// First real line of the notes in <paramref name="language"/>, skipping headings and
    /// list markers, or null when the release has no notes in that language.
    /// </summary>
    public static string? FirstLine(string? notes, string language)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var lines = notes.Replace("\r\n", "\n").Split('\n');
        var summary = Summary(lines, language);
        if (summary is not null) return summary;

        // The release body has pt-BR and en-US sections; every other language reads the English one.
        var isEnglish = !string.Equals(language, LocalizationService.PortugueseBrazil, StringComparison.Ordinal);
        var bilingual = lines.Any(l => IsHeading(l, PortugueseHeading) || IsHeading(l, EnglishHeading));

        // Since 1.5.0-beta.6 the body is English only (Portuguese lives in CHANGELOG.md, linked
        // from the body), so a body without language headings is shown to everyone. The old
        // pt-only bodies (1.4.0 and earlier) are older than any client that would read them.
        if (!bilingual) isEnglish = false;

        var wanted = isEnglish ? EnglishHeading : PortugueseHeading;
        var other = isEnglish ? PortugueseHeading : EnglishHeading;
        var inSection = !bilingual;
        foreach (var raw in lines)
        {
            if (IsHeading(raw, wanted)) { inSection = true; continue; }
            if (IsHeading(raw, other)) { inSection = false; continue; }
            if (!inSection) continue;

            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed == "---") continue;
            var line = Clean(trimmed);
            if (line.Length > 0) return line;
        }
        return null;
    }

    /// <summary>The hidden summary for exactly <paramref name="language"/>, if the release has one.</summary>
    private static string? Summary(string[] lines, string language)
    {
        var prefix = SummaryPrefix + language + ":";
        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal) || !trimmed.EndsWith("-->", StringComparison.Ordinal)) continue;
            var line = Clean(trimmed[prefix.Length..^3]);
            if (line.Length > 0) return line;
        }
        return null;
    }

    /// <summary>Plain text for the notice: no list marker, bold or link syntax, at most 140 characters.</summary>
    private static string Clean(string text)
    {
        var line = MarkdownLink.Replace(text.Trim().TrimStart('-', '*', ' ').Replace("**", ""), "$1").Trim();
        return line.Length > 140 ? line[..140] + "…" : line;
    }

    private static bool IsHeading(string line, string heading) =>
        string.Equals(line.Trim().TrimStart('﻿'), heading, StringComparison.Ordinal);
}
