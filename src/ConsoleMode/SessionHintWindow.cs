using System.Numerics;
using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace ConsoleMode;

/// <summary>
/// A toast in the top-right corner of the game screen, shown once when the session starts:
/// "hold Select + Y to open the panel". It never takes the focus from the game (shown without
/// activation, WS_EX_NOACTIVATE) and closes by itself. Also used for the low-battery notice.
/// It slides in from the screen's right edge, stays while a thin bar runs out, and slides back out the same way:
/// the window reaches the edge and is transparent around the card, so the card comes from behind the edge
/// (and never shows on a screen next to it). Sized like the rest of the interface, in proportion to the screen.
/// </summary>
public sealed class SessionHintWindow : Window
{
    // In the interface's own units (UiScale: a screen 1080 tall).
    private const double CardWidth = 470;
    private const double CardHeight = 96;
    private const double Margin = 28;

    private static readonly TimeSpan EnterTime = TimeSpan.FromMilliseconds(560);
    private static readonly TimeSpan LeaveTime = TimeSpan.FromMilliseconds(420);

    // Nobody holds the window after "new": without this the GC can collect it and its timer
    // before they fire, and the toast then stays on screen and never closes.
    private static readonly HashSet<SessionHintWindow> Live = [];

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;
    private readonly Border _card;
    private readonly Border _bar;
    private readonly TimeSpan _duration;
    /// <summary>The card slides (the window is transparent around it); without a transparent window it only fades.</summary>
    private readonly bool _slides;
    private bool _leaving;

    private float Travel => _slides ? (float)(CardWidth + Margin) : 0;

