using System.Text.RegularExpressions;
using ConsoleMode.Models;

namespace ConsoleMode.Services;

public enum GameChange { None, Started, Ended, Switched }

/// <summary>
/// Pure rules for the per-game profiles (issue #69): noticing when a Steam game starts or closes,
/// keeping the list, finding a game's name, and the one-line summary the settings show.
/// </summary>
public static partial class GameProfiles
{
    /// <summary>What happened between two reads of Steam's running game. An unknown read (null) changes nothing.</summary>
    public static GameChange Change(int previous, int? current)
    {
        if (current is not { } now || now == previous) return GameChange.None;
        if (previous == 0) return GameChange.Started;
        return now == 0 ? GameChange.Ended : GameChange.Switched;
    }

    public static GameProfile? Find(IEnumerable<GameProfile>? profiles, int appId) =>
        appId <= 0 || profiles is null ? null : profiles.FirstOrDefault(p => p.SteamAppId == appId);

    /// <summary>The list with this game's profile added or replaced, sorted by name.</summary>
    public static List<GameProfile> Upsert(IEnumerable<GameProfile>? profiles, GameProfile profile) =>
        [.. (profiles ?? []).Where(p => p.SteamAppId != profile.SteamAppId).Append(profile)
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)];

    public static List<GameProfile> Remove(IEnumerable<GameProfile>? profiles, int appId) =>
        [.. (profiles ?? []).Where(p => p.SteamAppId != appId)];

    /// <summary>Whether a profile would change anything at all.</summary>
    public static bool IsEmpty(GameProfile profile) =>
        profile.Mode is null && profile.Hdr is null && profile.FpsLimit is null && string.IsNullOrWhiteSpace(profile.AudioDeviceId);

    [GeneratedRegex("\"name\"\\s+\"(?<v>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex NameLine();

    [GeneratedRegex("\"path\"\\s+\"(?<v>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex PathLine();

    /// <summary>The game's name from its steamapps/appmanifest_ID.acf, or null.</summary>
    public static string? ParseManifestName(string? acf)
    {
        if (string.IsNullOrWhiteSpace(acf)) return null;
        var match = NameLine().Match(acf);
        var name = match.Success ? Unescape(match.Groups["v"].Value).Trim() : "";
        return name.Length > 0 ? name : null;
    }

    /// <summary>Every Steam library folder listed in libraryfolders.vdf.</summary>
    public static List<string> ParseLibraryPaths(string? vdf)
    {
        if (string.IsNullOrWhiteSpace(vdf)) return [];
        return [.. PathLine().Matches(vdf).Select(m => Unescape(m.Groups["v"].Value)).Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>"3840 × 2160 @ 120 Hz · HDR on · 60 FPS · LG TV": only what the profile changes.</summary>
    public static string Summary(GameProfile profile, string hdrOn, string hdrOff, string noLimit, string nothing)
    {
        var parts = new List<string>();
        if (profile.Mode is { Width: > 0, Height: > 0 } mode)
            parts.Add(mode.Frequency > 0 ? $"{mode.Width} × {mode.Height} @ {mode.Frequency} Hz" : $"{mode.Width} × {mode.Height}");
        if (profile.Hdr is { } hdr) parts.Add(hdr ? hdrOn : hdrOff);
        if (profile.FpsLimit is { } fps) parts.Add(fps > 0 ? $"{fps} FPS" : noLimit);
        if (!string.IsNullOrWhiteSpace(profile.AudioDeviceName)) parts.Add(profile.AudioDeviceName!);
        return parts.Count == 0 ? nothing : string.Join("  ·  ", parts);
    }

    private static string Unescape(string value) => value.Replace("\\\\", "\\").Replace("\\\"", "\"");
}
