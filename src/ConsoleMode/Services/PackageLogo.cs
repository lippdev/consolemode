namespace ConsoleMode.Services;

/// <summary>
/// Picks the sharpest file of a Store app's logo. The manifest names it as "Assets\Logo.png", but the
/// package holds variants such as "Logo.targetsize-256.png" or "Logo.scale-200.png"; the shell
/// only hands out the small one, which looks blurred on a TV tile.
/// </summary>
public static class PackageLogo
{
    /// <summary>The best file among <paramref name="files"/> (names in the logo's folder), or null.</summary>
    public static string? Pick(string manifestLogo, IEnumerable<string> files)
    {
        var stem = Path.GetFileNameWithoutExtension(manifestLogo);
        var extension = Path.GetExtension(manifestLogo);
        if (string.IsNullOrWhiteSpace(stem)) return null;

        string? best = null;
        var bestScore = int.MinValue;
        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            if (!name.StartsWith(stem, StringComparison.OrdinalIgnoreCase)
                || !name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
            var rest = name[stem.Length..^extension.Length];
            // "Logo2.png" is another file, not a variant of "Logo.png".
            if (rest.Length > 0 && rest[0] != '.') continue;
            var qualifiers = rest.TrimStart('.');
            if (Score(qualifiers) is not { } score || score <= bestScore) continue;
            best = file;
            bestScore = score;
        }
        return best;
    }

    /// <summary>Bigger wins; the unplated form beats the plated one at the same size. Contrast themes are skipped.</summary>
    private static int? Score(string qualifiers)
    {
        if (qualifiers.Length == 0) return 0;
        var size = 0;
        var unplated = false;
        foreach (var part in qualifiers.Split('_'))
        {
            if (part.StartsWith("contrast-", StringComparison.OrdinalIgnoreCase)) return null;
            if (part.Equals("altform-unplated", StringComparison.OrdinalIgnoreCase)) unplated = true;
            else if (part.StartsWith("altform-", StringComparison.OrdinalIgnoreCase)) return null;
            else if (part.StartsWith("targetsize-", StringComparison.OrdinalIgnoreCase) && int.TryParse(part[11..], out var target)) size = target;
            // scale-200 of a 44 px logo is 88 px.
            else if (part.StartsWith("scale-", StringComparison.OrdinalIgnoreCase) && int.TryParse(part[6..], out var scale)) size = 44 * scale / 100;
        }
        return size * 2 + (unplated ? 1 : 0);
    }
}
