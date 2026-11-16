using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class DisplayModeMatchTests
{
    [Theory]
    // The owner's LG TV through NVIDIA: asked for 2560x1440@60, Windows puts it at @59 (59.94 Hz).
    [InlineData(2560, 1440, 60, 2560, 1440, 59, true)]
    [InlineData(2560, 1440, 60, 2560, 1440, 60, true)]
    [InlineData(3840, 2160, 24, 3840, 2160, 23, true)]
    [InlineData(1920, 1080, 120, 1920, 1080, 119, true)]
    [InlineData(1920, 1080, 144, 1920, 1080, 143, true)]
    // A different rate or size is not the mode asked for.
    [InlineData(2560, 1440, 60, 2560, 1440, 50, false)]
    [InlineData(1920, 1080, 120, 1920, 1080, 60, false)]
    [InlineData(2560, 1440, 60, 3840, 2160, 60, false)]
    [InlineData(2560, 1440, 60, 2560, 1600, 60, false)]
    // 0 = any (a mode saved without a rate, or only a rate).
    [InlineData(2560, 1440, 0, 2560, 1440, 59, true)]
    [InlineData(0, 0, 60, 3840, 2160, 59, true)]
    public void A_screen_is_in_the_mode_asked_for(int w, int h, int hz, int curW, int curH, int curHz, bool same) =>
        Assert.Equal(same, DisplayModeMatch.IsSame(w, h, hz, curW, curH, curHz));
}
