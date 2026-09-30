using System.Net;
using System.Net.Http.Headers;
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

    /// <summary>
    /// Only used when the user ticked "Accept a self-signed certificate". It skips the certificate
    /// check for every address it is used with, so it is never the default and each use is logged.
    /// </summary>
    private static readonly HttpClient InsecureHttp = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    })
    { Timeout = TimeSpan.FromSeconds(15) };

    public Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var call = HomeAssistantApi.TurnOn(config.HomeAssistantOnEntity)
                   ?? throw new TvControlException(LocalizationService.Get("TvHaEntityMissing"));
        return CallAsync(config, call, ct);
    }

    public Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        HomeAssistantApi.ServiceCall? call;
        try
        {
            call = HomeAssistantApi.TurnOff(config.HomeAssistantOnEntity, config.HomeAssistantOffEntity);
        }
        catch (ArgumentException)
        {
            throw new TvControlException(LocalizationService.Get("TvHaEntityInvalid"));
        }
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
        if (!SecretProtector.TryUnprotect(config.HomeAssistantToken, out var token))
            throw new TvControlException(LocalizationService.Get("TvHaTokenUnreadable"));
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

        if (HomeAssistantApi.IsPlainHttp(config.HomeAssistantUrl))
            AppLog.Write("TV: Home Assistant por http:// — o token vai em texto puro pela rede; use só em rede confiável");
        var selfSigned = config.HomeAssistantAllowSelfSigned && uri.Scheme == Uri.UriSchemeHttps;
        if (selfSigned) AppLog.Write("TV: Home Assistant com certificado autoassinado aceito pelo usuário (sem validar o certificado)");

        HttpResponseMessage response;
        try
        {
            response = await (selfSigned ? InsecureHttp : Http).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException ex)
        {
            AppLog.Write($"TV: Home Assistant inacessível: {ex.Message}");
            var certificate = ex.InnerException is System.Security.Authentication.AuthenticationException;
            throw new TvControlException(LocalizationService.Get(certificate ? "TvHaCertificate" : "TvHaUnreachable", config.HomeAssistantUrl));
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
