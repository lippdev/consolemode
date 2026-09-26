using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// Home Assistant: runs the entity the user picked (a script that turns the TV on and sets
/// the input, a media_player…) through the REST API with a long-lived access token.
/// </summary>
public sealed class HomeAssistantController : ITvController
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var call = HomeAssistantApi.TurnOn(config.HomeAssistantOnEntity)
                   ?? throw new TvControlException(LocalizationService.Get("TvHaEntityMissing"));
        return CallAsync(config, call, ct);
    }

    public Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        var call = HomeAssistantApi.TurnOff(config.HomeAssistantOnEntity, config.HomeAssistantOffEntity);
        if (call is null)
        {
            AppLog.Write("TV: Home Assistant sem entidade para desligar; nada a fazer");
            return Task.CompletedTask;
        }
        return CallAsync(config, call.Value, ct);
    }

    private static async Task CallAsync(TvControlConfig config, HomeAssistantApi.ServiceCall call, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.HomeAssistantUrl))
            throw new TvControlException(LocalizationService.Get("TvHaUrlMissing"));
        var token = SecretProtector.Unprotect(config.HomeAssistantToken);
        if (string.IsNullOrWhiteSpace(token))
            throw new TvControlException(LocalizationService.Get("TvHaTokenMissing"));

        Uri uri;
        try
        {
            uri = HomeAssistantApi.ServiceUri(config.HomeAssistantUrl, call);
        }
        catch (UriFormatException)
        {
            throw new TvControlException(LocalizationService.Get("TvHaUrlMissing"));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(HomeAssistantApi.Body(call), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            AppLog.Write($"TV: Home Assistant inacessível: {ex.Message}");
            throw new TvControlException(LocalizationService.Get("TvHaUnreachable", config.HomeAssistantUrl));
        }

        using (response)
        {
            AppLog.Write($"TV: Home Assistant {call.Domain}.{call.Service} {call.EntityId} → {(int)response.StatusCode}");
            if (response.IsSuccessStatusCode) return;
            throw new TvControlException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => LocalizationService.Get("TvHaUnauthorized"),
                HttpStatusCode.NotFound or HttpStatusCode.BadRequest => LocalizationService.Get("TvHaNotFound", call.EntityId),
                _ => LocalizationService.Get("TvHaFailed", (int)response.StatusCode)
            });
        }
    }
}

/// <summary>Secrets in config.json, encrypted for the signed-in Windows user (DPAPI).</summary>
public static class SecretProtector
{
    private const string Prefix = "dpapi:";

    public static string Protect(string secret)
    {
        if (string.IsNullOrEmpty(secret)) return "";
        var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(data);
    }

    /// <summary>Plain text (never protected) passes through; unreadable data (other user/PC) is empty.</summary>
    public static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored ?? "";
        try
        {
            var data = ProtectedData.Unprotect(Convert.FromBase64String(stored[Prefix.Length..]), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            AppLog.Write($"TV: token do Home Assistant ilegível ({ex.GetType().Name}); informe de novo");
            return "";
        }
    }
}
