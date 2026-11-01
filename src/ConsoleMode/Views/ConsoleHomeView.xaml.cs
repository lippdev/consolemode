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
    private readonly ScrollEdges _rowsEdges;
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
        TaskViewShortcutPickerHost.Content = new ShortcutPicker(ViewModel, ShortcutSlot.TaskView, consoleStyle: true);
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
        // The list's edges and its scroll thumb follow the scrolling, the window's size and the category shown.
        _rowsEdges = new ScrollEdges(SettingsRows, RowsFadeTop, RowsFadeBottom, RowsScrollTrack, RowsScrollThumb);
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
            case nameof(MainViewModel.IsSystemTab) when ViewModel.IsSystemTab:
                // Entering System: the list's edges land with it, never a hard cut that then fades.
                _rowsEdges.Snap();
                break;
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
                        (TaskViewShortcutPickerHost.Content as ShortcutPicker)?.CancelCapture();
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
            "taskview" => TaskViewShortcutPickerHost.Content as ShortcutPicker,
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
            "taskview" => TaskViewShortcutRow,
            _ => null
        };
        if (ViewModel.IsSystemTab && row?.Focus(FocusState.Keyboard) == true) return;
        FocusDefault();
    }

    private Button CategoryButton(int index) => (Button)CategoryRail.Children[Math.Clamp(index, 0, CategoryRail.Children.Count - 1)];

    private Panel SystemSection(int index) => index switch
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

    /// <summary>Shows the opening in the style it was started with (BootIntro): one layer each, animated by Composition.</summary>
    private void ShowIntro()
    {
        var style = ViewModel.IntroStyleNow;
        IntroConsole.Visibility = style == BootIntro.ConsoleStyle ? Visibility.Visible : Visibility.Collapsed;
        IntroX360.Visibility = style == BootIntro.Xbox360Style ? Visibility.Visible : Visibility.Collapsed;
        IntroPs4.Visibility = style == BootIntro.Ps4Style ? Visibility.Visible : Visibility.Collapsed;
        IntroPanel.Visibility = Visibility.Visible;
        // Without animations, the style's background and logo just show (no flash over them, the logo drawn whole).
        if (!Ui.AnimationsEnabled) { Visual(X360Flash).Opacity = 0f; ShowConsoleLogoWhole(); return; }
        try
        {
            ElementCompositionPreview.GetElementVisual(IntroPanel).Opacity = 1f;
            switch (style)
            {
                case BootIntro.Xbox360Style: ShowX360Intro(); break;
                case BootIntro.Ps4Style: ShowPs4Intro(); break;
                default: ShowConsoleIntro(); break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Abertura: animação: {ex.Message}");
        }
    }

    /// <summary>
    /// "Liga o PC": everything starts dark; the power light on the base comes on and pulses, the base shows, the screen
    /// draws itself and flashes green, the >_ is typed and the cursor blinks, then the name.
    /// </summary>
    private void ShowConsoleIntro()
    {
        Fade(Visual(LogoPower), 0, 1, 200, 150);
        Pulse(Visual(LogoPowerGlow), 350, 900, 2);
        Fade(Visual(LogoBase), 0, 1, 700, 400);
        Fade(Visual(LogoDash), 0, 1, 900, 200);
        var draw = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        Draw(draw, LogoScreen, 40, 900, 700);
        Draw(draw, LogoChevron, 7.2, 1750, 300);
        Draw(draw, LogoCursor, 3.8, 2050, 200);
        draw.Begin();
        // A hidden line still shows its round caps as dots: the prompt stays out of sight until it is typed.
        Fade(Visual(LogoChevron), 0, 1, 1750, 1);
        Fade(Visual(LogoCursorBox), 0, 1, 2050, 1);
        var flash = Visual(LogoScreenFill);
        var glow = flash.Compositor.CreateScalarKeyFrameAnimation();
        glow.InsertKeyFrame(0f, 0f);
        glow.InsertKeyFrame(0.25f, 0.35f);
        glow.InsertKeyFrame(1f, 0f);
        glow.DelayTime = TimeSpan.FromMilliseconds(1500);
        glow.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        glow.Duration = TimeSpan.FromMilliseconds(900);
        flash.StartAnimation("Opacity", glow);
        Blink(Visual(LogoCursor), 2300, 500, 3);
        Fade(Visual(IntroGlow), 0, 0.55f, 1500, 800);
        Fade(Visual(IntroName), 0, 1, 2400, 500);
    }

    /// <summary>The logo of the app's own opening as it ends: for Windows' animations off.</summary>
    private void ShowConsoleLogoWhole()
    {
        LogoScreen.StrokeDashOffset = 0;
        LogoChevron.StrokeDashOffset = 0;
        LogoCursor.StrokeDashOffset = 0;
        Visual(LogoScreenFill).Opacity = 0f;
        Visual(LogoPowerGlow).Opacity = 0f;
    }

    /// <summary>Draws a line along itself: its one dash (as long as the line) slides in from the offset that hides it.</summary>
    private static void Draw(Microsoft.UI.Xaml.Media.Animation.Storyboard storyboard, Microsoft.UI.Xaml.Shapes.Shape shape, double length, int beginMs, int ms)
    {
        shape.StrokeDashOffset = length;
        var slide = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = length,
            To = 0,
            BeginTime = TimeSpan.FromMilliseconds(beginMs),
            Duration = TimeSpan.FromMilliseconds(ms),
            EnableDependentAnimation = true,
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(slide, shape);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(slide, "StrokeDashOffset");
        storyboard.Children.Add(slide);
    }

    /// <summary>A light that comes up and dies down, a few times (the power light).</summary>
    private static void Pulse(Microsoft.UI.Composition.Visual visual, int delayMs, int ms, int times)
    {
        var pulse = visual.Compositor.CreateScalarKeyFrameAnimation();
        pulse.InsertKeyFrame(0f, 0f);
        pulse.InsertKeyFrame(0.5f, 1f);
        pulse.InsertKeyFrame(1f, 0f);
        pulse.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        pulse.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        pulse.Duration = TimeSpan.FromMilliseconds(ms);
        pulse.IterationBehavior = Microsoft.UI.Composition.AnimationIterationBehavior.Count;
        pulse.IterationCount = times;
        visual.StartAnimation("Opacity", pulse);
    }

    /// <summary>On and off like a text cursor, a few times (half a period each), ending on.</summary>
    private static void Blink(Microsoft.UI.Composition.Visual visual, int delayMs, int periodMs, int times)
    {
        var compositor = visual.Compositor;
        var linear = compositor.CreateLinearEasingFunction();
        var blink = compositor.CreateScalarKeyFrameAnimation();
        // Hard cuts: each change happens within a thousandth of the run.
        blink.InsertKeyFrame(0f, 1f);
        for (var i = 0; i < times; i++)
        {
            var off = (i + 0.5f) / times;
            var on = (i + 1f) / times;
            blink.InsertKeyFrame(off - 0.001f, 1f, linear);
            blink.InsertKeyFrame(off, 0f, linear);
            blink.InsertKeyFrame(on - 0.001f, 0f, linear);
            blink.InsertKeyFrame(on, 1f, linear);
        }
        blink.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        blink.Duration = TimeSpan.FromMilliseconds(periodMs * times);
        visual.StartAnimation("Opacity", blink);
    }

    /// <summary>
    /// In the mood of the Xbox 360's: ribbons orbit and close in on a growing orb, the orb flashes white, and the logo is
    /// there when the flash clears (dark name on the light background).
    /// </summary>
    private void ShowX360Intro()
    {
        if (X360Ribbons.Children.Count == 0) BuildX360Ribbons();
        var (width, height) = IntroSize();
        var ribbons = Visual(X360Ribbons);
        ribbons.CenterPoint = new Vector3((float)width / 2, (float)height / 2, 0);
        ScaleTo(ribbons, 1.6f, 0.12f, 0, 1700);
        Fade(ribbons, 1, 0, 1450, 500);
        foreach (var turn in X360Ribbons.Children.OfType<Grid>())
        {
            var spin = Visual(turn.Children[0] as FrameworkElement ?? turn);
            var size = (float)((FrameworkElement)turn.Children[0]).Width;
            spin.CenterPoint = new Vector3(size / 2, size / 2, 0);
            var rotate = spin.Compositor.CreateScalarKeyFrameAnimation();
            var from = (float)(turn.Tag is double d ? d : 0);
            rotate.InsertKeyFrame(0f, from);
            rotate.InsertKeyFrame(1f, from + 540f, spin.Compositor.CreateLinearEasingFunction());
            rotate.Duration = TimeSpan.FromMilliseconds(2000);
            spin.StartAnimation("RotationAngleInDegrees", rotate);
        }
        var orb = Visual(X360Orb);
        orb.CenterPoint = new Vector3(120, 120, 0);
        var grow = orb.Compositor.CreateVector3KeyFrameAnimation();
        grow.InsertKeyFrame(0f, new Vector3(0.25f, 0.25f, 1));
        grow.InsertKeyFrame(0.55f, new Vector3(1f, 1f, 1));
        grow.InsertKeyFrame(1f, new Vector3(5f, 5f, 1));
        grow.Duration = TimeSpan.FromMilliseconds(2000);
        orb.StartAnimation("Scale", grow);
        Fade(orb, 1, 0, 1500, 600);
        var flash = Visual(X360Flash);
        var blink = flash.Compositor.CreateScalarKeyFrameAnimation();
        blink.InsertKeyFrame(0f, 0f);
        blink.InsertKeyFrame(0.45f, 0.85f);
        blink.InsertKeyFrame(1f, 0f);
        blink.DelayTime = TimeSpan.FromMilliseconds(1450);
        blink.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        blink.Duration = TimeSpan.FromMilliseconds(600);
        flash.StartAnimation("Opacity", blink);
        var mark = Visual(X360Mark);
        mark.CenterPoint = new Vector3((float)X360Mark.ActualWidth / 2, (float)X360Mark.ActualHeight / 2, 0);
        Fade(mark, 0, 1, 1750, 450);
        ScaleTo(mark, 0.92f, 1f, 1750, 650);
    }

    /// <summary>Nine dashed circles of different sizes, green and silver, each turning inside a flattened frame.</summary>
    private void BuildX360Ribbons()
    {
        for (var i = 0; i < 9; i++)
        {
            var size = 700 + i % 3 * 180 + i * 20;
            var silver = i % 3 == 2;
            var circle = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(silver ? Windows.UI.Color.FromArgb(0x99, 0x96, 0xA0, 0xAA) : Windows.UI.Color.FromArgb(0xB3, 0x5C, 0xBE, 0x28)),
                StrokeThickness = silver ? 4 : 9,
                // One long arc and a short one per circle, like the ribbons.
                StrokeDashArray = [size * 0.9 / (silver ? 4 : 9), size * 1.4 / (silver ? 4 : 9), size * 0.25 / (silver ? 4 : 9), size * 0.6 / (silver ? 4 : 9)],
                StrokeDashCap = PenLineCap.Round
            };
            // The frame flattens the circle (an orbit seen from the side) and tilts it a little.
            var frame = new Grid
            {
                Width = size,
                Height = size,
                Tag = (double)(i * 40),
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = new CompositeTransform { ScaleX = 1.25, ScaleY = 0.46, Rotation = -8 + i % 4 * 5 }
            };
            frame.Children.Add(circle);
            X360Ribbons.Children.Add(frame);
        }
    }

    /// <summary>In the mood of the PS4's: waves of light drifting across a deep blue, particles rising, the logo in the light.</summary>
    private void ShowPs4Intro()
    {
        var (width, height) = IntroSize();
        if (Ps4Waves.Children.Count == 0) BuildPs4(width, height);
        var waves = Visual(Ps4Waves);
        var drift = waves.Compositor.CreateScalarKeyFrameAnimation();
        drift.InsertKeyFrame(0f, 0f);
        drift.InsertKeyFrame(1f, -(float)width, waves.Compositor.CreateLinearEasingFunction());
        drift.Duration = TimeSpan.FromSeconds(9);
        ElementCompositionPreview.SetIsTranslationEnabled(Ps4Waves, true);
        waves.StartAnimation("Translation.X", drift);
        Fade(waves, 0, 1, 0, 800);
        ElementCompositionPreview.SetIsTranslationEnabled(Ps4Particles, true);
        var particles = Visual(Ps4Particles);
        var rise = particles.Compositor.CreateScalarKeyFrameAnimation();
        rise.InsertKeyFrame(0f, 0f);
        rise.InsertKeyFrame(1f, -(float)height * 0.25f, particles.Compositor.CreateLinearEasingFunction());
        rise.Duration = TimeSpan.FromSeconds(4);
        particles.StartAnimation("Translation.Y", rise);
        Fade(particles, 0, 1, 0, 800);
        Fade(Visual(Ps4Light), 0, 1, 600, 1000);
        Fade(Visual(Ps4Mark), 0, 1, 900, 1000);
    }

    /// <summary>Four sine waves twice as wide as the screen (they drift left) and sixty particles.</summary>
    private void BuildPs4(double width, double height)
    {
        for (var i = 0; i < 4; i++)
        {
            var line = new Microsoft.UI.Xaml.Shapes.Polyline
            {
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(46 + i * 13), 120, 180, 255)),
                StrokeThickness = 1.5 + i
            };
            for (var x = 0.0; x <= width * 2; x += 12)
                line.Points.Add(new Windows.Foundation.Point(x, height * (0.55 + i * 0.07) + Math.Sin(x / width * Math.PI * 2 + i) * height * 0.08));
            Ps4Waves.Children.Add(line);
        }
        var random = new Random(7);
        for (var i = 0; i < 60; i++)
        {
            var r = 1.2 + random.NextDouble() * 3.4;
            var dot = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = r,
                Height = r,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(40 + random.Next(120)), 190, 220, 255))
            };
            Canvas.SetLeft(dot, random.NextDouble() * width);
            Canvas.SetTop(dot, random.NextDouble() * height * 1.3);
            Ps4Particles.Children.Add(dot);
        }
    }

    /// <summary>The window's size for drawing; before the first layout, the screen's.</summary>
    private (double Width, double Height) IntroSize()
    {
        var width = IntroPanel.ActualWidth;
        var height = IntroPanel.ActualHeight;
        if (width > 0 && height > 0) return (width, height);
        var bounds = XamlRoot?.Size ?? new Windows.Foundation.Size(1920, 1080);
        return (Math.Max(bounds.Width, 1280), Math.Max(bounds.Height, 720));
    }

    private static Microsoft.UI.Composition.Visual Visual(UIElement element) => ElementCompositionPreview.GetElementVisual(element);

    private static void Fade(Microsoft.UI.Composition.Visual visual, float from, float to, int delayMs, int ms)
    {
        var fade = visual.Compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, from);
        fade.InsertKeyFrame(1f, to);
        fade.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        fade.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        fade.Duration = TimeSpan.FromMilliseconds(ms);
        visual.StartAnimation("Opacity", fade);
    }

    private static void ScaleTo(Microsoft.UI.Composition.Visual visual, float from, float to, int delayMs, int ms)
    {
        var compositor = visual.Compositor;
        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, new Vector3(from, from, 1f));
        scale.InsertKeyFrame(1f, new Vector3(to, to, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.8f), new Vector2(0.2f, 1f)));
        scale.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        scale.DelayBehavior = Microsoft.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        scale.Duration = TimeSpan.FromMilliseconds(ms);
        visual.StartAnimation("Scale", scale);
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
        _rowsEdges.Snap();
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
