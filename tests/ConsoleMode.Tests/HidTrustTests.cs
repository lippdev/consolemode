using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class HidTrustTests
{
    [Fact]
    public void A_pad_with_impossible_readings_is_distrusted_after_a_few_polls()
    {
        // A Switch Pro Controller read as generic HID: 6 to 10 buttons down, nobody touching it.
        var streak = 0;
        Assert.False(ControllerMapping.UpdateHidTrust(6, ref streak));
        Assert.False(ControllerMapping.UpdateHidTrust(8, ref streak));
        Assert.True(ControllerMapping.UpdateHidTrust(10, ref streak));
    }

    [Fact]
    public void A_normal_pad_is_never_distrusted()
    {
        var streak = 0;
        for (var i = 0; i < 100; i++)
            Assert.False(ControllerMapping.UpdateHidTrust(i % 4, ref streak));
        Assert.Equal(0, streak);
    }

    [Fact]
    public void A_short_burst_over_the_limit_does_not_condemn_the_pad()
    {
        var streak = 0;
        Assert.False(ControllerMapping.UpdateHidTrust(7, ref streak));
        Assert.False(ControllerMapping.UpdateHidTrust(7, ref streak));
        Assert.False(ControllerMapping.UpdateHidTrust(2, ref streak));   // back to normal: the count restarts
        Assert.Equal(0, streak);
        Assert.False(ControllerMapping.UpdateHidTrust(7, ref streak));
        Assert.False(ControllerMapping.UpdateHidTrust(7, ref streak));
    }

    [Fact]
    public void The_limit_itself_is_allowed()
    {
        var streak = 0;
        for (var i = 0; i < 10; i++)
            Assert.False(ControllerMapping.UpdateHidTrust(ControllerMapping.MaxSimultaneousHidButtons, ref streak));
    }
}
