using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace ConsoleMode;

/// <summary>
/// The Select + Y menu over the game (see MainViewModel.SessionMenu). Centered on the game
/// screen, always on top, acrylic glass with rounded corners, driven by the controller through
/// GamepadNavigator or by keyboard. It slides up and its tiles come in one after another; closing
/// fades it out. Exclusive-fullscreen games can't be covered; Big Picture and borderless games can.
/// </summary>
public sealed partial class SessionMenuWindow : Window
{
    private const double WidthDip = 1180;
    private const double HeightDip = 660;

    private static readonly UISettings Ui = new();

    private readonly nint _hwnd;
    private readonly GamepadNavigator _navigator;
    private bool _editingVolume;
    private bool _closing;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _closeTimer;

    public MainViewModel ViewModel { get; }

    public SessionMenuWindow(MainViewModel viewModel, ScreenRect? target)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        WindowPlacement.CenterOn(appWindow, target, WidthDip, HeightDip);
        TryGlassAndRoundCorners();

        // Hidden until its entrance animation runs, or the tiles would flash in place first.
        if (Animate) HideForEntrance();

        // No foreground gate and HID for PlayStation pads: the game often keeps the focus even
        // with the menu on top, and then the menu never heard the controller.
        _navigator = new GamepadNavigator(DispatcherQueue, Root, readSonyHid: true)
        {
            Sounds = true,
            // Focus may only land inside the picker while it is open (it covers the tiles).
            SearchRoot = () => ViewModel.IsSessionSwitcherOpen ? SwitcherCard : ViewModel.IsSessionPickerOpen ? PickerCard : Root,
            // Adjust mode on the volume tile: Left/Right change it, Up/Down are swallowed.
            BeforeMove = direction =>
            {
                if (!_editingVolume) return false;
                if (direction == FocusNavigationDirection.Left) { ViewModel.ChangeVolume(-1); UiSounds.Play(UiSound.Move); }
                else if (direction == FocusNavigationDirection.Right) { ViewModel.ChangeVolume(1); UiSounds.Play(UiSound.Move); }
                return true;
            }
        };
        _navigator.OptionRequested += () =>
        {
            if (ViewModel.IsSessionSwitcherOpen) CloseFocusedWindow();
            else if (IsVolumeFocused()) ViewModel.ToggleMuteCommand.Execute(null);
        };
        _navigator.BackRequested += () =>
        {
            if (_editingVolume) SetEditingVolume(false);
            else ViewModel.SessionMenuBack();
        };
        _navigator.Start();

