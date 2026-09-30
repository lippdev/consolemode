using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// The pieces of the ADB wire protocol (the one <c>adb connect</c> speaks to port 5555) that
/// Console Mode needs to send key presses to an Android TV / Google TV, without shipping adb.exe.
/// Pure logic: framing, the auth signature and Android's public key format.
/// </summary>
public static class AdbProtocol
{
    public const uint Connect = 0x4e584e43; // CNXN
    public const uint Auth = 0x48545541;    // AUTH
    public const uint Open = 0x4e45504f;    // OPEN
    public const uint Okay = 0x59414b4f;    // OKAY
    public const uint Close = 0x45534c43;   // CLSE
    public const uint Write = 0x45545257;   // WRTE
    public const uint StartTls = 0x534c5453; // STLS

    public const uint AuthToken = 1;
    public const uint AuthSignature = 2;
    public const uint AuthRsaPublicKey = 3;

    /// <summary>Version with the payload checksum, which every device still accepts.</summary>
    public const uint Version = 0x01000000;
    public const uint MaxPayload = 256 * 1024;
    public const int HeaderSize = 24;
    public const int DefaultPort = 5555;

    // android.view.KeyEvent codes.
    public const int KeyWakeUp = 224;
    public const int KeySleep = 223;
    public const int KeyTvInputHdmi1 = 243;

    public readonly record struct Message(uint Command, uint Arg0, uint Arg1, byte[] Data);

    public static byte[] Encode(uint command, uint arg0, uint arg1, byte[]? data = null)
    {
        data ??= [];
        var buffer = new byte[HeaderSize + data.Length];
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span[0..], command);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], arg0);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], arg1);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)data.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], Checksum(data));
        BinaryPrimitives.WriteUInt32LittleEndian(span[20..], command ^ 0xFFFFFFFF);
        data.CopyTo(buffer, HeaderSize);
        return buffer;
    }

    /// <summary>Reads a header; returns the command, args and payload length.</summary>
    public static (uint Command, uint Arg0, uint Arg1, int Length) DecodeHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < HeaderSize) throw new InvalidDataException("ADB: cabeçalho curto");
        var command = BinaryPrimitives.ReadUInt32LittleEndian(header);
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header[20..]);
        if (magic != (command ^ 0xFFFFFFFF)) throw new InvalidDataException("ADB: cabeçalho inválido");
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header[12..]);
        if (length > MaxPayload) throw new InvalidDataException("ADB: mensagem grande demais");
        return (command,
            BinaryPrimitives.ReadUInt32LittleEndian(header[4..]),
            BinaryPrimitives.ReadUInt32LittleEndian(header[8..]),
            (int)length);
    }

    public static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        foreach (var b in data) sum += b;
        return sum;
    }

    /// <summary>"host::" banner sent with CNXN.</summary>
    public static byte[] ConnectBanner() => Encoding.ASCII.GetBytes("host::\0");

    public static byte[] ShellService(string command) => Encoding.UTF8.GetBytes($"shell:{command}\0");

    /// <summary>
    /// adb signs the 20-byte token as if it were a SHA-1 digest (RSA_sign with NID_sha1),
    /// i.e. PKCS#1 v1.5 over the SHA-1 DigestInfo of the raw token.
    /// </summary>
    public static byte[] SignToken(RSA key, byte[] token) =>
        key.SignHash(token, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

    /// <summary>
    /// Android's RSAPublicKey struct (android_pubkey_encode), base64 encoded, followed by
    /// " comment" and a NUL: what the TV shows in "Allow debugging from this computer?".
    /// </summary>
    public static byte[] EncodePublicKey(RSAParameters key, string comment)
    {
        var modulus = key.Modulus ?? throw new ArgumentException("modulus");
        var exponent = key.Exponent ?? throw new ArgumentException("exponent");
        if (modulus.Length != 256) throw new ArgumentException("ADB keys are RSA-2048");

        var n = new BigInteger(modulus, isUnsigned: true, isBigEndian: true);
        var n0 = BinaryPrimitives.ReadUInt32BigEndian(modulus.AsSpan(modulus.Length - 4));
        // Newton's iteration for the inverse of an odd number mod 2^32.
        var inverse = n0;
        for (var i = 0; i < 5; i++) inverse = unchecked(inverse * (2 - n0 * inverse));
        var n0Inverse = unchecked(0u - inverse);
        var rr = BigInteger.ModPow(2, 2 * 2048, n);

        var buffer = new byte[4 + 4 + 256 + 256 + 4];
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, 256 / 4);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], n0Inverse);
        WriteLittleEndian(n, span.Slice(8, 256));
        WriteLittleEndian(rr, span.Slice(264, 256));
        uint e = 0;
        foreach (var b in exponent) e = (e << 8) | b;
        BinaryPrimitives.WriteUInt32LittleEndian(span[520..], e);

        return Encoding.ASCII.GetBytes($"{Convert.ToBase64String(buffer)} {comment}\0");
    }

    /// <summary>"host" or "host:port" (default 5555).</summary>
    public static (string Host, int Port) ParseEndpoint(string value)
    {
        var text = value.Trim();
        var colon = text.LastIndexOf(':');
        if (colon > 0 && text.IndexOf(':') == colon && int.TryParse(text[(colon + 1)..], out var port) && port is > 0 and < 65536)
            return (text[..colon], port);
        return (text, DefaultPort);
    }

    /// <summary>Key code for "HDMI n" (1-4).</summary>
    public static int HdmiKey(int input) => KeyTvInputHdmi1 + Math.Clamp(input, 1, 4) - 1;

    private static void WriteLittleEndian(BigInteger value, Span<byte> destination)
    {
        destination.Clear();
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        bytes.AsSpan(0, Math.Min(bytes.Length, destination.Length)).CopyTo(destination);
    }
}