    public SessionHintWindow(string title, string body, ScreenRect? target, TimeSpan duration, string? glyph = null)
    {
        _duration = duration;
        try
        {
            SystemBackdrop = new TransparentBackdrop();
            _slides = true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aviso: sem janela transparente, só esmaece: {ex.Message}");
        }

        var icon = new FontIcon { Glyph = glyph ?? "", FontSize = 30, VerticalAlignment = VerticalAlignment.Center };
        var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        text.Children.Add(new TextBlock { Text = body, FontSize = 15, TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.85 });
        var row = new Grid { ColumnSpacing = 16 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(text, 1);
        row.Children.Add(icon);
        row.Children.Add(text);

        // How long it stays: a thin bar in the accent colour that runs out, under the text.
        var accent = Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var brush) && brush is Brush b
            ? b : new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x6E, 0xE0, 0xB8));
        _bar = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5),
            Background = accent,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, -8)
        };
        var inside = new Grid();
        inside.Children.Add(row);
        inside.Children.Add(_bar);

        _card = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(_slides ? 14 : 0),
            Background = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x0F, 0x14, 0x18)),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20, 12, 20, 12),
            Child = inside
        };
        // The window reaches the screen's edge: the card, then the margin it slides through.
        var stage = new Grid
        {
            Width = _slides ? CardWidth + Margin : CardWidth,
            Height = CardHeight,
            Background = new SolidColorBrush(Colors.Transparent),
            RequestedTheme = ElementTheme.Dark
        };
        stage.Children.Add(_card);
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = stage };

        // Off-screen (or see-through) until the first frame, so it never flashes in place before sliding in.
        var visual = ElementCompositionPreview.GetElementVisual(_card);
        ElementCompositionPreview.SetIsTranslationEnabled(_card, true);
        visual.Properties.InsertVector3("Translation", new Vector3(Travel, 0, 0));
        visual.Opacity = 0;

        var hwnd = WindowNative.GetWindowHandle(this);
        // Tool window (no taskbar button) that never activates.
        SetWindowLongPtr(hwnd, GwlExStyle, GetWindowLongPtr(hwnd, GwlExStyle) | WsExNoActivate | WsExToolWindow);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        WindowChrome.Strip(hwnd);
        SquareAndBorderless(hwnd);
        if (_slides) SeeThrough(hwnd);
        Place(appWindow, target);
        appWindow.Show(activateWindow: false);

        Live.Add(this);
        Closed += (_, _) =>
        {
            _timer?.Stop();
            Live.Remove(this);
        };

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = EnterTime + duration;
        _timer.IsRepeating = false;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Leave();
        };
        _card.Loaded += (_, _) => Enter();
        _timer.Start();
    }

    /// <summary>
    /// Top-right of the screen, in proportion to it like the interface (UiScale); with the slide, flush with the
    /// right edge so the card comes from behind it.
    /// </summary>
    private void Place(AppWindow appWindow, ScreenRect? target)
    {
        var screen = target is { Width: > 0, Height: > 0 }
            ? new Windows.Graphics.RectInt32(target.X, target.Y, target.Width, target.Height)
            : DisplayArea.Primary.OuterBounds;
        var scale = UiScale.For(screen.Width, screen.Height);
        var width = (int)Math.Round((_slides ? CardWidth + Margin : CardWidth) * scale);
        var height = (int)Math.Round(CardHeight * scale);
        var margin = (int)Math.Round(Margin * scale);
        var left = screen.X + screen.Width - width - (_slides ? 0 : margin);
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(left, screen.Y + margin, width, height));
    }

    /// <summary>In from the right, easing out, while it fades in; then the bar starts running out.</summary>
    private void Enter()
    {
        var visual = ElementCompositionPreview.GetElementVisual(_card);
        var compositor = visual.Compositor;
        var easeOut = compositor.CreateCubicBezierEasingFunction(new Vector2(0.16f, 1f), new Vector2(0.3f, 1f));

        var slide = compositor.CreateScalarKeyFrameAnimation();
        slide.InsertKeyFrame(0, Travel);
        slide.InsertKeyFrame(1, 0, easeOut);
        slide.Duration = EnterTime;
        visual.StartAnimation("Translation.X", slide);

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, compositor.CreateLinearEasingFunction());
        fade.Duration = TimeSpan.FromMilliseconds(EnterTime.TotalMilliseconds * 0.6);
        visual.StartAnimation("Opacity", fade);

        var bar = ElementCompositionPreview.GetElementVisual(_bar);
        bar.CenterPoint = new Vector3(0, 0, 0);
        var runOut = compositor.CreateScalarKeyFrameAnimation();
        runOut.InsertKeyFrame(0, 1);
        runOut.InsertKeyFrame(1, 0, compositor.CreateLinearEasingFunction());
        runOut.Duration = _duration;
        runOut.DelayTime = EnterTime;
        runOut.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        bar.StartAnimation("Scale.X", runOut);
    }

    /// <summary>Back out to the right, the way it came, easing in; closes once it is gone.</summary>
    private void Leave()
    {
        if (_leaving) return;
        _leaving = true;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(_card);
            var compositor = visual.Compositor;
            var easeIn = compositor.CreateCubicBezierEasingFunction(new Vector2(0.7f, 0f), new Vector2(0.84f, 0f));
            var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

            var slide = compositor.CreateScalarKeyFrameAnimation();
            slide.InsertKeyFrame(0, 0);
            slide.InsertKeyFrame(1, Travel, easeIn);
            slide.Duration = LeaveTime;
            visual.StartAnimation("Translation.X", slide);

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0, 1);
            // Sliding, it stays solid until it is mostly behind the edge; without the slide, the fade is the exit.
            if (_slides) fade.InsertKeyFrame(0.6f, 1);
            fade.InsertKeyFrame(1, 0);
            fade.Duration = LeaveTime;
            visual.StartAnimation("Opacity", fade);

            batch.End();
            batch.Completed += (_, _) => CloseQuietly();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aviso: animação de saída: {ex.Message}");
            CloseQuietly();
        }
    }

    private void CloseQuietly()
    {
        try { Close(); }
        catch (Exception ex) { AppLog.Write($"Menu da sessão: aviso não fechou: {ex.Message}"); }
    }

    /// <summary>No rounded window corners and no 1 px Windows outline: only the card is drawn.</summary>
    private static void SquareAndBorderless(nint hwnd)
    {
        try
        {
            var square = 1; // DWMWCP_DONOTROUND
            DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref square, sizeof(int));
            var none = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
            DwmSetWindowAttribute(hwnd, 34 /* DWMWA_BORDER_COLOR */, ref none, sizeof(int));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aviso: cantos: {ex.Message}");
        }
    }

    /// <summary>
    /// The frame extended over the whole window: Windows then lets what is behind show through where nothing is drawn,
    /// instead of the black of the window's own surface (the transparent backdrop alone leaves that black).
    /// </summary>
    private static void SeeThrough(nint hwnd)
    {
        try
        {
            var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            Marshal.ThrowExceptionForHR(DwmExtendFrameIntoClientArea(hwnd, ref margins));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aviso: transparência: {ex.Message}");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left, Right, Top, Bottom;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

    private const int GwlExStyle = -20;
    private const nint WsExNoActivate = 0x08000000;
    private const nint WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}