        // PreviewKeyDown (tunneling): the ScrollViewer would otherwise handle the arrows as scrolling
        // before they ever bubble up to Root.
        Root.PreviewKeyDown += (_, e) =>
        {
            // Arrows go through the same navigator as the D-pad (aligned card, then the nearest one).
            var direction = e.Key switch
            {
                Windows.System.VirtualKey.Up => FocusNavigationDirection.Up,
                Windows.System.VirtualKey.Down => FocusNavigationDirection.Down,
                Windows.System.VirtualKey.Left => FocusNavigationDirection.Left,
                Windows.System.VirtualKey.Right => FocusNavigationDirection.Right,
                _ => FocusNavigationDirection.None
            };
            if (direction != FocusNavigationDirection.None)
            {
                e.Handled = true;
                _navigator.Navigate(direction);
                return;
            }
            // Delete closes the highlighted window in the switcher (the keyboard's X / Square).
            if (e.Key == Windows.System.VirtualKey.Delete && ViewModel.IsSessionSwitcherOpen) { e.Handled = true; CloseFocusedWindow(); return; }
            // The pad's B comes from the navigator's polling; handling GamepadB here too would count it twice.
            if (e.Key is not Windows.System.VirtualKey.Escape) return;
            e.Handled = true;
            if (_editingVolume) { SetEditingVolume(false); return; }
            ViewModel.SessionMenuBack();
        };
        // The focused tile grows a little, like the console interface.
        Root.GotFocus += (_, e) => ScaleTile(e.OriginalSource, 1.06f);
        Root.LostFocus += (_, e) => ScaleTile(e.OriginalSource, 1f);
        // Mouse clicks sound like a confirm (the pad has its own sounds in the navigator).
        Root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);

        ViewModel.PropertyChanged += OnViewModelChanged;
        VolumeRow.LostFocus += (_, _) => SetEditingVolume(false);
        // BringToFront runs before the tree exists; the real first focus happens here.
        Root.Loaded += (_, _) =>
        {
            FirstRow.Focus(FocusState.Keyboard);
            if (Animate) PlayEntrance();
        };
        Activated += (_, args) =>
        {
            // XamlRoot is null on the very first activation; GetFocusedElement(null) throws.
            if (args.WindowActivationState != WindowActivationState.Deactivated && Root.XamlRoot is { } root
                && FocusManager.GetFocusedElement(root) is null)
                FirstRow.Focus(FocusState.Keyboard);
        };
        Closed += (_, _) =>
        {
            ViewModel.PropertyChanged -= OnViewModelChanged;
            _navigator.Dispose();
        };
    }

    private static bool Animate => Ui.AnimationsEnabled;

    // ── Look: acrylic glass and rounded corners ──────────────────────────────────────────────

    private void TryGlassAndRoundCorners()
    {
        try
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            var round = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(_hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref round, sizeof(int));
        }
        catch (Exception ex)
        {
            // Plain dark panel: still a working menu.
            AppLog.Write($"Menu da sessão: vidro/cantos: {ex.Message}");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    // ── Animations ───────────────────────────────────────────────────────────────────────────

    private IEnumerable<UIElement> EntranceElements()
    {
        yield return HeaderPanel;
        foreach (var tile in TilesPanel.Children) yield return tile;
        yield return ExitPanel;
        yield return HintsPanel;
    }

    private void HideForEntrance()
    {
        try
        {
            foreach (var element in EntranceElements()) ElementCompositionPreview.GetElementVisual(element).Opacity = 0f;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
        }
    }

    /// <summary>The header first, then each tile 45 ms after the one before, then the exits and the hints.</summary>
    private void PlayEntrance()
    {
        try
        {
            var index = 0;
            foreach (var element in EntranceElements())
            {
                var isHeader = ReferenceEquals(element, HeaderPanel);
                Enter(element, isHeader ? 14f : 26f, isHeader ? 240 : 290, isHeader ? 0 : 60 + index * 45);
                if (!isHeader) index++;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
            foreach (var element in EntranceElements()) ElementCompositionPreview.GetElementVisual(element).Opacity = 1f;
        }
    }

    /// <summary>Slides an element up from <paramref name="fromY"/> while it fades in.</summary>
    private static void Enter(UIElement element, float fromY, int milliseconds, int delayMilliseconds)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));
        var delay = TimeSpan.FromMilliseconds(delayMilliseconds);

        var slide = compositor.CreateScalarKeyFrameAnimation();
        slide.InsertKeyFrame(0f, fromY);
        slide.InsertKeyFrame(1f, 0f, ease);
        slide.Duration = TimeSpan.FromMilliseconds(milliseconds);
        slide.DelayTime = delay;
        slide.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation("Translation.Y", slide);

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f, ease);
        fade.Duration = TimeSpan.FromMilliseconds(milliseconds - 40);
        fade.DelayTime = delay;
        fade.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation("Opacity", fade);
    }

    /// <summary>Fades the menu out and sinks it a little, then closes the window.</summary>
    public void CloseAnimated()
    {
        if (_closing) return;
        _closing = true;
        if (!Animate) { Close(); return; }
        try
        {
            ElementCompositionPreview.SetIsTranslationEnabled(Root, true);
            var visual = ElementCompositionPreview.GetElementVisual(Root);
            var compositor = visual.Compositor;
            var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(1f, 1f));

            var sink = compositor.CreateScalarKeyFrameAnimation();
            sink.InsertKeyFrame(0f, 0f);
            sink.InsertKeyFrame(1f, 26f, ease);
            sink.Duration = TimeSpan.FromMilliseconds(170);
            visual.StartAnimation("Translation.Y", sink);

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 1f);
            fade.InsertKeyFrame(1f, 0f, ease);
            fade.Duration = TimeSpan.FromMilliseconds(170);
            visual.StartAnimation("Opacity", fade);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
            Close();
            return;
        }
        // A timer, not the animation's completion: the window must go away even if the composition stalls.
        _closeTimer = DispatcherQueue.CreateTimer();
        _closeTimer.Interval = TimeSpan.FromMilliseconds(190);
        _closeTimer.IsRepeating = false;
        _closeTimer.Tick += (_, _) => Close();
        _closeTimer.Start();
    }

    /// <summary>A panel over the tiles (the picker, the window switcher): the veil fades in, the card fades and rises in.</summary>
    private void PlayOverlayEntrance(UIElement overlay, UIElement card)
    {
        if (!Animate) return;
        try
        {
            ElementCompositionPreview.GetElementVisual(overlay).Opacity = 0f;
            Enter(overlay, 0f, 200, 0);
            ElementCompositionPreview.GetElementVisual(card).Opacity = 0f;
            Enter(card, 22f, 240, 30);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
            ElementCompositionPreview.GetElementVisual(overlay).Opacity = 1f;
            ElementCompositionPreview.GetElementVisual(card).Opacity = 1f;
        }
    }

    /// <summary>
    /// Tiles (not the picker rows) grow when focused and shrink back when it leaves. Done on the
    /// composition visual: once a tile's visual is in use for the entrance animation, WinUI refuses
    /// UIElement.Scale and CenterPoint on it.
    /// </summary>
    private static void ScaleTile(object source, float scale)
    {
        if (source is not Button { Tag: "card" } button) return;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(button);
            visual.CenterPoint = new Vector3((float)button.ActualWidth / 2, (float)button.ActualHeight / 2, 0);
            var compositor = visual.Compositor;
            var grow = compositor.CreateVector3KeyFrameAnimation();
            grow.InsertKeyFrame(1f, new Vector3(scale, scale, 1f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
            grow.Duration = TimeSpan.FromMilliseconds(140);
            visual.StartAnimation("Scale", grow);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: foco: {ex.Message}");
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        for (var node = e.OriginalSource as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ButtonBase { IsEnabled: true })
            {
                UiSounds.Play(UiSound.Confirm);
                return;
            }
        }
    }

    // ── Volume adjust mode ───────────────────────────────────────────────────────────────────

    private bool IsVolumeFocused() =>
        Root.XamlRoot is { } root && ReferenceEquals(FocusManager.GetFocusedElement(root), VolumeRow);

    /// <summary>A on the volume tile toggles adjust mode; the arrows show while it is on.</summary>
    private void OnVolumeClick(object sender, RoutedEventArgs e) => SetEditingVolume(!_editingVolume);

    private void SetEditingVolume(bool on)
    {
        _editingVolume = on;
        VolumeLeft.Visibility = VolumeRight.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Windows won't hand a background process the foreground; this forces it (PlayStation pads need it).</summary>
    public void BringToFront()
    {
        if (!NativeWindows.ForceForeground(_hwnd)) AppLog.Write("Menu da sessão: não conseguiu vir para frente");
        FirstRow.Focus(FocusState.Keyboard);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // A session menu goes with its session; the preview (opened outside one) does not.
            case nameof(MainViewModel.IsConsoleActive) when !ViewModel.IsConsoleActive && !ViewModel.IsSessionMenuPreview:
                Close();
                break;
            case nameof(MainViewModel.IsSessionPickerOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsSessionPickerOpen) { PlayOverlayEntrance(PickerOverlay, PickerCard); FocusPickerSelection(); }
                    else FirstRow.Focus(FocusState.Keyboard);
                });
                break;
            case nameof(MainViewModel.IsSessionSwitcherOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsSessionSwitcherOpen) { PlayOverlayEntrance(SwitcherOverlay, SwitcherCard); FocusSwitcherCard(); }
                    else FirstRow.Focus(FocusState.Keyboard);
                });
                break;
        }
    }

    /// <summary>X / Delete in the switcher: ask the highlighted window to close.</summary>
    private void CloseFocusedWindow()
    {
        if (Root.XamlRoot is not { } root) return;
        if (FocusManager.GetFocusedElement(root) is FrameworkElement { DataContext: SwitchWindowItem item })
            _ = ViewModel.CloseSwitcherWindowAsync(item);
    }

    /// <summary>Opens on the second card (the first is the window that was in front), like Alt + Tab.</summary>
    private void FocusSwitcherCard()
    {
        var index = AltTabRules.InitialIndex(ViewModel.SwitcherWindows.Count);
        bool TryFocus() =>
            SwitcherList.ContainerFromIndex(index) is { } container
            && (FocusManager.FindFirstFocusableElement(container) as Control)?.Focus(FocusState.Keyboard) == true;
        if (ViewModel.SwitcherWindows.Count == 0 || TryFocus()) return;
        void OnLayout(object? s, object e)
        {
            SwitcherList.LayoutUpdated -= OnLayout;
            TryFocus();
        }
        SwitcherList.LayoutUpdated += OnLayout;
    }

    private void FocusPickerSelection()
    {
        var index = Math.Max(ViewModel.SessionPickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
        if (TryFocusPickerRow(index)) return;
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
}
