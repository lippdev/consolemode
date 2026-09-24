using System.Text;
using System.Text.Json;

namespace ConsoleMode.Services;

/// <summary>
/// Wire format of the local control API (<see cref="ControlPipeService"/>): one JSON line in,
/// one JSON line out. Kept free of app state so it can be unit-tested.
/// <code>
/// → {"cmd":"status"}          // or "start", "stop", "show"
/// ← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
/// </code>
/// </summary>
public static class ControlProtocol
{
    public const string Status = "status";
    public const string Start = ProtocolService.StartAction;
    public const string Stop = ProtocolService.StopAction;
    public const string Show = ProtocolService.ShowAction;

    /// <summary>The command of a request line, lower-cased; "" when the line isn't one.</summary>
    public static string ParseCommand(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return "";
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("cmd", out var cmd) ||
                cmd.ValueKind != JsonValueKind.String)
                return "";
            var value = (cmd.GetString() ?? "").Trim().ToLowerInvariant();
            return value is Status or Start or Stop or Show ? value : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    public static string Reply(bool ok, string? error, bool active, bool restoring, string mode, string version)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteBoolean("ok", ok);
            if (error is not null) json.WriteString("error", error);
            json.WriteBoolean("active", active);
            json.WriteBoolean("restoring", restoring);
            json.WriteString("mode", mode);
            json.WriteString("version", version);
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
