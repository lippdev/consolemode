using System.Text.Json;
using System.Text.Json.Nodes;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// LG webOS "SSAP" messages (JSON over WebSocket, port 3000 / 3001): pairing and the two
/// requests Console Mode needs. Pure logic, so it's unit tested.
/// </summary>
public static class WebOsProtocol
{
    public const int Port = 3000;
    public const int SecurePort = 3001;

    public const string SwitchInputUri = "ssap://tv/switchInput";
    public const string TurnOffUri = "ssap://system/turnOff";

    /// <summary>Only what switching input and power need; the TV lists them in its pairing prompt.</summary>
    public static readonly string[] Permissions = ["CONTROL_POWER", "CONTROL_INPUT_TV", "READ_INPUT_DEVICE_LIST", "READ_POWER_STATE"];

    public enum ReplyKind
    {
        /// <summary>The TV shows "Allow this device?" and waits.</summary>
        Prompt,
        Registered,
        Response,
        Error,
        Other
    }

    public readonly record struct Reply(ReplyKind Kind, string? Id, string? ClientKey, string? Error);

    /// <param name="clientKey">Key from a previous pairing; without it the TV prompts.</param>
    public static string Register(string? clientKey)
    {
        var payload = new JsonObject
        {
            ["forcePairing"] = false,
            ["pairingType"] = "PROMPT",
            ["manifest"] = new JsonObject
            {
                ["manifestVersion"] = 1,
                ["appVersion"] = "1.1",
                ["permissions"] = new JsonArray(Permissions.Select(p => (JsonNode?)JsonValue.Create(p)).ToArray())
            }
        };
        if (!string.IsNullOrWhiteSpace(clientKey)) payload["client-key"] = clientKey;
        return new JsonObject { ["type"] = "register", ["id"] = "register_0", ["payload"] = payload }.ToJsonString();
    }

    public static string Request(string id, string uri, JsonObject? payload = null)
    {
        var message = new JsonObject { ["type"] = "request", ["id"] = id, ["uri"] = uri };
        if (payload is not null) message["payload"] = payload;
        return message.ToJsonString();
    }

    public static string SwitchInput(string id, int hdmi) =>
        Request(id, SwitchInputUri, new JsonObject { ["inputId"] = InputId(hdmi) });

    /// <summary>webOS names the HDMI ports "HDMI_1".."HDMI_4".</summary>
    public static string InputId(int hdmi) => $"HDMI_{Math.Clamp(hdmi, 1, 4)}";

    public static Reply Parse(string json)
    {
        try
        {
            var root = JsonNode.Parse(json) as JsonObject;
            if (root is null) return new Reply(ReplyKind.Other, null, null, null);
            var type = root["type"]?.GetValue<string>();
            var id = root["id"]?.GetValue<string>();
            var payload = root["payload"] as JsonObject;

            return type switch
            {
                "registered" => new Reply(ReplyKind.Registered, id, payload?["client-key"]?.GetValue<string>(), null),
                "error" => new Reply(ReplyKind.Error, id, null, root["error"]?.GetValue<string>() ?? "error"),
                "response" when payload?["pairingType"]?.GetValue<string>() == "PROMPT" => new Reply(ReplyKind.Prompt, id, null, null),
                "response" when payload?["returnValue"]?.GetValue<bool>() == false =>
                    new Reply(ReplyKind.Error, id, null, payload["errorText"]?.GetValue<string>() ?? "returnValue false"),
                "response" => new Reply(ReplyKind.Response, id, null, null),
                _ => new Reply(ReplyKind.Other, id, null, null)
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return new Reply(ReplyKind.Other, null, null, null);
        }
    }
}
