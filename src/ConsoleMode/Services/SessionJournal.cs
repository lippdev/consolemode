using System.Text.Json;

namespace ConsoleMode.Services;

/// <summary>
/// What a console session changed, written to disk when it starts and deleted once the desk is restored (PRD D-22,
/// CORE-012). If the app dies in between, the file is still there on the next start, and the session is undone from
/// it: the screens and RTSS from their own backup files, the audio, the desktop icons and the pointer from here.
/// </summary>
public sealed class SessionJournal
{
    public DateTime StartedAt { get; set; }
    public string FullscreenMode { get; set; } = "";
    /// <summary>The default audio output before the session.</summary>
    public string? BackupAudioId { get; set; }
    /// <summary>The desktop icons before the session; null when they aren't restored.</summary>
    public List<DesktopIcon>? DesktopIcons { get; set; }
    /// <summary>Where the pointer was before the session; null when it isn't parked.</summary>
    public int? PointerX { get; set; }
    public int? PointerY { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public (int X, int Y)? PointerBefore => PointerX is { } x && PointerY is { } y ? (x, y) : null;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string Serialize(SessionJournal journal) => JsonSerializer.Serialize(journal, Options);

    /// <summary>The journal, or null when the text isn't one (a half-written file must not stop the app from opening).</summary>
    public static SessionJournal? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<SessionJournal>(json, Options); }
        catch (JsonException) { return null; }
    }

    public static SessionJournal? Read(string path) => File.Exists(path) ? Parse(File.ReadAllText(path)) : null;

    public static void Write(string path, SessionJournal journal) => File.WriteAllText(path, Serialize(journal));

    public static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
