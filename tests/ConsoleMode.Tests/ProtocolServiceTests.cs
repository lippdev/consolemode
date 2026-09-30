using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ProtocolServiceTests
{
    [Theory]
    [InlineData("consolemode://start", "start")]
    [InlineData("consolemode://start/", "start")]
    [InlineData("CONSOLEMODE://Stop", "stop")]
    [InlineData("consolemode:show", "show")]
    [InlineData("consolemode://start?preset=sala", "start")]
    [InlineData("consolemode://start#x", "start")]
    [InlineData("consolemode://menu", "menu")]
    public void ParseAction_ReadsTheActionFromTheUri(string argument, string expected) =>
        Assert.Equal(expected, ProtocolService.ParseAction(argument));

    [Theory]
    [InlineData("consolemode://reboot")]
    [InlineData("consolemode://")]
    [InlineData("--start")]
    [InlineData("http://consolemode://start")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseAction_IgnoresAnythingElse(string? argument) =>
        Assert.Null(ProtocolService.ParseAction(argument));
}
