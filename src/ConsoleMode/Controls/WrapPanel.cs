using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace ConsoleMode.Controls;

/// <summary>
/// Lays children left to right and starts a new line when the next one doesn't fit,
/// so the summary chips wrap instead of being clipped on a narrow window.
/// </summary>
public sealed class WrapPanel : Panel
{
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(WrapPanel),
            new PropertyMetadata(0.0, (d, _) => ((WrapPanel)d).InvalidateMeasure()));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        return Place(availableSize.Width, arrange: false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Place(finalSize.Width, arrange: true);
        return finalSize;
    }

    private Size Place(double maxWidth, bool arrange)
    {
        double x = 0, y = 0, lineHeight = 0, width = 0;
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            if (size.Width <= 0 && size.Height <= 0) continue;

            if (x > 0 && x + size.Width > maxWidth)
            {
                x = 0;
                y += lineHeight + Spacing;
                lineHeight = 0;
            }

            if (arrange) child.Arrange(new Rect(x, y, Math.Min(size.Width, maxWidth), size.Height));
            width = Math.Max(width, x + size.Width);
            x += size.Width + Spacing;
            lineHeight = Math.Max(lineHeight, size.Height);
        }
        return new Size(double.IsInfinity(maxWidth) ? width : Math.Min(width, maxWidth), y + lineHeight);
    }
}
