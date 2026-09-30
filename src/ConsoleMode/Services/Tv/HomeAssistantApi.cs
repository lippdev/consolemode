namespace ConsoleMode.Services.Tv;

/// <summary>
/// Which Home Assistant service runs an entity (REST <c>POST /api/services/&lt;domain&gt;/&lt;service&gt;</c>).
/// Home Assistant already speaks CEC, webOS, Samsung, Roku…, so a script there can do
/// anything; Console Mode only starts it. Pure logic, so it's unit tested.
/// </summary>
public static class HomeAssistantApi
{
    public readonly record struct ServiceCall(string Domain, string Service, string EntityId);

    /// <summary>Entities that run an action instead of holding an on/off state.</summary>
    private static readonly Dictionary<string, string> Actions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["script"] = "turn_on",
        ["scene"] = "turn_on",
        ["automation"] = "trigger",
        ["button"] = "press",
        ["input_button"] = "press"
    };

    public static bool IsValidEntity(string? entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId)) return false;
        var id = entityId.Trim();
        var dot = id.IndexOf('.');
        return dot > 0 && dot < id.Length - 1 && id.IndexOf('.', dot + 1) < 0 &&
               id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '_' or '.');
    }

    /// <summary>Start: run the "on" entity (turn_on / trigger / press).</summary>
    public static ServiceCall? TurnOn(string onEntity) => For(onEntity, turnOn: true);

    /// <summary>
    /// Restore: run the "off" entity, or turn the "on" entity off when it holds a state
    /// (media_player, switch…). A script/scene/automation can't be "turned off": null.
    /// </summary>
    public static ServiceCall? TurnOff(string onEntity, string offEntity)
    {
        if (IsValidEntity(offEntity)) return For(offEntity, turnOn: true);
        if (!IsValidEntity(onEntity) || Actions.ContainsKey(Domain(onEntity))) return null;
        return For(onEntity, turnOn: false);
    }

    /// <summary>
    /// The base URL with a scheme. Without one it is https: the token travels in every request,
    /// so plain http has to be typed on purpose (<c>http://homeassistant.local:8123</c>).
    /// </summary>
    public static string NormalizeBaseUrl(string baseUrl)
    {
        var text = baseUrl.Trim().TrimEnd('/');
        return text.Length == 0 || text.Contains("://") ? text : "https://" + text;
    }

    /// <summary>True when the token would be sent in clear (an explicit http:// address).</summary>
    public static bool IsPlainHttp(string? baseUrl) =>
        !string.IsNullOrWhiteSpace(baseUrl) &&
        NormalizeBaseUrl(baseUrl).StartsWith("http://", StringComparison.OrdinalIgnoreCase);

    /// <summary>https://host:8123 (with or without a trailing slash) + /api/services/domain/service.</summary>
    public static Uri ServiceUri(string baseUrl, ServiceCall call) =>
        new($"{NormalizeBaseUrl(baseUrl)}/api/services/{call.Domain}/{call.Service}");

    public static string Body(ServiceCall call) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { ["entity_id"] = call.EntityId });

    private static ServiceCall? For(string entityId, bool turnOn)
    {
        if (!IsValidEntity(entityId)) return null;
        var entity = entityId.Trim();
        var domain = Domain(entity);
        var service = Actions.TryGetValue(domain, out var action) ? action : turnOn ? "turn_on" : "turn_off";
        return new ServiceCall(domain, service, entity);
    }

    private static string Domain(string entityId)
    {
        var id = entityId.Trim();
        return id[..id.IndexOf('.')];
    }
}
