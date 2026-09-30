using System.Net.Sockets;
using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>Connecting to a TV on the network, waking it with Wake-on-LAN when it's in deep standby.</summary>
public static class TvNetwork
{
    /// <summary>A TV woken by Wake-on-LAN takes a while to bring its network (and services) up.</summary>
    private static readonly TimeSpan WakeWait = TimeSpan.FromSeconds(20);

    /// <param name="connect">One connection attempt; throws a network error when nobody answers.</param>
    /// <param name="wakeOnLan">Send the magic packet (when a MAC is set) if the first attempt fails.</param>
    public static async Task<T> ConnectAsync<T>(TvControlConfig config, bool wakeOnLan,
        Func<CancellationToken, Task<T>> connect, CancellationToken ct)
    {
        try
        {
            return await connect(ct);
        }
        catch (Exception ex) when (IsUnreachable(ex) && wakeOnLan && WakeOnLan.TryParseMac(config.MacAddress, out var mac))
        {
            // Deep standby: no network until the magic packet wakes the TV.
            AppLog.Write($"TV: {config.Host} sem resposta; enviando Wake-on-LAN");
            await WakeOnLan.SendAsync(mac, ct);
        }
        catch (Exception ex) when (IsUnreachable(ex))
        {
            throw Unreachable(config);
        }

        var deadline = DateTime.UtcNow + WakeWait;
        while (true)
        {
            await Task.Delay(2000, ct);
            try
            {
                return await connect(ct);
            }
            catch (Exception ex) when (IsUnreachable(ex) && DateTime.UtcNow < deadline)
            {
                // Still booting; try again.
            }
            catch (Exception ex) when (IsUnreachable(ex))
            {
                throw Unreachable(config);
            }
        }
    }

    /// <summary>Nothing listening, no route, connect timeout or a TV that hung up mid-handshake.</summary>
    public static bool IsUnreachable(Exception ex) => ex is SocketException or IOException;

    private static TvControlException Unreachable(TvControlConfig config) =>
        new(LocalizationService.Get("TvUnreachable", config.Host));
}
