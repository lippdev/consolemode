using ConsoleMode.Services;
using Xunit;

namespace ConsoleMode.Tests;

public class UiScaleTests
{
    [Theory]
    // A 1080p screen at 100 %: as laid out.
    [InlineData(1920, 1080, 1)]
    // The same screen at 150 %: Windows gives 1280 × 720 units, the interface shrinks them back to the same pixels.
    [InlineData(1280, 720, 1280.0 / 1920)]
    // A 4K TV at 100 %: twice as big, the same share of the screen.
    [InlineData(3840, 2160, 2)]
    // A 1440p monitor at 125 %.
    [InlineData(2048, 1152, 1152.0 / 1080)]
    public void GrowsWithTheWindowsHeight(double width, double height, double expected) =>
        Assert.Equal(expected, UiScale.For(width, height), 6);

    [Fact]
    public void SameLayoutWhateverWindowsScale()
    {
        // A 4K TV at 100 %, 150 % and 200 %: the interface is laid out at the same size every time.
        Assert.Equal((1920, 1080), UiScale.LayoutSize(3840, 2160));
        Assert.Equal((1920, 1080), UiScale.LayoutSize(2560, 1440));
        Assert.Equal((1920, 1080), UiScale.LayoutSize(1920, 1080));
    }

    [Fact]
    public void ANarrowWindowShrinksToTheMinimumWidth()
    {
        var (width, height) = UiScale.LayoutSize(960, 1000);
        Assert.Equal(UiScale.MinimumWidth, width, 6);
        Assert.True(height > UiScale.DesignHeight);
    }

    [Fact]
    public void NoSizeYetLeavesItAlone() => Assert.Equal(1, UiScale.For(0, 0));
}
