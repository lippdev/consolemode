using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerConnectTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Triggers_only_after_the_startup_grace() =>
        Assert.False(ControllerConnectPolicy.ShouldTrigger(T0 + TimeSpan.FromSeconds(5), T0, DateTime.MinValue, canTrigger: true));

    [Fact]
    public void Triggers_when_grace_passed_and_allowed() =>
        Assert.True(ControllerConnectPolicy.ShouldTrigger(T0 + TimeSpan.FromMinutes(1), T0, DateTime.MinValue, canTrigger: true));

    [Fact]
    public void Stays_quiet_after_a_restore() =>
        Assert.False(ControllerConnectPolicy.ShouldTrigger(T0 + TimeSpan.FromMinutes(1), T0, T0 + TimeSpan.FromMinutes(2), canTrigger: true));

    [Fact]
    public void Never_triggers_when_not_allowed() =>
        Assert.False(ControllerConnectPolicy.ShouldTrigger(T0 + TimeSpan.FromMinutes(1), T0, DateTime.MinValue, canTrigger: false));
}
