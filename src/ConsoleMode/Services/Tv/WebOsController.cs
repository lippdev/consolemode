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
        var connection = await ConnectAsync(config, wakeOnLan: true, ct);
        using var socket = connection.Socket;
        await RegisterAsync(connection, config, approvalTimeout, ct);
        await RequestAsync(socket, WebOsProtocol.SwitchInput("input_0", config.HdmiInput), "input_0", ct);
    }

    public async Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        var connection = await ConnectAsync(config, wakeOnLan: false, ct);
        using var socket = connection.Socket;
        await RegisterAsync(connection, config, TimeSpan.FromSeconds(5), ct);
        await RequestAsync(socket, WebOsProtocol.Request("off_0", WebOsProtocol.TurnOffUri), "off_0", ct);
    }

    private sealed record Connection(ClientWebSocket Socket, string? Fingerprint, bool Secure, bool HasPin);

    private static async Task<Connection> ConnectAsync(TvControlConfig config, bool wakeOnLan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new TvControlException(LocalizationService.Get("TvHostMissing"));
        try
        {
            return await TvNetwork.ConnectAsync(config, wakeOnLan, token => OpenAsync(config, token), ct);
        }
        catch (TvControlException ex) when (!config.WebOsAllowInsecure &&
            ex.Message == LocalizationService.Get("TvUnreachable", config.Host))
        {
            throw new TvControlException(LocalizationService.Get("TvWebOsSecureUnavailable"));
        }
    }

    /// <summary>Secure port first. Legacy clear-text requires a deliberate setting.</summary>
    private static async Task<Connection> OpenAsync(TvControlConfig config, CancellationToken ct)
    {
        var host = config.Host.Trim();
        var pin = WebOsKeyStore.LoadPin(host);
        try
        {
            return await OpenAsync(new Uri($"wss://{host}:{WebOsProtocol.SecurePort}"), pin, ct);
        }
        catch (IOException) when (config.WebOsAllowInsecure)
        {
            AppLog.Write("TV: webOS usando ws:// sem criptografia por opção do usuário");
            return await OpenAsync(new Uri($"ws://{host}:{WebOsProtocol.Port}"), null, ct);
        }
    }

    private static async Task<Connection> OpenAsync(Uri uri, string? pin, CancellationToken ct)
    {
        var socket = new ClientWebSocket();
        string? fingerprint = null;
        var certificateMismatch = false;
        if (uri.Scheme == "wss")
        {
            // LG certificates are commonly self-signed. Trust the first successful pairing,
            // then require the same certificate before sending a saved client key.
            socket.Options.RemoteCertificateValidationCallback = (_, cert, _, _) =>
            {
                if (cert is null) return false;
                fingerprint = WebOsSecurity.Fingerprint(cert);
                if (pin is null) return true;
                try
                {
                    if (WebOsSecurity.MatchesPin(pin, fingerprint)) return true;
                }
                catch (FormatException) { /* Invalid saved pin must fail closed. */ }
                certificateMismatch = true;
                return false;
            };
        }
        using var connect = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connect.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            await socket.ConnectAsync(uri, connect.Token);
            return new Connection(socket, fingerprint, uri.Scheme == "wss", pin is not null);
        }
        catch (Exception ex)
        {
            socket.Dispose();
            if (certificateMismatch)
                throw new TvControlException(LocalizationService.Get("TvWebOsCertificateChanged"));
            if (ex is OperationCanceledException && !ct.IsCancellationRequested)
                throw new IOException($"webOS: {uri} não respondeu");
            if (ex is WebSocketException) throw new IOException($"webOS: {ex.Message}", ex);
            throw;
        }
    }

    private static async Task RegisterAsync(Connection connection, TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var host = config.Host.Trim();
        var key = WebOsSecurity.MaySendSavedKey(connection.Secure, connection.HasPin, config.WebOsAllowInsecure)
            ? WebOsKeyStore.LoadKey(host) : null;
        await SendAsync(connection.Socket, WebOsProtocol.Register(key), ct);

        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(approvalTimeout);
        try
        {
            while (true)
            {
                var reply = WebOsProtocol.Parse(await ReceiveAsync(connection.Socket, wait.Token));
                switch (reply.Kind)
                {
                    case WebOsProtocol.ReplyKind.Prompt:
                        AppLog.Write("TV: webOS pediu autorização; aguardando \"Permitir\" na TV");
                        break;
                    case WebOsProtocol.ReplyKind.Registered:
                        if (connection.Secure && connection.Fingerprint is not null)
                            WebOsKeyStore.SavePin(host, connection.Fingerprint);
                        if (!string.IsNullOrWhiteSpace(reply.ClientKey)) WebOsKeyStore.SaveKey(host, reply.ClientKey);
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
            if (!WebOsSecurity.WithinMessageLimit(message.Length, result.Count))
                throw new IOException("webOS: resposta grande demais");
            message.Write(buffer, 0, result.Count);
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.ToArray());
        }
    }
}

/// <summary>
/// The client key each LG TV hands out after "Allow", kept per TV address in the data folder,
/// encrypted with DPAPI (see <see cref="SecretProtector"/>): the portable data folder travels
/// with the exe, and the key lets whoever has it control the paired TV.
/// </summary>
internal static class WebOsKeyStore
{
    private static string PathFor(string host, string extension) =>
        Path.Combine(AppPaths.DataDir, $"webos-{WebOsSecurity.HostId(host)}.{extension}");

    // Old filenames collapsed punctuation and can refer to more than one host. Never reuse them.
    private static string LegacyPathFor(string host) =>
        Path.Combine(AppPaths.DataDir, $"webos-{string.Concat(host.Select(c => char.IsLetterOrDigit(c) ? c : '_'))}.key");

    /// <summary>Null = no usable key: the TV asks to allow Console Mode again (and hands out a new one).</summary>
    public static string? LoadKey(string host)
    {
        try
        {
            var path = PathFor(host, "key");
            if (!File.Exists(path))
            {
                if (File.Exists(LegacyPathFor(host)))
                    AppLog.Write("TV: chave webOS antiga ignorada; a TV pedirá autorização novamente");
                return null;
            }
            if (SecretProtector.TryUnprotect(File.ReadAllText(path).Trim(), out var key)) return key.Length > 0 ? key : null;
            AppLog.Write("TV: chave webOS de outro usuário/PC; a TV pedirá autorização de novo");
            return null;
        }
        catch (IOException ex)
        {
            AppLog.Write($"TV: chave webOS ilegível: {ex.Message}");
            return null;
        }
    }

    public static string? LoadPin(string host)
    {
        var path = PathFor(host, "pin");
        if (!File.Exists(path)) return null;
        var pin = File.ReadAllText(path).Trim();
        if (pin.Length != 64 || !pin.All(Uri.IsHexDigit))
            throw new TvControlException(LocalizationService.Get("TvWebOsCertificateChanged"));
        return pin;
    }

    public static void SavePin(string host, string fingerprint) =>
        File.WriteAllText(PathFor(host, "pin"), fingerprint);

    public static void SaveKey(string host, string key)
    {
        File.WriteAllText(PathFor(host, "key"), SecretProtector.Protect(key));
        AppLog.Write("TV: webOS pareada");
    }

    public static void Forget(string host)
    {
        File.Delete(PathFor(host, "key"));
        File.Delete(PathFor(host, "pin"));
        AppLog.Write("TV: pareamento webOS esquecido; a TV pedirá autorização novamente");
    }
}
