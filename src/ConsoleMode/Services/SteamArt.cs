using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>
/// Pure rules for the console background made of the user's own Steam covers: which games are
/// installed (steamapps/libraryfolders.vdf), where Steam keeps their cover, and which ones to show.
/// Nothing is bundled or copied: the app only reads images Steam already downloaded to this PC.
/// </summary>
public static partial class SteamArt
{
    public const string CoverFile = "library_600x900.jpg";

    [GeneratedRegex("\"apps\"\\s*\\{(?<body>[^}]*)\\}", RegexOptions.IgnoreCase)]
    private static partial Regex AppsBlock();

    [GeneratedRegex("\"(?<id>\\d+)\"\\s*\"\\d+\"")]
    private static partial Regex AppLine();

    /// <summary>App ids of the installed games listed in libraryfolders.vdf, in file order, without repeats.</summary>
    public static List<int> ParseInstalledAppIds(string vdf)
    {
        var ids = new List<int>();
        var seen = new HashSet<int>();
        foreach (Match block in AppsBlock().Matches(vdf))
        {
            foreach (Match line in AppLine().Matches(block.Groups["body"].Value))
            {
                if (int.TryParse(line.Groups["id"].Value, out var id) && id > 0 && seen.Add(id)) ids.Add(id);
            }
        }
        return ids;
    }

    /// <summary>Steam's cover cache for one game.</summary>
    public static string CoverPath(string steamPath, int appId) =>
        Path.Combine(steamPath, "appcache", "librarycache", appId.ToString(), CoverFile);

    /// <summary>
    /// Up to <paramref name="count"/> ids that have a cover, in a shuffled order so the collage
    /// differs from one launch to the next.
    /// </summary>
    public static List<int> Pick(IEnumerable<int> candidates, Func<int, bool> hasCover, int count, Random random)
    {
        var withCover = candidates.Where(hasCover).Distinct().ToList();
        for (var i = withCover.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (withCover[i], withCover[j]) = (withCover[j], withCover[i]);
        }
        return withCover.Take(Math.Max(0, count)).ToList();
    }

    /// <summary>Extensions the "my own image" option accepts.</summary>
    public static bool IsSupportedImage(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp";
}
