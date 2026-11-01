using ConsoleMode.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Controls;

/// <summary>
/// Draws a window's interface in proportion to the window, like a console (see <see cref="UiScale"/>): the content is
/// laid out at a size 1080 tall and the <see cref="Viewbox"/> grows or shrinks it to fill <paramref name="area"/>.
/// </summary>
public static class UiScaleHost
{
    /// <summary>
    /// Keeps <paramref name="content"/> (the Viewbox's child) at the layout size for <paramref name="area"/>, and tells
    /// <paramref name="changed"/> the screen pixels per layout pixel (for images drawn to the pixel).
    /// </summary>
    public static void Attach(FrameworkElement area, Viewbox box, FrameworkElement content, Action<double>? changed = null)
    {
        box.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
        void Update()
        {
            if (area.ActualWidth <= 0 || area.ActualHeight <= 0) return;
            var (width, height) = UiScale.LayoutSize(area.ActualWidth, area.ActualHeight);
            content.Width = width;
            content.Height = height;
            changed?.Invoke(UiScale.For(area.ActualWidth, area.ActualHeight) * (area.XamlRoot?.RasterizationScale ?? 1));
        }
        area.SizeChanged += (_, _) => Update();
        area.Loaded += (_, _) =>
        {
            Update();
            if (area.XamlRoot is { } root) root.Changed += (_, _) => Update();
        };
    }
}
