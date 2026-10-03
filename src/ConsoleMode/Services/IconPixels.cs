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
}
