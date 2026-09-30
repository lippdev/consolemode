using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// Minimal ADB client over TCP ("ADB debugging" / "network debugging" on the TV, port 5555):
/// connect, authenticate with this PC's key and run shell commands.
/// The first time, the TV asks "Allow debugging from this computer?"; with "Always allow"
/// ticked the key is remembered and later connections go straight through.
/// </summary>
public sealed class AdbClient : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private uint _nextLocalId = 1;

    private AdbClient(TcpClient tcp)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
    }

    /// <param name="approvalTimeout">How long to wait for the user to accept the prompt on the TV.</param>
    public static async Task<AdbClient> ConnectAsync(string host, int port, TimeSpan approvalTimeout, CancellationToken ct)
    {
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            using (var connect = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                connect.CancelAfter(TimeSpan.FromSeconds(3));
                try
                {
                    await tcp.ConnectAsync(host, port, connect.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new SocketException((int)SocketError.TimedOut);
                }
            }
            var client = new AdbClient(tcp);
            await client.HandshakeAsync(approvalTimeout, ct);
            return client;
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    public async Task<string> ShellAsync(string command, CancellationToken ct)
    {
        var localId = _nextLocalId++;
        uint remoteId = 0;
        var output = new StringBuilder();
        await SendAsync(AdbProtocol.Open, localId, 0, AdbProtocol.ShellService(command), ct);

        while (true)
        {
            var message = await ReadAsync(ct);
            if (message.Arg1 != localId) continue;
            switch (message.Command)
            {
                case AdbProtocol.Okay:
                    remoteId = message.Arg0;
                    break;
                case AdbProtocol.Write:
                    output.Append(Encoding.UTF8.GetString(message.Data));
                    await SendAsync(AdbProtocol.Okay, localId, message.Arg0, null, ct);
                    break;
                case AdbProtocol.Close:
                    if (remoteId != 0) await SendAsync(AdbProtocol.Close, localId, remoteId, null, ct);
                    return output.ToString();
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _tcp.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task HandshakeAsync(TimeSpan approvalTimeout, CancellationToken ct)
    {
        using var key = AdbKeyStore.LoadOrCreate();
        await SendAsync(AdbProtocol.Connect, AdbProtocol.Version, AdbProtocol.MaxPayload, AdbProtocol.ConnectBanner(), ct);

        var sentSignature = false;
        var sentPublicKey = false;
        while (true)
        {
            // After sending our public key the TV waits for someone to press "Allow".
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
            wait.CancelAfter(sentPublicKey ? approvalTimeout : TimeSpan.FromSeconds(5));
            AdbProtocol.Message message;
            try
            {
                message = await ReadAsync(wait.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                if (sentPublicKey) throw new TvControlException(LocalizationService.Get("TvAdbNotAllowed"));
                throw new IOException("ADB: a TV não respondeu ao handshake");
            }

            switch (message.Command)
            {
                case AdbProtocol.Connect:
                    return;
                case AdbProtocol.StartTls:
                    // "Wireless debugging" (Android 11+ pairing) wraps ADB in TLS; the classic
                    // "ADB debugging"/"network debugging" on port 5555 doesn't.
                    throw new TvControlException(LocalizationService.Get("TvAdbTlsUnsupported"));
                case AdbProtocol.Auth when message.Arg0 == AdbProtocol.AuthToken:
                    if (!sentSignature)
                    {
                        await SendAsync(AdbProtocol.Auth, AdbProtocol.AuthSignature, 0, AdbProtocol.SignToken(key, message.Data), ct);
                        sentSignature = true;
                    }
                    else if (!sentPublicKey)
                    {
                        AppLog.Write("TV: chave deste PC ainda não autorizada; aguardando \"Permitir\" na TV");
                        var publicKey = AdbProtocol.EncodePublicKey(key.ExportParameters(false), $"ConsoleMode@{Environment.MachineName}");
                        await SendAsync(AdbProtocol.Auth, AdbProtocol.AuthRsaPublicKey, 0, publicKey, ct);
                        sentPublicKey = true;
                    }
                    else
                    {
                        throw new TvControlException(LocalizationService.Get("TvAdbNotAllowed"));
                    }
                    break;
            }
        }
    }

    private async Task SendAsync(uint command, uint arg0, uint arg1, byte[]? data, CancellationToken ct) =>
        await _stream.WriteAsync(AdbProtocol.Encode(command, arg0, arg1, data), ct);

    private async Task<AdbProtocol.Message> ReadAsync(CancellationToken ct)
    {
        var header = new byte[AdbProtocol.HeaderSize];
        await _stream.ReadExactlyAsync(header, ct);
        var (command, arg0, arg1, length) = AdbProtocol.DecodeHeader(header);
        var data = new byte[length];
        if (length > 0) await _stream.ReadExactlyAsync(data, ct);
        return new AdbProtocol.Message(command, arg0, arg1, data);
    }
}

/// <summary>This PC's ADB key (RSA-2048), kept next to the config so the TV only asks once.</summary>
internal static class AdbKeyStore
{
    private static readonly object Gate = new();

    public static RSA LoadOrCreate()
    {
        lock (Gate)
        {
            var path = Path.Combine(AppPaths.DataDir, "adbkey.pem");
            if (File.Exists(path))
            {
                var saved = RSA.Create();
                try
                {
                    saved.ImportFromPem(File.ReadAllText(path));
                    if (saved.KeySize == 2048) return saved;
                }
                catch (Exception ex)
                {
                    AppLog.Write($"TV: chave ADB ilegível, criando outra: {ex.Message}");
                }
                saved.Dispose();
            }

            var created = RSA.Create(2048);
            File.WriteAllText(path, created.ExportPkcs8PrivateKeyPem());
            AppLog.Write("TV: chave ADB criada");
            return created;
        }
    }
}
