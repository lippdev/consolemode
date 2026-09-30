using System.Text.Json;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControlProtocolTests
{
    [Theory]
    [InlineData("{\"cmd\":\"status\"}", "status")]
    [InlineData("{\"cmd\":\"start\"}", "start")]
    [InlineData("{\"cmd\":\" STOP \"}", "stop")]
    [InlineData("{\"cmd\":\"show\",\"from\":\"wakeon\"}", "show")]
    public void ParseCommand_ReadsKnownCommands(string line, string expected) =>
        Assert.Equal(expected, ControlProtocol.ParseCommand(line));

    [Theory]
    [InlineData("{\"cmd\":\"reboot\"}")]
    [InlineData("{\"cmd\":1}")]
    [InlineData("[\"start\"]")]
    [InlineData("start")]
    [InlineData("{")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseCommand_RejectsAnythingElse(string? line) =>
        Assert.Equal("", ControlProtocol.ParseCommand(line));

    [Fact]
    public void Reply_IsOneJsonObjectWithTheState()
    {
        var line = ControlProtocol.Reply(false, "restore did not finish", true, false, "xboxMode", "1.5.0");
        Assert.DoesNotContain('\n', line);
        using var doc = JsonDocument.Parse(line);
        var r = doc.RootElement;
        Assert.False(r.GetProperty("ok").GetBoolean());
        Assert.Equal("restore did not finish", r.GetProperty("error").GetString());
        Assert.True(r.GetProperty("active").GetBoolean());
        Assert.False(r.GetProperty("restoring").GetBoolean());
        Assert.Equal("xboxMode", r.GetProperty("mode").GetString());
        Assert.Equal("1.5.0", r.GetProperty("version").GetString());
    }

    [Fact]
    public void Reply_OmitsErrorWhenOk()
    {
        using var doc = JsonDocument.Parse(ControlProtocol.Reply(true, null, false, false, "bigPicture", "1.5.0"));
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
    }
}
