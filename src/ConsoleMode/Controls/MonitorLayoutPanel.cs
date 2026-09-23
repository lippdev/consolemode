using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace ConsoleMode.Controls;

/// <summary>
/// ItemsPanel that places each screen tile at its desktop position, like Windows display settings.
/// Positions and sizes are precomputed in <see cref="MainViewModel"/> (LayoutX/LayoutY/TileWidth/TileHeight).
/// </summary>
public sealed class MonitorLayoutPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            var row = RowOf(child);
            if (row is null)
            {
                child.Measure(new Size(0, 0));
                continue;
            }

            child.Measure(new Size(row.TileWidth, row.TileHeight));
            width = Math.Max(width, row.LayoutX + row.TileWidth);
            height = Math.Max(height, row.LayoutY + row.TileHeight);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var row = RowOf(child);
            if (row is null) continue;
            child.Arrange(new Rect(row.LayoutX, row.LayoutY, row.TileWidth, row.TileHeight));
        }
        return finalSize;
    }

    private static MonitorRowViewModel? RowOf(UIElement child) =>
        (child as ContentPresenter)?.Content as MonitorRowViewModel
        ?? (child as FrameworkElement)?.DataContext as MonitorRowViewModel;
}
