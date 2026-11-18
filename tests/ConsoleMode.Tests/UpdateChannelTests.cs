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

    [Theory]
    [InlineData("", true)]        // stable
    [InlineData(null, true)]
    [InlineData("beta", true)]    // 1.7.1-beta.2
    [InlineData("Beta", true)]
    [InlineData("rc", true)]      // 1.8.0-rc.1
    [InlineData("alpha", false)]  // 2.0.0-alpha.1: a closed test, never offered
    [InlineData("Alpha", false)]
    [InlineData("dev", false)]
    public void Only_betas_and_release_candidates_reach_the_test_channel(string? label, bool offered) =>
        Assert.Equal(offered, UpdateChannel.IsOfferedLabel(label));
}
