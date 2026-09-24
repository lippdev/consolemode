namespace ConsoleMode.Services;

/// <summary>
/// Reads the GitHub release body written by .github/workflows/release.yml.
/// Since 1.5.0 the body has one section per language, each opened by a fixed
/// "## …" heading; older releases are pt-BR only, with no language headings.
/// </summary>
public static class ReleaseNotes
{
    public const string PortugueseHeading = "## Português (Brasil)";
    public const string EnglishHeading = "## English (US)";

    /// <summary>
    /// First real line of the notes in <paramref name="language"/>, skipping headings and
    /// list markers, or null when the release has no notes in that language.
    /// </summary>
    public static string? FirstLine(string? notes, string language)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        // The release body has pt-BR and en-US sections; every other language reads the English one.
        var isEnglish = !string.Equals(language, LocalizationService.PortugueseBrazil, StringComparison.Ordinal);
        var lines = notes.Replace("\r\n", "\n").Split('\n');
        var bilingual = lines.Any(l => IsHeading(l, PortugueseHeading) || IsHeading(l, EnglishHeading));

        // Legacy single-language body: it is pt-BR, so English gets nothing.
        if (!bilingual && isEnglish) return null;

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
            var line = trimmed.TrimStart('-', '*', ' ').Replace("**", "").Trim();
            if (line.Length > 0) return line.Length > 140 ? line[..140] + "…" : line;
        }
        return null;
    }

    private static bool IsHeading(string line, string heading) =>
        string.Equals(line.Trim().TrimStart('﻿'), heading, StringComparison.Ordinal);
}
