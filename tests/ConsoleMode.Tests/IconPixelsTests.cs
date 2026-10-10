using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class IconPixelsTests
{
    [Fact]
    public void Straight_alpha_becomes_premultiplied()
    {
        // One half-transparent white pixel, one opaque blue one (BGRA).
        var pixels = new byte[] { 255, 255, 255, 128, 200, 0, 0, 255 };
        IconPixels.Premultiply(pixels, mask: null);
        Assert.Equal(new byte[] { 128, 128, 128, 128, 200, 0, 0, 255 }, pixels);
    }

    [Fact]
    public void An_icon_without_alpha_takes_it_from_the_mask()
    {
        var pixels = new byte[] { 10, 20, 30, 0, 40, 50, 60, 0 };
        var mask = new byte[] { 0, 0, 0, 0, 255, 255, 255, 0 };   // black = drawn, white = transparent
        IconPixels.Premultiply(pixels, mask);
        Assert.Equal(new byte[] { 10, 20, 30, 255, 0, 0, 0, 0 }, pixels);
    }

    [Fact]
    public void An_icon_without_alpha_and_without_mask_is_opaque()
    {
        var pixels = new byte[] { 10, 20, 30, 0 };
        IconPixels.Premultiply(pixels, mask: null);
        Assert.Equal(new byte[] { 10, 20, 30, 255 }, pixels);
    }

    [Fact]
    public void The_mask_is_ignored_when_the_icon_has_its_own_alpha()
    {
        var pixels = new byte[] { 255, 255, 255, 255, 0, 0, 0, 0 };
        var mask = new byte[] { 255, 255, 255, 0, 0, 0, 0, 0 };
        IconPixels.Premultiply(pixels, mask);
        Assert.Equal(new byte[] { 255, 255, 255, 255, 0, 0, 0, 0 }, pixels);
    }

    [Fact]
    public void Blank_means_no_visible_pixel()
    {
        Assert.True(IconPixels.IsBlank([]));
        Assert.True(IconPixels.IsBlank([9, 9, 9, 0]));
        Assert.False(IconPixels.IsBlank([0, 0, 0, 0, 0, 0, 0, 1]));
    }
}
