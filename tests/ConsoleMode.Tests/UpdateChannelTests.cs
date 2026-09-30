using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class UpdateChannelTests
{
    [Theory]
    [InlineData(false, false, false)] // stable, not opted in: stable releases only
    [InlineData(false, true, true)]   // stable, opted in: test versions too
    [InlineData(true, false, true)]   // already on a test version: keeps getting them
    [InlineData(true, true, true)]
    public void Test_versions_are_offered_when_opted_in_or_already_installed(bool currentIsPrerelease, bool optedIn, bool expected) =>
        Assert.Equal(expected, UpdateChannel.IncludePrereleases(currentIsPrerelease, optedIn));
}
