using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ConsoleMode.Services.Tv;

/// <summary>Wake-on-LAN "magic packet": 6 × 0xFF followed by the MAC address 16 times.</summary>
public static class WakeOnLan
{
    /// <summary>Accepts AA:BB:CC:DD:EE:FF, AA-BB-CC-DD-EE-FF or AABBCCDDEEFF.</summary>
    public static bool TryParseMac(string? text, out byte[] mac)
    {
        mac = [];
        if (string.IsNullOrWhiteSpace(text)) return false;
        var hex = text.Trim().Replace(":", "").Replace("-", "").Replace(".", "");
        if (hex.Length != 12) return false;

        var bytes = new byte[6];
        for (var i = 0; i < 6; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                return false;
        }
        mac = bytes;
        return true;
    }

    public static byte[] BuildPacket(byte[] mac)
    {
        if (mac.Length != 6) throw new ArgumentException("MAC must have 6 bytes", nameof(mac));
        var packet = new byte[6 + 16 * 6];
        Array.Fill(packet, (byte)0xFF, 0, 6);
        for (var i = 0; i < 16; i++) mac.CopyTo(packet, 6 + i * 6);
        return packet;
    }

    /// <summary>Broadcasts the packet on the usual ports (9 and 7); a few sends in case one is lost.</summary>
    public static async Task SendAsync(byte[] mac, CancellationToken ct)
    {
        var packet = BuildPacket(mac);
        using var udp = new UdpClient { EnableBroadcast = true };
        for (var attempt = 0; attempt < 3; attempt++)
        {
            foreach (var port in new[] { 9, 7 })
                await udp.SendAsync(packet, new IPEndPoint(IPAddress.Broadcast, port), ct);
            await Task.Delay(100, ct);
        }
    }
}
