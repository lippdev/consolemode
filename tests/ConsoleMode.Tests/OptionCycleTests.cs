using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class OptionCycleTests
{
    [Theory]
    [InlineData(3, 0, 1, 1)]
    [InlineData(3, 2, 1, 0)]
    [InlineData(3, 0, -1, 2)]
    [InlineData(3, -1, 1, 0)]
    [InlineData(3, -1, -1, 2)]
    [InlineData(0, 0, 1, -1)]
    public void Cycle_WrapsAround(int count, int current, int step, int expected) =>
        Assert.Equal(expected, OptionCycle.Cycle(count, current, step));
}
