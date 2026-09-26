using System.Net.WebSockets;
using System.Text;
using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// LG webOS TVs over the network: Wake-on-LAN to power on (the TV's "Turn on via Wi-Fi" /
/// "LG Connect Apps" option), then SSAP over WebSocket to switch input and to power off.
/// The first time, the TV asks to allow Console Mode; the key it returns is kept.
/// </summary>
public sealed class WebOsController : ITvController
{
    public async Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        using var socket = await ConnectAsync(config, wakeOnLan: true, ct);
        await RegisterAsync(socket, config, approvalTimeout, ct);
        await RequestAsync(socket, WebOsProtocol.SwitchInput("input_0", config.HdmiInput), "input_0", ct);
    }

    public async Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        using var socket = await ConnectAsync(config, wakeOnLan: false, ct);
        await RegisterAsync(socket, config, TimeSpan.FromSeconds(5), ct);
        await RequestAsync(socket, WebOsProtocol.Request("off_0", WebOsProtocol.TurnOffUri), "off_0", ct);
    }

    private static Task<ClientWebSocket> ConnectAsync(TvControlConfig config, bool wakeOnLan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new TvControlException(LocalizationService.Get("TvHostMissing"));
        return TvNetwork.ConnectAsync(config, wakeOnLan, token => OpenAsync(config.Host.Trim(), token), ct);
    }

    /// <summary>Plain ws://:3000 first; newer firmware only answers wss://:3001 (self-signed).</summary>
    private static async Task<ClientWebSocket> OpenAsync(string host, CancellationToken ct)
    {
        try
        {
            return await OpenAsync(new Uri($"ws://{host}:{WebOsProtocol.Port}"), ct);
        }
        catch (Exception ex) when (TvNetwork.IsUnreachable(ex))
        {
            return await OpenAsync(new Uri($"wss://{host}:{WebOsProtocol.SecurePort}"), ct);
        }
    }

    private static async Task<ClientWebSocket> OpenAsync(Uri uri, CancellationToken ct)
    {
        var socket = new ClientWebSocket();
        // The TV's certificate is self-signed and only reachable on the local network.
        socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        using var connect = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connect.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            await socket.ConnectAsync(uri, connect.Token);
            return socket;
        }
        catch (Exception ex)
        {
            socket.Dispose();
            if (ex is OperationCanceledException && !ct.IsCancellationRequested)
                throw new IOException($"webOS: {uri} não respondeu");
            if (ex is WebSocketException) throw new IOException($"webOS: {ex.Message}", ex);
            throw;
        }
    }

    private static async Task RegisterAsync(ClientWebSocket socket, TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var host = config.Host.Trim();
        await SendAsync(socket, WebOsProtocol.Register(WebOsKeyStore.Load(host)), ct);

        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(approvalTimeout);
        try
        {
            while (true)
            {
                var reply = WebOsProtocol.Parse(await ReceiveAsync(socket, wait.Token));
                switch (reply.Kind)
                {
                    case WebOsProtocol.ReplyKind.Prompt:
                        AppLog.Write("TV: webOS pediu autorização; aguardando \"Permitir\" na TV");
                        break;
                    case WebOsProtocol.ReplyKind.Registered:
                        if (!string.IsNullOrWhiteSpace(reply.ClientKey)) WebOsKeyStore.Save(host, reply.ClientKey);
                        return;
                    case WebOsProtocol.ReplyKind.Error when reply.Id == "register_0":
                        AppLog.Write($"TV: webOS recusou o registro: {reply.Error}");
                        throw new TvControlException(LocalizationService.Get("TvWebOsNotAllowed"));
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TvControlException(LocalizationService.Get("TvWebOsNotAllowed"));
        }
    }

    private static async Task RequestAsync(ClientWebSocket socket, string message, string id, CancellationToken ct)
    {
        await SendAsync(socket, message, ct);
        while (true)
        {
            var reply = WebOsProtocol.Parse(await ReceiveAsync(socket, ct));
            if (reply.Id != id) continue;
            if (reply.Kind == WebOsProtocol.ReplyKind.Error)
                throw new TvControlException(LocalizationService.Get("TvWebOsRequestFailed", reply.Error ?? ""));
            if (reply.Kind == WebOsProtocol.ReplyKind.Response) return;
        }
    }

    private static Task SendAsync(ClientWebSocket socket, string message, CancellationToken ct) =>
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, endOfMessage: true, ct);

    private static async Task<string> ReceiveAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new IOException("webOS: a TV fechou a conexão");
            message.Write(buffer, 0, result.Count);
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.ToArray());
        }
    }
}

/// <summary>The key each LG TV hands out after "Allow", kept per TV address in the data folder.</summary>
internal static class WebOsKeyStore
{
    private static string PathFor(string host)
    {
        var safe = string.Concat(host.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        return Path.Combine(AppPaths.DataDir, $"webos-{safe}.key");
    }

    public static string? Load(string host)
    {
        try
        {
            var path = PathFor(host);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (Exception ex)
        {
            AppLog.Write($"TV: chave webOS ilegível: {ex.Message}");
            return null;
        }
    }

    public static void Save(string host, string key)
    {
        File.WriteAllText(PathFor(host), key);
        AppLog.Write("TV: webOS pareada");
    }
}
