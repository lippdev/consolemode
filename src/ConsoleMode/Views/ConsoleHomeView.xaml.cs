using System.ComponentModel;
using System.Numerics;
using Microsoft.UI.Xaml.Hosting;
using ConsoleMode.Services;
using ConsoleMode.Controls;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace ConsoleMode.Views;

public sealed partial class ConsoleHomeView : UserControl
{
    public MainViewModel ViewModel { get; }

    private GamepadNavigator? _navigator;
    private bool _windowActive = true;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _clock;
    private static Style? _cardStyle;
    private static Style? _launchTileStyle;
    private Control? _lastSettingsRow;
    /// <summary>When a category was last pressed with the mouse / touch: that click picks it, A goes into its options.</summary>
    private long _railPointerPressedAt = long.MinValue / 2;
    private int _shownTab;
    private static readonly Windows.UI.ViewManagement.UISettings Ui = new();

    public ConsoleHomeView()
    {
        ViewModel = App.ViewModel!;
        InitializeComponent();
        HomeShortcutPickerHost.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Home, consoleStyle: true);
        MenuShortcutPickerHost.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Menu, consoleStyle: true);
        ExitShortcutPickerHost.Content = new ShortcutPicker(ViewModel, ShortcutSlot.Exit, consoleStyle: true);
        Loaded += OnLoaded;
        Unloaded += (_, _) => { _navigator?.Dispose(); _navigator = null; _clock?.Stop(); };
        // Preview (tunnelling): a ScrollViewer would otherwise swallow the arrows to scroll the page.
        Root.PreviewKeyDown += OnKeyDown;
        // The focused card grows a little, like the Xbox home; on the Home row it also picks the launcher.
        Root.GotFocus += (_, e) => { ScaleTile(e.OriginalSource, 1.07f); PickFocusedLauncher(e.OriginalSource); };
        Root.LostFocus += (_, e) => ScaleTile(e.OriginalSource, 1f);
        // System tab: remember the option in use, so closing a picker lands back on it.
        SettingsRows.GotFocus += (_, e) =>
        {
            if (e.OriginalSource is not Control row) return;
            _lastSettingsRow = row;
            // What the row does, under the list: the descriptions the settings always had.
            RowHelp.Text = Microsoft.UI.Xaml.Automation.AutomationProperties.GetHelpText(row);
        };
        CategoryRail.GotFocus += (_, _) => RowHelp.Text = "";
        CategoryRail.GettingFocus += OnRailGettingFocus;
        CategoryRail.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => _railPointerPressedAt = Environment.TickCount64), true);
        // The sounds are the pad's (GamepadNavigator, LB / RB): mouse clicks stay silent.
        ViewModel.PropertyChanged += OnViewModelChanged;
        if (ViewModel.IsIntroShowing) ShowIntro();
        ViewModel.PickBackgroundImageRequested += () => _ = PickBackgroundImageAsync();
    }

    /// <summary>"My own image": the system file picker (needs the window handle in WinUI 3).</summary>
    private async Task PickBackgroundImageAsync()
    {
        try
        {
            if (App.MainWindowInstance is not { } window) return;
            var picker = new Windows.Storage.Pickers.FileOpenPicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary };
            foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" }) picker.FileTypeFilter.Add(extension);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            var file = await picker.PickSingleFileAsync();
            if (file is not null) ViewModel.SetBackgroundImage(file.Path);
            else if (!SteamArt.IsSupportedImage(ViewModel.ConsoleBackgroundImage)) ViewModel.CycleBackgroundMode();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Fundo: seletor de imagem: {ex.Message}");
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigator ??= new GamepadNavigator(DispatcherQueue, this,
            App.MainWindowInstance is { } w ? WinRT.Interop.WindowNative.GetWindowHandle(w) : 0,
            readSonyHid: true);
        _navigator.SearchRoot = () =>
            ViewModel.IsConsoleTourOpen ? ConsoleTourPanel
            : ViewModel.IsControllerTestOpen ? ControllerTestPanel
            : ViewModel.IsShortcutEditorOpen ? ShortcutEditorPanel
            : ViewModel.IsPickerOpen ? PickerPanel
            : ViewModel.IsConsoleSettingsOpen ? SettingsPanel
            : ViewModel.IsRolePanelOpen ? RolePanel
            : Root;
        _navigator.Sounds = true;
        _navigator.BackRequested += GoBack;
        _navigator.Intercept = action =>
        {
            // Any press on the opening skips it, and does nothing else.
            if (ViewModel.IsIntroShowing) { ViewModel.SkipIntro(); return true; }
            if (!ViewModel.IsCapturingShortcut) return false;
            return true;
        };
        _navigator.TabRequested += StepTab;
        _navigator.MenuRequested += () =>
        {
            if (ViewModel.CanStart && !ViewModel.IsRolePanelOpen && !ViewModel.IsConsoleSettingsOpen
                && !ViewModel.IsPickerOpen && !ViewModel.IsShortcutEditorOpen && !ViewModel.IsConsoleTourOpen
                && !ViewModel.IsControllerTestOpen && !ViewModel.IsUpdateOpen)
                ViewModel.StartCommand.Execute(null);
        };
        _navigator.AltRequested += () =>
        {
            if (ViewModel.IsRolePanelOpen || ViewModel.IsPickerOpen || ViewModel.IsShortcutEditorOpen || ViewModel.IsConsoleTourOpen || ViewModel.IsControllerTestOpen || ViewModel.IsUpdateOpen) return;
            if (ViewModel.IsConsoleSettingsOpen) ViewModel.CloseConsoleSettingsCommand.Execute(null);
            else ViewModel.OpenFullSettingsCommand.Execute(null);
        };
        if (App.MainWindowInstance is { } window)
            window.Activated += (_, args) =>
            {
                _windowActive = args.WindowActivationState != WindowActivationState.Deactivated;
                RefreshNavigator();
            };
        RefreshNavigator();
        StartClock();
        FocusDefault();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsConsoleActive):
                RefreshNavigator();
                DispatcherQueue.TryEnqueue(FocusDefault);
                break;
            case nameof(MainViewModel.IsUpdateOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsUpdateOpen) UpdateNowButton.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsRolePanelOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsRolePanelOpen) RolePlay.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsPickerOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsPickerOpen) FocusPickerSelection();
                    else if (ViewModel.IsConsoleSettingsOpen) FocusSystemRow();
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsControllerTestOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsControllerTestOpen) CopyDiagnosticsButton.Focus(FocusState.Keyboard);
                    else if (ViewModel.IsConsoleSettingsOpen) FocusSystemRow();
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsShortcutEditorOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsShortcutEditorOpen)
                        FocusShortcutEditorTarget();
                    else
                    {
                        (HomeShortcutPickerHost.Content as ShortcutPicker)?.CancelCapture();
                        (MenuShortcutPickerHost.Content as ShortcutPicker)?.CancelCapture();
                        (ExitShortcutPickerHost.Content as ShortcutPicker)?.CancelCapture();
                        FocusShortcutEditorRow();
                    }
                    RefreshNavigator();
                });
                break;
            case nameof(MainViewModel.IsIntroShowing):
                if (ViewModel.IsIntroShowing) ShowIntro();
                else HideIntro();
                break;
            case nameof(MainViewModel.IsConsoleTourOpen):
                if (!ViewModel.IsConsoleTourOpen) DispatcherQueue.TryEnqueue(FocusDefault);
                break;
            case nameof(MainViewModel.ConsoleTourIndex):
                // After the tab switch (queued by ConsoleTabIndex) has laid the new page out.
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ShowTourStop);
                break;
            case MainViewModel.ShortcutsProperty:
                RefreshNavigator();
                break;
            case nameof(MainViewModel.ConsoleTabIndex):
                DispatcherQueue.TryEnqueue(() =>
                {
                    // The new page was just made visible: lay it out first, or nothing in it can take the focus yet
                    // (the focus would stay on whatever is left visible, like a button in the top bar).
                    TabPanel(ViewModel.ConsoleTabIndex).UpdateLayout();
                    AnimateTab(_shownTab, ViewModel.ConsoleTabIndex);
                    _shownTab = ViewModel.ConsoleTabIndex;
                    FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsConsoleSettingsOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsConsoleSettingsOpen) FocusSystemCategory();
                    else FocusDefault();
                });
                break;
        }
    }

    /// <summary>
    /// Only listen while the window is in front: A pressed inside Big Picture must not click something
    /// in a hidden window. (XInput reads without focus.)
    /// </summary>
    private void RefreshNavigator()
    {
        if (_navigator is null) return;
        var listen = _windowActive;
        if (listen && !_navigator.IsRunning) _navigator.Start();
        else if (!listen && _navigator.IsRunning) _navigator.Stop();
    }

    private void FocusDefault()
    {
        if (ViewModel.IsConsoleActive) { RestoreButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsUpdateOpen) { UpdateNowButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsConsoleTourOpen) { ConsoleTourNextButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsShortcutEditorOpen) { FocusShortcutEditorTarget(); return; }
        if (ViewModel.IsSystemTab) { FocusSystemCategory(); return; }
        if (ViewModel.IsSessionTab) { (FocusManager.FindFirstFocusableElement(ScreenCards) as Control)?.Focus(FocusState.Keyboard); return; }
        if (ViewModel.CanStart) { PlayButton.Focus(FocusState.Keyboard); return; }
        (FocusManager.FindFirstFocusableElement(ScreenCards) as Control)?.Focus(FocusState.Keyboard);
    }

    /// <summary>Land on the current value so A confirms it and the stick moves from there.</summary>
    private void FocusPickerSelection()
    {
        var index = Math.Max(ViewModel.PickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
        if (TryFocusPickerRow(index)) return;
        // Containers appear on the next layout pass; try once more then.
        void OnLayout(object? s, object e)
        {
            PickerList.LayoutUpdated -= OnLayout;
            TryFocusPickerRow(index);
        }
        PickerList.LayoutUpdated += OnLayout;
    }

    private bool TryFocusPickerRow(int index)
    {
        if (PickerList.ContainerFromIndex(index) is not { } container) return false;
        return (FocusManager.FindFirstFocusableElement(container) as Control)?.Focus(FocusState.Keyboard) == true;
    }

    private FrameworkElement TabPanel(int tab) => tab switch
    {
        MainViewModel.HomeTabIndex => HomePanel,
        MainViewModel.SessionTabIndex => SessionPanel,
        _ => SettingsPanel
    };

    /// <summary>
    /// The page that just appeared slides in from the side the tab came from (RB: right, LB: left)
    /// while it fades in. Skipped when Windows has animations turned off.
    /// </summary>
    private void AnimateTab(int from, int to)
    {
        if (from == to || !Ui.AnimationsEnabled) return;
        var panel = TabPanel(to);
        try
        {
            ElementCompositionPreview.SetIsTranslationEnabled(panel, true);
            var visual = ElementCompositionPreview.GetElementVisual(panel);
            var compositor = visual.Compositor;
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

            var slide = compositor.CreateScalarKeyFrameAnimation();
            slide.InsertKeyFrame(0f, to > from ? 56f : -56f);
            slide.InsertKeyFrame(1f, 0f, easing);
            slide.Duration = TimeSpan.FromMilliseconds(260);
            visual.StartAnimation("Translation.X", slide);

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 0f);
            fade.InsertKeyFrame(1f, 1f, easing);
            fade.Duration = TimeSpan.FromMilliseconds(220);
            visual.StartAnimation("Opacity", fade);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Abas: animação: {ex.Message}");
        }
    }

    /// <summary>LB / RB: the previous / next tab, unless a panel is open over the page.</summary>
    private void StepTab(int step)
    {
        if (ViewModel.IsConsoleActive || ViewModel.IsRolePanelOpen || ViewModel.IsPickerOpen || ViewModel.IsShortcutEditorOpen || ViewModel.IsConsoleTourOpen || ViewModel.IsControllerTestOpen) return;
        if (ViewModel.StepConsoleTab(step)) UiSounds.Play(UiSound.Tab);
    }

    private void GoBack()
    {
        if (ViewModel.IsControllerTestOpen) ViewModel.CloseControllerTestCommand.Execute(null);
        else if (ViewModel.IsPickerOpen) ViewModel.ClosePickerCommand.Execute(null);
        else if (ViewModel.IsShortcutEditorOpen) ViewModel.CloseShortcutEditorCommand.Execute(null);
        else if (ViewModel.IsConsoleTourOpen) ViewModel.EndTourCommand.Execute(null);
        else if (ViewModel.IsRolePanelOpen) ViewModel.CloseRolePanelCommand.Execute(null);
        else if (ViewModel.IsSystemTab && IsFocusInside(SettingsRows)) FocusSystemCategory();
        else if (!ViewModel.IsHomeTab) ViewModel.SelectConsoleTabCommand.Execute("0");
        else if (ViewModel.IsUpdateOpen && !ViewModel.IsUpdating) ViewModel.DismissUpdateCommand.Execute(null);
    }

    private void FocusShortcutEditorTarget()
    {
        // Visibility changed before the dispatch callback; measure the cards before focusing their buttons.
        ShortcutEditorPanel.UpdateLayout();
        var picker = ViewModel.ShortcutEditorSlot switch
        {
            "home" => HomeShortcutPickerHost.Content as ShortcutPicker,
            "menu" => MenuShortcutPickerHost.Content as ShortcutPicker,
            "exit" => ExitShortcutPickerHost.Content as ShortcutPicker,
            _ => null
        };
        if (picker?.FocusCaptureButton() != true)
            (FocusManager.FindFirstFocusableElement(ShortcutEditorPanel) as Control)?.Focus(FocusState.Keyboard);
    }

    private void FocusShortcutEditorRow()
    {
        var row = ViewModel.ShortcutEditorSlot switch
        {
            "home" => HomeShortcutRow,
            "menu" => MenuShortcutRow,
            "exit" => ExitShortcutRow,
            _ => null
        };
        if (ViewModel.IsSystemTab && row?.Focus(FocusState.Keyboard) == true) return;
        FocusDefault();
    }

    private Button CategoryButton(int index) => (Button)CategoryRail.Children[Math.Clamp(index, 0, CategoryRail.Children.Count - 1)];

    private StackPanel SystemSection(int index) => index switch
    {
        0 => SystemSection0,
        1 => SystemSection1,
        2 => SystemSection2,
        3 => SystemSection3,
        4 => SystemSection4,
        5 => SystemSection5,
        6 => SystemSection6,
        _ => SystemSection7
    };

    private void FocusSystemCategory()
    {
        // The tour opens the System tab under itself: the focus stays on its card.
        if (ViewModel.IsConsoleTourOpen) { ConsoleTourNextButton.Focus(FocusState.Keyboard); return; }
        CategoryButton(ViewModel.SystemCategory).Focus(FocusState.Keyboard);
    }

    // ───────────── Opening ─────────────

    private void OnIntroTapped(object sender, TappedRoutedEventArgs e) => ViewModel.SkipIntro();

    /// <summary>
    /// "Aproximação": the logo starts three times its size, blurred and clear, and settles sharp in the middle (the blurred
    /// copy fades into the sharp one on the way), then the name fades in.
    /// </summary>
    private void ShowIntro()
    {
        IntroPanel.Visibility = Visibility.Visible;
        if (!Ui.AnimationsEnabled) return;
        try
        {
            var panel = ElementCompositionPreview.GetElementVisual(IntroPanel);
            panel.Opacity = 1f;
            var box = ElementCompositionPreview.GetElementVisual(IntroLogoBox);
            var compositor = box.Compositor;
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.2f, 1f));
            box.CenterPoint = new Vector3((float)IntroLogoBox.Width / 2, (float)IntroLogoBox.Height / 2, 0);
            var approach = compositor.CreateVector3KeyFrameAnimation();
            approach.InsertKeyFrame(0f, new Vector3(3f, 3f, 1f));
            approach.InsertKeyFrame(1f, Vector3.One, easing);
            approach.Duration = TimeSpan.FromMilliseconds(900);
            box.StartAnimation("Scale", approach);
            FadeIn(box, 0, 500);
            FadeIn(ElementCompositionPreview.GetElementVisual(IntroLogo), 0, 900);
            var blur = ElementCompositionPreview.GetElementVisual(IntroLogoBlur);
            var unblur = compositor.CreateScalarKeyFrameAnimation();
            unblur.InsertKeyFrame(0f, 1f);
            unblur.InsertKeyFrame(1f, 0f, easing);
            unblur.Duration = TimeSpan.FromMilliseconds(900);
            blur.StartAnimation("Opacity", unblur);
            FadeIn(ElementCompositionPreview.GetElementVisual(IntroName), 600, 500);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Abertura: animação: {ex.Message}");
        }

        static void FadeIn(Microsoft.UI.Composition.Visual visual, int delayMs, int ms)
        {
            var fade = visual.Compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 0f);
            fade.InsertKeyFrame(1f, 1f);
            fade.DelayTime = TimeSpan.FromMilliseconds(delayMs);
            fade.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
            fade.Duration = TimeSpan.FromMilliseconds(ms);
            visual.StartAnimation("Opacity", fade);
        }
    }

    /// <summary>The opening fades out over the interface, which is ready underneath (the tour too, the first time).</summary>
    private void HideIntro()
    {
        if (IntroPanel.Visibility == Visibility.Collapsed) return;
        void Done()
        {
            IntroPanel.Visibility = Visibility.Collapsed;
            FocusDefault();
        }
        if (!Ui.AnimationsEnabled) { Done(); return; }
        try
        {
            var panel = ElementCompositionPreview.GetElementVisual(IntroPanel);
            var batch = panel.Compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
            var fade = panel.Compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 1f);
            fade.InsertKeyFrame(1f, 0f);
            fade.Duration = TimeSpan.FromMilliseconds(380);
            panel.StartAnimation("Opacity", fade);
            batch.End();
            batch.Completed += (_, _) => DispatcherQueue.TryEnqueue(Done);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Abertura: animação: {ex.Message}");
            Done();
        }
    }

    // ───────────── First-run tour ─────────────

    private const double TourHolePadding = 14;
    private const double TourEdge = 40;
    private const double TourGap = 28;

    /// <summary>The part of the screen a tour stop is about, or null for the stops that only talk.</summary>
    private Rect? TourTarget(ConsoleTourStop stop)
    {
        FrameworkElement[] parts = stop switch
        {
            ConsoleTourStop.Tabs => [TabBar],
            ConsoleTourStop.Launchers => [LaunchRow],
            // What will open, where and what changes, and the play button: the text talks about the line above it.
            ConsoleTourStop.Play => [HomeSummary],
            ConsoleTourStop.Screens => [ScreenCards],
            ConsoleTourStop.QuickSettings => [QuickCards],
            ConsoleTourStop.System => [CategoryRail],
            ConsoleTourStop.Shortcuts => [HomeShortcutRow, ExitShortcutRow],
            _ => []
        };
        Rect? area = null;
        foreach (var part in parts)
        {
            if (part.ActualWidth <= 0 || part.ActualHeight <= 0) continue;
            var bounds = part.TransformToVisual(ConsoleTourPanel).TransformBounds(new Rect(0, 0, part.ActualWidth, part.ActualHeight));
            if (area is { } union) { union.Union(bounds); area = union; }
            else area = bounds;
        }
        return area;
    }

    /// <summary>A stop came up: the page under it is laid out, so draw the hole and place the card, then focus Next.</summary>
    private void ShowTourStop()
    {
        if (!ViewModel.IsConsoleTourOpen) return;
        if (TourTargetElement(ViewModel.ConsoleTourStopNow) is { } part)
        {
            // The quick settings sit low on the Session page: scroll them in before measuring.
            part.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        }
        TabPanel(ViewModel.ConsoleTabIndex).UpdateLayout();
        ConsoleTourPanel.UpdateLayout();
        PlaceTour();
        FadeInTourCard();
        ConsoleTourNextButton.Focus(FocusState.Keyboard);
    }

    private FrameworkElement? TourTargetElement(ConsoleTourStop stop) => stop switch
    {
        ConsoleTourStop.QuickSettings => QuickCards,
        ConsoleTourStop.Screens => ScreenCards,
        ConsoleTourStop.Shortcuts => HomeShortcutRow,
        _ => null
    };

    private void OnTourPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ViewModel.IsConsoleTourOpen) PlaceTour();
    }

    /// <summary>The veil with its hole, the ring around the hole and the card beside it (below, above, beside or centred).</summary>
    private void PlaceTour()
    {
        var width = ConsoleTourPanel.ActualWidth;
        var height = ConsoleTourPanel.ActualHeight;
        if (width <= 0 || height <= 0) return;

        var stop = ViewModel.ConsoleTourStopNow;
        var target = ConsoleTour.IsCentered(stop) ? null : TourTarget(stop);
        Rect? hole = null;
        if (target is { } t)
        {
            var left = Math.Max(t.X - TourHolePadding, 8);
            var top = Math.Max(t.Y - TourHolePadding, 8);
            var right = Math.Min(t.X + t.Width + TourHolePadding, width - 8);
            var bottom = Math.Min(t.Y + t.Height + TourHolePadding, height - 8);
            if (right > left && bottom > top) hole = new Rect(left, top, right - left, bottom - top);
        }

        var veil = new GeometryGroup { FillRule = FillRule.EvenOdd };
        veil.Children.Add(new RectangleGeometry { Rect = new Rect(0, 0, width, height) });
        if (hole is { } h)
        {
            veil.Children.Add(RoundedRectangle(h, 20));
            TourRing.Margin = new Thickness(h.X, h.Y, 0, 0);
            TourRing.Width = h.Width;
            TourRing.Height = h.Height;
            TourRing.Visibility = Visibility.Visible;
        }
        else TourRing.Visibility = Visibility.Collapsed;
        TourVeil.Data = veil;

        var cardWidth = Math.Min(600, width - 2 * TourEdge);
        TourCard.Width = cardWidth;
        TourCard.Measure(new Size(cardWidth, double.PositiveInfinity));
        // DesiredSize counts the margin, which is where the card was put last time.
        var cardHeight = TourCard.DesiredSize.Height - TourCard.Margin.Top - TourCard.Margin.Bottom;
        double x, y;
        if (hole is not { } spot)
        {
            x = (width - cardWidth) / 2;
            y = (height - cardHeight) / 2;
        }
        else if (spot.Bottom + TourGap + cardHeight <= height - TourEdge)
        {
            x = spot.X;
            y = spot.Bottom + TourGap;
        }
        else if (spot.Y - TourGap - cardHeight >= TourEdge)
        {
            x = spot.X;
            y = spot.Y - TourGap - cardHeight;
        }
        else if (spot.Right + TourGap + cardWidth <= width - TourEdge)
        {
            x = spot.Right + TourGap;
            y = spot.Y;
        }
        else if (spot.X - TourGap - cardWidth >= TourEdge)
        {
            x = spot.X - TourGap - cardWidth;
            y = spot.Y;
        }
        else
        {
            x = (width - cardWidth) / 2;
            y = height - TourEdge - cardHeight;
        }
        x = Math.Clamp(x, TourEdge, Math.Max(TourEdge, width - cardWidth - TourEdge));
        y = Math.Clamp(y, TourEdge, Math.Max(TourEdge, height - cardHeight - TourEdge));
        TourCard.Margin = new Thickness(x, y, 0, 0);
    }

    private static PathGeometry RoundedRectangle(Rect r, double radius)
    {
        radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
        var corner = new Size(radius, radius);
        var figure = new PathFigure { StartPoint = new Point(r.X + radius, r.Y), IsClosed = true, IsFilled = true };
        void Line(double px, double py) => figure.Segments.Add(new LineSegment { Point = new Point(px, py) });
        void Arc(double px, double py) => figure.Segments.Add(new ArcSegment { Point = new Point(px, py), Size = corner, SweepDirection = SweepDirection.Clockwise });
        Line(r.Right - radius, r.Y);
        Arc(r.Right, r.Y + radius);
        Line(r.Right, r.Bottom - radius);
        Arc(r.Right - radius, r.Bottom);
        Line(r.X + radius, r.Bottom);
        Arc(r.X, r.Bottom - radius);
        Line(r.X, r.Y + radius);
        Arc(r.X + radius, r.Y);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    /// <summary>Each stop's card fades in where it lands. Skipped when Windows has animations turned off.</summary>
    private void FadeInTourCard()
    {
        if (!Ui.AnimationsEnabled) return;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(TourCard);
            var fade = visual.Compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 0f);
            fade.InsertKeyFrame(1f, 1f);
            fade.Duration = TimeSpan.FromMilliseconds(220);
            visual.StartAnimation("Opacity", fade);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Tour: animação: {ex.Message}");
        }
    }

    /// <summary>Back to the option that was in use, else the first one of the category, else the category itself.</summary>
    private void FocusSystemRow()
    {
        if (_lastSettingsRow?.Focus(FocusState.Keyboard) == true) return;
        var section = SystemSection(ViewModel.SystemCategory);
        section.UpdateLayout();
        if ((FocusManager.FindFirstFocusableElement(section) as Control)?.Focus(FocusState.Keyboard) == true) return;
        FocusSystemCategory();
    }

    /// <summary>Moving over a category shows its options, like a console's settings.</summary>
    private void OnCategoryFocused(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } || !int.TryParse(tag, out var index) || index == ViewModel.SystemCategory) return;
        ViewModel.SystemCategory = index;
        _lastSettingsRow = null;
        SettingsRows.ChangeView(null, 0, null, true);
    }

    /// <summary>A on a category goes into its options; a mouse click only picks the category (its focus already did).</summary>
    private void OnCategoryClick(object sender, RoutedEventArgs e)
    {
        if (Environment.TickCount64 - _railPointerPressedAt < 1500) return;
        _lastSettingsRow = null;
        FocusSystemRow();
    }

    /// <summary>Coming back from the options (left), land on the category they belong to, not the nearest one.
    /// Only for the pad and the keyboard: a click on another category must reach it.</summary>
    private void OnRailGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (args.FocusState == FocusState.Pointer || args.InputDevice is FocusInputDeviceKind.Mouse or FocusInputDeviceKind.Touch or FocusInputDeviceKind.Pen) return;
        var current = CategoryButton(ViewModel.SystemCategory);
        if (args.OldFocusedElement is DependencyObject old && IsInside(old, SettingsRows) && !ReferenceEquals(args.NewFocusedElement, current))
            args.TrySetNewFocusedElement(current);
    }

    private bool IsFocusInside(DependencyObject container) =>
        Root.XamlRoot is { } root && FocusManager.GetFocusedElement(root) is DependencyObject focused && IsInside(focused, container);

    private static bool IsInside(DependencyObject node, DependencyObject container)
    {
        for (var current = node; current is not null; current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current))
            if (ReferenceEquals(current, container)) return true;
        return false;
    }

    private void StartClock()
    {
        _clock ??= DispatcherQueue.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(15);
        _clock.Tick -= OnClockTick;
        _clock.Tick += OnClockTick;
        OnClockTick(null!, null!);
        _clock.Start();
    }

    private void OnClockTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args) =>
        ClockText.Text = DateTime.Now.ToString("HH:mm");

    /// <summary>Like a console's game row: the launcher under the focus is the one that opens.</summary>
    private void PickFocusedLauncher(object source)
    {
        if (source is Button { DataContext: LaunchTile tile }) ViewModel.SelectLaunchTileCommand.Execute(tile);
    }

    /// <summary>Cards (not settings rows) grow when focused and shrink back when it leaves; the launchers grow more.</summary>
    private static void ScaleTile(object source, float scale)
    {
        // The screens on Session sit side by side as on the desk: growing one would cover its neighbour.
        if (source is not Button button || button.Tag is "ScreenTile") return;
        _cardStyle ??= Application.Current.Resources["ConsoleCard"] as Style;
        _launchTileStyle ??= Application.Current.Resources["LaunchTileButton"] as Style;
        if (ReferenceEquals(button.Style, _launchTileStyle)) scale = scale > 1f ? 1.14f : 1f;
        else if (!ReferenceEquals(button.Style, _cardStyle)) return;
        button.CenterPoint = new Vector3((float)button.ActualWidth / 2, (float)button.ActualHeight / 2, 0);
        button.ScaleTransition ??= new Vector3Transition { Duration = TimeSpan.FromMilliseconds(140) };
        button.Scale = new Vector3(scale, scale, 1);
    }

    private static Microsoft.UI.Xaml.Input.FocusNavigationDirection? Arrow(VirtualKey key) => key switch
    {
        VirtualKey.Up => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up,
        VirtualKey.Down => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Down,
        VirtualKey.Left => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Left,
        VirtualKey.Right => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Right,
        _ => null
    };

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (ViewModel.IsIntroShowing)
        {
            ViewModel.SkipIntro();
            e.Handled = true;
            return;
        }
        // The pad is read by the navigator (polling), never through these key events, or every press
        // would count twice. The keyboard goes through the same navigator, so both move the same way.
        if (e.Key is VirtualKey.Escape)
        {
            GoBack();
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.PageUp)
        {
            StepTab(-1);
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.PageDown)
        {
            StepTab(1);
            e.Handled = true;
        }
        else if (Root.XamlRoot is { } root && FocusManager.GetFocusedElement(root) is TextBox
                 && (e.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down))
        {
            // Let text inputs move the caret/selection; the console menu only handles arrows elsewhere.
            return;
        }
        else if (_navigator is not null && Arrow(e.Key) is { } direction)
        {
            _navigator.Navigate(direction);
            e.Handled = true;
        }
    }
}
