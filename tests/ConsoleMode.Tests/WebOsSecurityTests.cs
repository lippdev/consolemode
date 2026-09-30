using ConsoleMode.Services.Tv;

namespace ConsoleMode.Tests;

public sealed class WebOsSecurityTests
{
    [Fact]
    public void Host_id_is_stable_but_does_not_collapse_distinct_addresses()
    {
        Assert.Equal(WebOsSecurity.HostId(" TV.EXAMPLE "), WebOsSecurity.HostId("tv.example"));
        Assert.NotEqual(WebOsSecurity.HostId("192.168.0.2"), WebOsSecurity.HostId("192-168-0-2"));
    }

    [Fact]
    public void Saved_key_requires_a_pin_on_secure_connection_or_explicit_legacy_consent()
    {
        Assert.False(WebOsSecurity.MaySendSavedKey(secure: true, hasPinnedCertificate: false, allowInsecure: false));
        Assert.True(WebOsSecurity.MaySendSavedKey(secure: true, hasPinnedCertificate: true, allowInsecure: false));
        Assert.False(WebOsSecurity.MaySendSavedKey(secure: false, hasPinnedCertificate: false, allowInsecure: false));
        Assert.True(WebOsSecurity.MaySendSavedKey(secure: false, hasPinnedCertificate: false, allowInsecure: true));
    }

    [Fact]
    public void Changed_certificate_is_rejected()
    {
        var expected = new string('A', 64);
        Assert.True(WebOsSecurity.MatchesPin(expected, expected));
        Assert.False(WebOsSecurity.MatchesPin(expected, new string('B', 64)));
        Assert.False(WebOsSecurity.MatchesPin("", expected));
    }

    [Fact]
    public void Fragmented_reply_cannot_exceed_the_total_message_limit()
    {
        Assert.True(WebOsSecurity.WithinMessageLimit(WebOsSecurity.MaxMessageBytes - 1, 1));
        Assert.False(WebOsSecurity.WithinMessageLimit(WebOsSecurity.MaxMessageBytes - 1, 2));
        Assert.False(WebOsSecurity.WithinMessageLimit(0, WebOsSecurity.MaxMessageBytes + 1));
    }
}
