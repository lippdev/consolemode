using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class UiModeResolverTests
{
    [Theory]
    [InlineData("console", false, false, true)]
    [InlineData("desktop", true, true, false)]
    [InlineData("auto", false, false, false)]
    [InlineData("auto", true, false, true)]
    [InlineData("auto", false, true, true)]
    [InlineData(null, true, false, true)]
    [InlineData("whatever", false, false, false)]
    public void IsConsole_FollowsTheChoiceThenTheController(string? mode, bool controller, bool launchedByController, bool expected) =>
        Assert.Equal(expected, UiModeResolver.IsConsole(mode, controller, launchedByController));

    [Theory]
    [InlineData(3, 0, 1, 1)]
    [InlineData(3, 2, 1, 0)]
    [InlineData(3, 0, -1, 2)]
    [InlineData(3, -1, 1, 0)]
    [InlineData(3, -1, -1, 2)]
    [InlineData(0, 0, 1, -1)]
    public void Cycle_WrapsAround(int count, int current, int step, int expected) =>
        Assert.Equal(expected, UiModeResolver.Cycle(count, current, step));
}
