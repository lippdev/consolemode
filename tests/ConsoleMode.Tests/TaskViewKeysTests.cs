using ConsoleMode.Services;
using Xunit;

namespace ConsoleMode.Tests;

public class TaskViewKeysTests
{
    private static readonly TaskViewPad Idle = new(0, 0, 0);
    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    /// <summary>A driver past its first sample with nothing held, ready to act.</summary>
    private static TaskViewKeys Armed()
    {
        var keys = new TaskViewKeys();
        keys.Step(Idle, Ms(0));
        return keys;
    }

    [Fact]
    public void NothingCountsUntilTheShortcutIsLetGo()
    {
        var keys = new TaskViewKeys();
        // Select + X (the shortcut) still held, then A pressed with it: no Enter yet.
        Assert.Empty(keys.Step(new TaskViewPad(0x0020 | 0x4000, 0, 0), Ms(0)));
        Assert.Empty(keys.Step(new TaskViewPad(0x0020 | 0x4000 | TaskViewKeys.A, 0, 0), Ms(16)));
        // All let go: armed, and only the next press counts.
        Assert.Empty(keys.Step(Idle, Ms(32)));
        Assert.Equal([TaskViewKey.Enter], keys.Step(new TaskViewPad(TaskViewKeys.A, 0, 0), Ms(48)));
    }

    [Fact]
    public void ADoesEnterAndBDoesEscapeOncePerPress()
    {
        var keys = Armed();
        Assert.Equal([TaskViewKey.Enter], keys.Step(new TaskViewPad(TaskViewKeys.A, 0, 0), Ms(16)));
        Assert.Empty(keys.Step(new TaskViewPad(TaskViewKeys.A, 0, 0), Ms(600)));
        Assert.Empty(keys.Step(Idle, Ms(616)));
        Assert.Equal([TaskViewKey.Escape], keys.Step(new TaskViewPad(TaskViewKeys.B, 0, 0), Ms(632)));
    }

    [Fact]
    public void AHeldDirectionRepeatsLikeAHeldKey()
    {
        var keys = Armed();
        var right = new TaskViewPad(TaskViewKeys.DpadRight, 0, 0);
        Assert.Equal([TaskViewKey.Right], keys.Step(right, Ms(100)));
        // Nothing more before the repeat delay...
        Assert.Empty(keys.Step(right, Ms(100 + 400)));
        // ...then one per interval.
        Assert.Equal([TaskViewKey.Right], keys.Step(right, Ms(100 + 420)));
        Assert.Empty(keys.Step(right, Ms(100 + 500)));
        Assert.Equal([TaskViewKey.Right], keys.Step(right, Ms(100 + 560)));
    }

    [Fact]
    public void ChangingDirectionActsAtOnce()
    {
        var keys = Armed();
        Assert.Equal([TaskViewKey.Left], keys.Step(new TaskViewPad(TaskViewKeys.DpadLeft, 0, 0), Ms(16)));
        Assert.Equal([TaskViewKey.Down], keys.Step(new TaskViewPad(TaskViewKeys.DpadDown, 0, 0), Ms(32)));
        Assert.Empty(keys.Step(Idle, Ms(48)));
        Assert.Equal([TaskViewKey.Down], keys.Step(new TaskViewPad(TaskViewKeys.DpadDown, 0, 0), Ms(64)));
    }

    [Theory]
    [InlineData(-0.9, 0.1, TaskViewKey.Left)]
    [InlineData(0.8, -0.3, TaskViewKey.Right)]
    [InlineData(0.2, 0.9, TaskViewKey.Up)]
    [InlineData(-0.1, -0.7, TaskViewKey.Down)]
    public void TheStickMovesAlongItsStrongerAxis(double x, double y, TaskViewKey expected) =>
        Assert.Equal(expected, TaskViewKeys.Direction(new TaskViewPad(0, x, y)));

    [Fact]
    public void ASmallStickNudgeIsNoDirection() =>
        Assert.Null(TaskViewKeys.Direction(new TaskViewPad(0, 0.3, -0.4)));

    [Fact]
    public void TheDpadWinsOverTheStick() =>
        Assert.Equal(TaskViewKey.Up, TaskViewKeys.Direction(new TaskViewPad(TaskViewKeys.DpadUp, 0.9, 0)));

    [Fact]
    public void TheTaskViewShortcutIsSuggestedAndFreeOfTheOthers()
    {
        var suggested = ControllerShortcuts.Suggested(ShortcutSlot.TaskView);
        Assert.True(ControllerShortcuts.IsAcceptable(suggested));
        foreach (var other in new[] { ShortcutSlot.Home, ShortcutSlot.Menu, ShortcutSlot.Exit })
            Assert.False(ControllerShortcuts.Overlaps(suggested, ControllerShortcuts.Suggested(other)));
    }
}
