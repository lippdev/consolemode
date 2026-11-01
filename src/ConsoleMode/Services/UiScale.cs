namespace ConsoleMode.Services;

/// <summary>
/// The interface's size, like a console's: it is laid out for a screen 1080 tall and drawn in proportion to the
/// window, so it looks the same on a 1080p monitor, a 1440p one or a 4K TV, whatever scale Windows is set to.
/// Kept apart so it is tested.
/// </summary>
public static class UiScale
{
    /// <summary>The height the interface is laid out for (in its own pixels).</summary>
    public const double DesignHeight = 1080;

    /// <summary>The narrowest it is laid out for: on a narrow window it shrinks to this width instead of cutting.</summary>
    public const double MinimumWidth = 1280;

    /// <summary>
    /// How much the interface is grown (or shrunk) in a window of <paramref name="width"/> × <paramref name="height"/>
    /// (in Windows' own units): the window's height over 1080, or less when the window is too narrow for the layout.
    /// Windows' scale needs no part here: its units already grow with it, and this divides it back out.
    /// </summary>
    public static double For(double width, double height)
    {
        if (width <= 0 || height <= 0) return 1;
        return Math.Min(height / DesignHeight, width / MinimumWidth);
    }

    /// <summary>The size the interface is laid out at to fill that window once grown by <see cref="For"/>.</summary>
    public static (double Width, double Height) LayoutSize(double width, double height)
    {
        var scale = For(width, height);
        return (width / scale, height / scale);
    }
}
