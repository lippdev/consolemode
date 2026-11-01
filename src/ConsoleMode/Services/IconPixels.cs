namespace ConsoleMode.Services;

/// <summary>
/// Pure pixel rules for the program icons of the window switcher. Windows hands an icon over as BGRA
/// with straight alpha, or, for old icons, with no alpha at all and a separate mask; the card needs
/// premultiplied BGRA. Kept apart so it is tested.
/// </summary>
public static class IconPixels
{
    /// <summary>
    /// Turns straight BGRA into premultiplied BGRA, in place. An icon without any alpha takes it from
    /// <paramref name="mask"/> (BGRA, same size: a non-black pixel is transparent), or becomes opaque without one.
    /// </summary>
    public static void Premultiply(byte[] color, byte[]? mask)
    {
        var hasAlpha = false;
        for (var i = 3; i < color.Length; i += 4)
        {
            if (color[i] == 0) continue;
            hasAlpha = true;
            break;
        }

        for (var i = 0; i + 3 < color.Length; i += 4)
        {
            var alpha = hasAlpha ? color[i + 3]
                : mask is not null && i + 2 < mask.Length && (mask[i] | mask[i + 1] | mask[i + 2]) != 0 ? (byte)0
                : (byte)255;
            color[i] = (byte)(color[i] * alpha / 255);
            color[i + 1] = (byte)(color[i + 1] * alpha / 255);
            color[i + 2] = (byte)(color[i + 2] * alpha / 255);
            color[i + 3] = alpha;
        }
    }

    /// <summary>Nothing to see: every pixel is fully transparent (or there are none).</summary>
    public static bool IsBlank(byte[] pixels)
    {
        for (var i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0) return false;
        return true;
    }

    /// <summary>
    /// The size an icon of <paramref name="width"/> × <paramref name="height"/> takes inside a square of
    /// <paramref name="box"/> pixels, keeping its shape; never larger than it is (enlarging is left to the screen).
    /// </summary>
    public static (int Width, int Height) FitInside(int width, int height, int box)
    {
        if (width <= 0 || height <= 0 || box <= 0) return (0, 0);
        var scale = Math.Min(1.0, (double)box / Math.Max(width, height));
        return (Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }

    /// <summary>
    /// Shrinks premultiplied BGRA by averaging every source pixel each new one covers (a box filter, partial
    /// pixels weighted by how much they cover). Drawing a 256-pixel logo at 64 lets the screen sample only a few
    /// of them, which leaves jagged edges; averaging them all keeps the edges smooth.
    /// </summary>
    public static byte[] Shrink(byte[] pixels, int width, int height, int newWidth, int newHeight)
    {
        if (newWidth >= width && newHeight >= height) return pixels;
        // Across first (each row to the new width), then down (each column to the new height).
        newWidth = Math.Min(newWidth, width);
        newHeight = Math.Min(newHeight, height);
        var across = new double[newWidth * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < newWidth; x++)
            {
                var (first, last, start, end) = Covered(x, width, newWidth);
                for (var i = first; i <= last; i++)
                {
                    var weight = Math.Min(end, i + 1) - Math.Max(start, i);
                    for (var c = 0; c < 4; c++) across[(y * newWidth + x) * 4 + c] += weight * pixels[(y * width + i) * 4 + c];
                }
                for (var c = 0; c < 4; c++) across[(y * newWidth + x) * 4 + c] /= end - start;
            }
        var result = new byte[newWidth * newHeight * 4];
        for (var y = 0; y < newHeight; y++)
        {
            var (first, last, start, end) = Covered(y, height, newHeight);
            for (var x = 0; x < newWidth; x++)
                for (var c = 0; c < 4; c++)
                {
                    var sum = 0.0;
                    for (var i = first; i <= last; i++)
                        sum += (Math.Min(end, i + 1) - Math.Max(start, i)) * across[(i * newWidth + x) * 4 + c];
                    result[(y * newWidth + x) * 4 + c] = (byte)Math.Clamp(Math.Round(sum / (end - start)), 0, 255);
                }
        }
        return result;
    }

    /// <summary>The source pixels new pixel <paramref name="index"/> covers, shrinking <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static (int First, int Last, double Start, double End) Covered(int index, int from, int to)
    {
        var step = (double)from / to;
        var start = index * step;
        var end = Math.Min(from, start + step);
        return ((int)start, Math.Min(from - 1, (int)Math.Ceiling(end) - 1), start, end);
    }
}
