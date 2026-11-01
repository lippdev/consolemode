using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Controls;

/// <summary>
/// A scrolling list's edges and where it is: each edge fades into the background while there is more past it
/// (instead of a hard cut), and a thin thumb beside it shows the position, only when the list is longer than
/// its space. The fades follow the scrolling with a short transition, but land at once when the list appears,
/// changes or is resized: animating then showed the hard cut for a moment before the fade came in.
/// </summary>
public sealed class ScrollEdges
{
    private readonly ScrollViewer _scroller;
    private readonly UIElement _fadeTop;
    private readonly UIElement _fadeBottom;
    private readonly FrameworkElement _track;
    private readonly FrameworkElement _thumb;
    private long _snapUntil;

    public ScrollEdges(ScrollViewer scroller, UIElement fadeTop, UIElement fadeBottom, FrameworkElement track, FrameworkElement thumb)
    {
        (_scroller, _fadeTop, _fadeBottom, _track, _thumb) = (scroller, fadeTop, fadeBottom, track, thumb);
        // Only a scroll the person makes animates; the rest is layout, which must look right on its first frame.
        scroller.ViewChanged += (_, _) => Update(animate: Environment.TickCount64 > _snapUntil);
        scroller.SizeChanged += (_, _) => Update(animate: false);
        scroller.Loaded += (_, _) => Snap();
        track.SizeChanged += (_, _) => Update(animate: false);
        if (scroller.Content is FrameworkElement content) content.SizeChanged += (_, _) => Update(animate: false);
        Snap();
    }

    /// <summary>
    /// The list was just shown or switched (a tab, a category): the edges land at once, including the scroll back to
    /// the top that usually follows.
    /// </summary>
    public void Snap()
    {
        _snapUntil = Environment.TickCount64 + 400;
        Update(animate: false);
    }

    private void Update(bool animate)
    {
        var scrollable = _scroller.ScrollableHeight > 1;
        var offset = _scroller.VerticalOffset;
        Show(_fadeTop, scrollable && offset > 1, animate);
        Show(_fadeBottom, scrollable && offset < _scroller.ScrollableHeight - 1, animate);
        Show(_track, scrollable, animate);
        if (!scrollable || _scroller.ExtentHeight <= 0) return;
        var track = _track.ActualHeight;
        if (track <= 0) return;
        var thumb = Math.Max(36, track * _scroller.ViewportHeight / _scroller.ExtentHeight);
        _thumb.Height = thumb;
        _thumb.Margin = new Thickness(0, (track - thumb) * Math.Clamp(offset / _scroller.ScrollableHeight, 0, 1), 0, 0);
    }

    /// <summary>Its opacity to on or off; without the transition when it must land at once.</summary>
    private static void Show(UIElement element, bool on, bool animate)
    {
        var value = on ? 1 : 0;
        if (element.Opacity == value) return;
        if (animate)
        {
            element.Opacity = value;
            return;
        }
        var transition = element.OpacityTransition;
        element.OpacityTransition = null;
        element.Opacity = value;
        element.OpacityTransition = transition;
    }
}
