using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>Finds Steam on this PC and lists the covers of the user's installed games (see <see cref="SteamArt"/>).</summary>
public static class ConsoleBackgroundService
{
    public const string ModeAuto = "auto";
    public const string ModeGradient = "gradient";
    public const string ModeImage = "image";

    public static readonly string[] Modes = [ModeAuto, ModeGradient, ModeImage];

    public static string NormalizeMode(string? mode) =>
        Modes.Contains(mode, StringComparer.OrdinalIgnoreCase) ? mode!.ToLowerInvariant() : ModeAuto;

    /// <summary>Steam's folder, or null when it isn't installed.</summary>
    public static string? FindSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? Path.GetFullPath(path) : null;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Fundo: Steam não encontrada: {ex.Message}");
            return null;
        }
    }

    /// <summary>Cover files of up to <paramref name="count"/> installed games, or an empty list (no Steam, no covers).</summary>
    public static List<string> FindCovers(int count)
    {
        try
        {
            var steam = FindSteamPath();
            if (steam is null) return [];
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            var installed = File.Exists(vdf) ? SteamArt.ParseInstalledAppIds(File.ReadAllText(vdf)) : [];
            bool HasCover(int id) => File.Exists(SteamArt.CoverPath(steam, id));

            // The installed games first; if they don't fill the screen, other covers Steam has cached
            // (games in the library), then all shuffled together.
            var ids = SteamArt.Pick(installed, HasCover, count, Random.Shared);
            if (ids.Count < count)
            {
                var cache = Path.Combine(steam, "appcache", "librarycache");
                var cached = Directory.Exists(cache)
                    ? Directory.EnumerateDirectories(cache).Select(d => int.TryParse(Path.GetFileName(d), out var id) ? id : 0).Where(id => id > 0)
                    : [];
                ids.AddRange(SteamArt.Pick(cached.Except(ids), HasCover, count - ids.Count, Random.Shared));
            }
            ids = SteamArt.Pick(ids, _ => true, count, Random.Shared);
            return [.. ids.Select(id => SteamArt.CoverPath(steam, id))];
        }
        catch (Exception ex)
        {
            AppLog.Write($"Fundo: capas da Steam: {ex.Message}");
            return [];
        }
    }
}
