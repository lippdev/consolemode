using ConsoleMode.Services;
using Xunit;

namespace ConsoleMode.Tests;

public class IconShrinkTests
{
    private static byte[] Solid(int width, int height, byte b, byte g, byte r, byte a)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (b, g, r, a);
        return pixels;
    }

    [Fact]
    public void FitInside_KeepsTheShapeAndNeverEnlarges()
    {
        Assert.Equal((73, 73), IconPixels.FitInside(256, 256, 73));
        Assert.Equal((96, 48), IconPixels.FitInside(256, 128, 96));
        Assert.Equal((32, 32), IconPixels.FitInside(32, 32, 110));
        Assert.Equal((0, 0), IconPixels.FitInside(0, 10, 64));
    }

    [Fact]
    public void Shrink_KeepsASolidColour()
    {
        var shrunk = IconPixels.Shrink(Solid(256, 256, 10, 20, 30, 255), 256, 256, 73, 73);
        Assert.Equal(73 * 73 * 4, shrunk.Length);
        for (var i = 0; i < shrunk.Length; i += 4)
            Assert.Equal(new byte[] { 10, 20, 30, 255 }, shrunk[i..(i + 4)]);
    }

    [Fact]
    public void Shrink_AveragesEveryPixelItCovers()
    {
        // A 4×1 line, black-white-black-white, halved: each new pixel is half of each, so grey.
        var line = new byte[] { 0, 0, 0, 255, 255, 255, 255, 255, 0, 0, 0, 255, 255, 255, 255, 255 };
        var shrunk = IconPixels.Shrink(line, 4, 1, 2, 1);
        Assert.Equal(new byte[] { 128, 128, 128, 255, 128, 128, 128, 255 }, shrunk);
    }

    [Fact]
    public void Shrink_EdgeComesOutSoft()
    {
        // A hard edge falling inside a new pixel (3 opaque of 4 across) gives a partly covered pixel, not a step.
        var pixels = new byte[8 * 1 * 4];
        for (var x = 0; x < 3; x++) pixels[x * 4 + 3] = 255;
        var shrunk = IconPixels.Shrink(pixels, 8, 1, 2, 1);
        Assert.Equal(191, shrunk[3]);
        Assert.Equal(0, shrunk[7]);
    }

    [Fact]
    public void Shrink_LeavesSmallerIconsAsTheyAre()
    {
        var pixels = Solid(32, 32, 1, 2, 3, 4);
        Assert.Same(pixels, IconPixels.Shrink(pixels, 32, 32, 73, 73));
    }
}
