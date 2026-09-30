using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ConsoleMode.Services.Tv;

namespace ConsoleMode.Tests;

public sealed class TvControlTests
{
    [Fact]
    public void Adb_messages_carry_checksum_and_magic_and_decode_back()
    {
        var banner = AdbProtocol.ConnectBanner();
        var bytes = AdbProtocol.Encode(AdbProtocol.Connect, AdbProtocol.Version, AdbProtocol.MaxPayload, banner);

        Assert.Equal(AdbProtocol.HeaderSize + banner.Length, bytes.Length);
        Assert.Equal("CNXN", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal(AdbProtocol.Checksum(banner), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16)));
        Assert.Equal(AdbProtocol.Connect ^ 0xFFFFFFFF, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(20)));

        var (command, arg0, arg1, length) = AdbProtocol.DecodeHeader(bytes);
        Assert.Equal(AdbProtocol.Connect, command);
        Assert.Equal(AdbProtocol.Version, arg0);
        Assert.Equal(AdbProtocol.MaxPayload, arg1);
        Assert.Equal(banner.Length, length);
        Assert.Equal("host::\0", Encoding.ASCII.GetString(bytes, AdbProtocol.HeaderSize, length));
    }

    [Fact]
    public void Adb_header_with_a_wrong_magic_is_rejected()
    {
        var bytes = AdbProtocol.Encode(AdbProtocol.Okay, 1, 2);
        bytes[20] ^= 0xFF;
        Assert.Throws<InvalidDataException>(() => AdbProtocol.DecodeHeader(bytes));
    }

    [Fact]
    public void Adb_token_signature_verifies_as_a_sha1_digest()
    {
        using var key = RSA.Create(2048);
        var token = RandomNumberGenerator.GetBytes(20);
        var signature = AdbProtocol.SignToken(key, token);
        Assert.True(key.VerifyHash(token, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void Adb_public_key_uses_the_android_layout()
    {
        using var key = RSA.Create(2048);
        var parameters = key.ExportParameters(false);
        var encoded = Encoding.ASCII.GetString(AdbProtocol.EncodePublicKey(parameters, "ConsoleMode@PC"));

        Assert.EndsWith(" ConsoleMode@PC\0", encoded);
        var raw = Convert.FromBase64String(encoded[..encoded.IndexOf(' ')]);
        Assert.Equal(524, raw.Length);
        Assert.Equal(64u, BinaryPrimitives.ReadUInt32LittleEndian(raw));

        var modulus = new BigInteger(raw.AsSpan(8, 256), isUnsigned: true, isBigEndian: false);
        Assert.Equal(new BigInteger(parameters.Modulus, isUnsigned: true, isBigEndian: true), modulus);

        // n0inv = -1 / n mod 2^32
        var n0 = (uint)(modulus & uint.MaxValue);
        var n0Inverse = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4));
        Assert.Equal(uint.MaxValue, unchecked(n0 * n0Inverse));

        var rr = new BigInteger(raw.AsSpan(264, 256), isUnsigned: true, isBigEndian: false);
        Assert.Equal(BigInteger.ModPow(2, 4096, modulus), rr);
        Assert.Equal(65537u, BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(520)));
    }

    [Theory]
    [InlineData("192.168.0.50", "192.168.0.50", 5555)]
    [InlineData(" 192.168.0.50:5556 ", "192.168.0.50", 5556)]
    [InlineData("tv.lan", "tv.lan", 5555)]
    [InlineData("192.168.0.50:abc", "192.168.0.50:abc", 5555)]
    public void Adb_endpoint_defaults_to_port_5555(string input, string host, int port)
    {
        Assert.Equal((host, port), AdbProtocol.ParseEndpoint(input));
    }

    [Theory]
    [InlineData(1, 243)]
    [InlineData(4, 246)]
    [InlineData(0, 243)]
    [InlineData(9, 246)]
    public void Hdmi_inputs_map_to_the_android_tv_input_keys(int input, int key)
    {
        Assert.Equal(key, AdbProtocol.HdmiKey(input));
    }

    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("aa-bb-cc-dd-ee-ff")]
    [InlineData("AABBCCDDEEFF")]
    public void Mac_addresses_parse_in_the_usual_formats(string text)
    {
        Assert.True(WakeOnLan.TryParseMac(text, out var mac));
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, mac);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AA:BB:CC:DD:EE")]
    [InlineData("GG:BB:CC:DD:EE:FF")]
    public void Invalid_mac_addresses_are_rejected(string? text)
    {
        Assert.False(WakeOnLan.TryParseMac(text, out _));
    }

    [Fact]
    public void Magic_packet_is_six_ff_then_the_mac_sixteen_times()
    {
        var mac = new byte[] { 1, 2, 3, 4, 5, 6 };
        var packet = WakeOnLan.BuildPacket(mac);

        Assert.Equal(102, packet.Length);
        Assert.All(packet[..6], b => Assert.Equal(0xFF, b));
        for (var i = 0; i < 16; i++)
            Assert.Equal(mac, packet[(6 + i * 6)..(12 + i * 6)]);
    }
}
