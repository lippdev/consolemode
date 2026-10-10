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
/// The Select + Y menu over the game (see MainViewModel.SessionMenu), in the spirit of Steam's
/// Shift + Tab: it covers the whole screen, the game dims and blurs behind it, the options sit in
/// a side panel and the open windows (an always-visible Alt + Tab) fill the middle. Driven by the
/// controller through GamepadNavigator or by keyboard. The panel slides in from the left and the
/// rows follow one after another; closing fades it out. Exclusive-fullscreen games can't be
/// covered; Big Picture and borderless games can.
/// </summary>
public sealed partial class SessionMenuWindow : Window
{
    private static readonly UISettings Ui = new();

    private readonly nint _hwnd;
    private readonly GamepadNavigator _navigator;
    private bool _editingVolume;
    private bool _closing;
    private Control? _pickerOpener;
    private Control? _lastSidebarFocus;
    private FocusNavigationDirection _lastDirection;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _closeTimer;
    private SwitchWindowItem? _pendingCloseItem;
    private int _pendingCloseIndex;

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
        // Before sizing: the classic frame is what shows as a light border and eats 6 px of the client area.
        WindowChrome.Strip(_hwnd);
        WindowPlacement.FillScreen(appWindow, target);
        TryRemoveRoundingAndOutline();
        ApplyMaterial();

        // Hidden until its entrance animation runs, or the rows would flash in place first.
        if (Animate) HideForEntrance();

        // No foreground gate and HID for PlayStation pads: the game often keeps the focus even
        // with the menu on top, and then the menu never heard the controller.
        _navigator = new GamepadNavigator(DispatcherQueue, Root, readSonyHid: true)
        {
            Sounds = true,
            // The side panel and the windows grid sit next to each other, never on the same row.
            NearestOnSides = true,
            // The end of a column is an end: the next item in reading order would be the one beside it in the grid.
            ReadingOrderFallback = false,
            // Focus may only land inside the picker while it is open (it covers everything else); up and
            // down stay inside the panel or the grid they start in, so the end of a list is an end.
            SearchRoot = () => CloseConfirmOverlay.Visibility == Visibility.Visible ? CloseConfirmCard
                : ViewModel.IsSessionPickerOpen ? PickerCard
                : _lastDirection is FocusNavigationDirection.Up or FocusNavigationDirection.Down ? RegionOfFocus() : Root,
            // Adjust mode on the volume row: Left/Right change it, Up/Down are swallowed.
            BeforeMove = direction =>
            {
                _lastDirection = direction;
                if (CloseConfirmOverlay.Visibility == Visibility.Visible) return false;
                // Left from the first column of windows goes back to the row of the panel you came from.
                if (!_editingVolume && direction == FocusNavigationDirection.Left && !ViewModel.IsSessionPickerOpen && IsOnLeftmostCard())
                {
                    (_lastSidebarFocus ?? FirstRow).Focus(FocusState.Keyboard);
                    UiSounds.Play(UiSound.Move);
                    return true;
                }
                // Along a row of window cards: pick the neighbour by position, not by the spatial search.
                if (!_editingVolume && direction is (FocusNavigationDirection.Left or FocusNavigationDirection.Right)
                    && !ViewModel.IsSessionPickerOpen && MoveAcrossWindowCards(direction))
                {
                    UiSounds.Play(UiSound.Move);
                    return true;
                }
                // Cross into the grid explicitly; spatial focus may prefer another sidebar row.
                if (!_editingVolume && direction == FocusNavigationDirection.Right && !ViewModel.IsSessionPickerOpen
                    && Root.XamlRoot is { } root
                    && FocusManager.GetFocusedElement(root) is DependencyObject focused
                    && IsInside(SidebarPanel, focused) && FocusFirstWindowCard())
                {
                    UiSounds.Play(UiSound.Move);
                    return true;
                }
                if (!_editingVolume) return false;
                if (direction == FocusNavigationDirection.Left) { ViewModel.ChangeVolume(-1); UiSounds.Play(UiSound.Move); }
                else if (direction == FocusNavigationDirection.Right) { ViewModel.ChangeVolume(1); UiSounds.Play(UiSound.Move); }
                return true;
            }
        };
        // X / Square: close the highlighted window, or mute on the volume row.
        _navigator.OptionRequested += () =>
        {
            if (CloseConfirmOverlay.Visibility == Visibility.Visible) return;
            if (!CloseFocusedWindow() && IsVolumeFocused()) ViewModel.ToggleMuteCommand.Execute(null);
        };
        _navigator.BackRequested += () =>
        {
            if (CloseConfirmOverlay.Visibility == Visibility.Visible) CancelCloseConfirmation();
            else if (_editingVolume) SetEditingVolume(false);
            else ViewModel.SessionMenuBack();
        };
        _navigator.Start();

        // PreviewKeyDown (tunneling): the ScrollViewer would otherwise handle the arrows as scrolling
        // before they ever bubble up to Root.
        Root.PreviewKeyDown += (_, e) =>
        {
            if (CloseConfirmOverlay.Visibility == Visibility.Visible)
            {
                var confirmDirection = e.Key switch
                {
                    Windows.System.VirtualKey.Up => FocusNavigationDirection.Up,
                    Windows.System.VirtualKey.Down => FocusNavigationDirection.Down,
                    Windows.System.VirtualKey.Left => FocusNavigationDirection.Left,
                    Windows.System.VirtualKey.Right => FocusNavigationDirection.Right,
                    _ => FocusNavigationDirection.None
                };
                if (confirmDirection != FocusNavigationDirection.None)
                {
                    e.Handled = true;
                    _navigator.Navigate(confirmDirection);
                    return;
                }
                if (e.Key == Windows.System.VirtualKey.Tab)
                {
                    e.Handled = true;
                    var focused = Root.XamlRoot is { } confirmRoot ? FocusManager.GetFocusedElement(confirmRoot) : null;
                    if (ReferenceEquals(focused, CancelCloseButton)) ConfirmCloseButton.Focus(FocusState.Keyboard);
                    else CancelCloseButton.Focus(FocusState.Keyboard);
                    return;
                }
                if (e.Key is Windows.System.VirtualKey.Escape or Windows.System.VirtualKey.Back)
                {
                    e.Handled = true;
                    CancelCloseConfirmation();
                }
                else if (e.Key == Windows.System.VirtualKey.Delete) e.Handled = true;
                return;
            }
            // Arrows go through the same navigator as the D-pad (aligned, then the nearest).
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
            // Delete closes the highlighted window (the keyboard's X / Square).
            if (e.Key == Windows.System.VirtualKey.Delete && CloseFocusedWindow()) { e.Handled = true; return; }
            // The pad's B comes from the navigator's polling; handling GamepadB here too would count it twice.
            if (e.Key is not Windows.System.VirtualKey.Escape) return;
            e.Handled = true;
            if (_editingVolume) { SetEditingVolume(false); return; }
            ViewModel.SessionMenuBack();
        };
        // The focused row / card outlines in white and grows a little, like the console interface.
        Root.GotFocus += (_, e) =>
        {
            ScaleFocused(e.OriginalSource, grow: true);
            if (e.OriginalSource is Control focused && IsInside(SidebarPanel, focused)) _lastSidebarFocus = focused;
            if (e.OriginalSource is FrameworkElement { DataContext: SwitchWindowItem } card)
                card.StartBringIntoView();
        };
        Root.LostFocus += (_, e) => ScaleFocused(e.OriginalSource, grow: false);
        // Mouse clicks sound like a confirm (the pad has its own sounds in the navigator).
        Root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);

        ViewModel.PropertyChanged += OnViewModelChanged;
        ViewModel.SessionPickerRefocus += FocusPickerSelection;
        Ui.ColorValuesChanged += OnColorValuesChanged;
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
            ViewModel.SessionPickerRefocus -= FocusPickerSelection;
            Ui.ColorValuesChanged -= OnColorValuesChanged;
            _navigator.Dispose();
        };
    }

    private static bool Animate => Ui.AnimationsEnabled;

    // ── Look: dim glass over the game, if the user has Windows' transparency effects on ───────

    /// <summary>
    /// Glass when Settings → Personalization → Colors → "Transparency effects" is on (the game dims and
    /// blurs behind the menu); when it is off, or the battery saver turns it off, a solid dark veil with
    /// the same contrast. Re-applied live when the user flips the setting.
    /// </summary>
    private void ApplyMaterial()
    {
        var glass = Ui.AdvancedEffectsEnabled;
        try
        {
            SystemBackdrop = glass ? new DesktopAcrylicBackdrop() : null;
        }
        catch (Exception ex)
        {
            glass = false;
            AppLog.Write($"Menu da sessão: vidro: {ex.Message}");
        }
        Root.Background = new SolidColorBrush(glass ? Windows.UI.Color.FromArgb(0x99, 0x07, 0x0A, 0x0E) : Windows.UI.Color.FromArgb(0xF7, 0x07, 0x0A, 0x0E));
        SidebarPanel.Background = new SolidColorBrush(glass ? Windows.UI.Color.FromArgb(0xB3, 0x11, 0x18, 0x20) : Windows.UI.Color.FromArgb(0xFF, 0x13, 0x1A, 0x22));
    }

    private void OnColorValuesChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(ApplyMaterial);

    private void TryRemoveRoundingAndOutline()
    {
        try
        {
            // A full-screen overlay has no rounded corners and no 1 px Windows outline.
            var square = 1; // DWMWCP_DONOTROUND
            DwmSetWindowAttribute(_hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref square, sizeof(int));
            var none = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
            DwmSetWindowAttribute(_hwnd, 34 /* DWMWA_BORDER_COLOR */, ref none, sizeof(int));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: cantos: {ex.Message}");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    // ── Animations ───────────────────────────────────────────────────────────────────────────

    private IEnumerable<UIElement> EntranceElements()
    {
        yield return HeaderPanel;
        yield return SidebarPanel;
        yield return WindowsPanel;
        yield return HintsPanel;
    }

    private void HideForEntrance()
    {
        try
        {
            foreach (var element in EntranceElements()) ElementCompositionPreview.GetElementVisual(element).Opacity = 0f;
            foreach (var row in RowsPanel.Children) ElementCompositionPreview.GetElementVisual(row).Opacity = 0f;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
        }
    }

    /// <summary>The header drops in, the side panel slides in from the left with its rows one after another, then the windows and the hints.</summary>
    private void PlayEntrance()
    {
        try
        {
            Enter(HeaderPanel, 0f, -16f, 260, 0);
            Enter(SidebarPanel, -56f, 0f, 320, 40);
            var index = 0;
            foreach (var row in RowsPanel.Children) Enter(row, 0f, 14f, 260, 120 + index++ * 40);
            Enter(WindowsPanel, 0f, 26f, 320, 180);
            Enter(HintsPanel, 0f, 10f, 240, 320);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
            foreach (var element in EntranceElements()) ElementCompositionPreview.GetElementVisual(element).Opacity = 1f;
            foreach (var row in RowsPanel.Children) ElementCompositionPreview.GetElementVisual(row).Opacity = 1f;
        }
    }

    /// <summary>Slides an element in from (<paramref name="fromX"/>, <paramref name="fromY"/>) while it fades in.</summary>
    private static void Enter(UIElement element, float fromX, float fromY, int milliseconds, int delayMilliseconds)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));
        var delay = TimeSpan.FromMilliseconds(delayMilliseconds);

        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0f, new Vector3(fromX, fromY, 0f));
        slide.InsertKeyFrame(1f, Vector3.Zero, ease);
        slide.Duration = TimeSpan.FromMilliseconds(milliseconds);
        slide.DelayTime = delay;
        slide.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation("Translation", slide);

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f, ease);
        fade.Duration = TimeSpan.FromMilliseconds(Math.Max(milliseconds - 40, 120));
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
            sink.InsertKeyFrame(1f, 22f, ease);
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

    /// <summary>The picker fades and rises in over everything.</summary>
    private void PlayPickerEntrance()
    {
        if (!Animate) return;
        try
        {
            ElementCompositionPreview.GetElementVisual(PickerOverlay).Opacity = 0f;
            Enter(PickerOverlay, 0f, 0f, 200, 0);
            ElementCompositionPreview.GetElementVisual(PickerCard).Opacity = 0f;
            Enter(PickerCard, 0f, 22f, 240, 30);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Menu da sessão: animação: {ex.Message}");
            ElementCompositionPreview.GetElementVisual(PickerOverlay).Opacity = 1f;
            ElementCompositionPreview.GetElementVisual(PickerCard).Opacity = 1f;
        }
    }

    /// <summary>
    /// Window cards grow 4% when focused (Tag "card") and shrink back when it leaves; the side rows stay
    /// still and show the selection with the ring alone. Done on the composition visual: once a visual is used for the entrance animation, WinUI
    /// refuses UIElement.Scale and CenterPoint on it.
    /// </summary>
    private static void ScaleFocused(object source, bool grow)
    {
        if (source is not Button button) return;
        var factor = button.Tag switch { "card" => 1.04f, _ => 0f };
        if (factor == 0f) return;
        try
        {
            var scale = grow ? factor : 1f;
            var visual = ElementCompositionPreview.GetElementVisual(button);
            visual.CenterPoint = new Vector3((float)button.ActualWidth / 2, (float)button.ActualHeight / 2, 0);
            var compositor = visual.Compositor;
            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1f, new Vector3(scale, scale, 1f),
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
            animation.Duration = TimeSpan.FromMilliseconds(140);
            visual.StartAnimation("Scale", animation);
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

    /// <summary>A on the volume row toggles adjust mode; the arrows show while it is on.</summary>
    private void OnVolumeClick(object sender, RoutedEventArgs e) => SetEditingVolume(!_editingVolume);

    private void SetEditingVolume(bool on)
    {
        _editingVolume = on;
        VolumeLeft.Visibility = VolumeRight.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool IsInside(DependencyObject ancestor, DependencyObject? node)
    {
        for (; node is not null; node = VisualTreeHelper.GetParent(node))
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    private DependencyObject RegionOfFocus()
    {
        var focused = Root.XamlRoot is { } root ? FocusManager.GetFocusedElement(root) as DependencyObject : null;
        if (IsInside(WindowsPanel, focused)) return WindowsPanel;
        if (IsInside(SidebarPanel, focused)) return SidebarPanel;
        return Root;
    }

    /// <summary>Enter the windows grid even when spatial focus cannot reach it from the sidebar.</summary>
    private bool FocusFirstWindowCard()
    {
        SwitcherList.UpdateLayout();
        for (var i = 0; i < ViewModel.SwitcherWindows.Count; i++)
        {
            if (SwitcherList.ContainerFromIndex(i) is { } container
                && FocusManager.FindFirstFocusableElement(container) is Control card
                && card.Focus(FocusState.Keyboard))
            {
                card.StartBringIntoView();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Left/right between window cards by position: the card on the same row with the next X to that side.
    /// The XY focus search can miss the neighbour (cards scaled while focused, clipped or scrolled in the
    /// panel), which left the grid impossible to cross sideways. The end of a row is an end: Right on the last
    /// card is consumed so the search can't jump somewhere else; Left on the first column is the caller's
    /// (back to the side panel). False when the focus is not on a window card.
    /// </summary>
    private bool MoveAcrossWindowCards(FocusNavigationDirection direction)
    {
        if (Root.XamlRoot is not { } root
            || FocusManager.GetFocusedElement(root) is not FrameworkElement { DataContext: SwitchWindowItem } current)
            return false;

        Windows.Foundation.Point Origin(FrameworkElement e) => e.TransformToVisual(SwitcherList).TransformPoint(new Windows.Foundation.Point(0, 0));
        var from = Origin(current);
        var rowTolerance = Math.Max(current.ActualHeight / 2, 1);

        Control? best = null;
        var bestX = 0d;
        for (var i = 0; i < ViewModel.SwitcherWindows.Count; i++)
        {
            if (SwitcherList.ContainerFromIndex(i) is not { } container
                || FocusManager.FindFirstFocusableElement(container) is not Control card
                || ReferenceEquals(card, current))
                continue;
            var at = Origin(card);
            if (Math.Abs(at.Y - from.Y) > rowTolerance) continue;
            var dx = at.X - from.X;
            if (direction == FocusNavigationDirection.Right ? dx <= 1 : dx >= -1) continue;
            if (best is null || (direction == FocusNavigationDirection.Right ? at.X < bestX : at.X > bestX))
            {
                best = card;
                bestX = at.X;
            }
        }

        if (best is null) return direction == FocusNavigationDirection.Right;
        best.Focus(FocusState.Keyboard);
        best.StartBringIntoView();
        return true;
    }

    /// <summary>True when the focus is on a window card with no card to its left (the first column of the grid).</summary>
    private bool IsOnLeftmostCard()
    {
        if (Root.XamlRoot is not { } root || FocusManager.GetFocusedElement(root) is not FrameworkElement { DataContext: SwitchWindowItem } card) return false;
        double X(FrameworkElement e) => e.TransformToVisual(SwitcherList).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
        var cardX = X(card);
        for (var i = 0; i < ViewModel.SwitcherWindows.Count; i++)
        {
            if (SwitcherList.ContainerFromIndex(i) is { } container
                && FocusManager.FindFirstFocusableElement(container) is FrameworkElement other && X(other) < cardX - 1)
                return false;
        }
        return true;
    }

    /// <summary>X / Delete on a window card: ask that window to close. False when the focus is not on a window card.</summary>
    private bool CloseFocusedWindow()
    {
        if (Root.XamlRoot is not { } root) return false;
        if (FocusManager.GetFocusedElement(root) is not FrameworkElement { DataContext: SwitchWindowItem item }) return false;
        var index = ViewModel.SwitcherWindows.IndexOf(item);
        ShowCloseConfirmation(item, index);
        return true;
    }

    private void ShowCloseConfirmation(SwitchWindowItem item, int index)
    {
        if (CloseConfirmOverlay.Visibility == Visibility.Visible) return;
        _pendingCloseItem = item;
        _pendingCloseIndex = index;
        CloseConfirmProcessText.Text = item.ProcessName;
        CloseConfirmProcessText.Visibility = item.ProcessName.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        CloseConfirmTitleText.Text = item.Title;
        CloseConfirmIcon.Source = item.Icon;
        CloseConfirmGlyph.Visibility = item.HasNoIcon ? Visibility.Visible : Visibility.Collapsed;
        CloseConfirmOverlay.Visibility = Visibility.Visible;
        CancelCloseButton.Focus(FocusState.Programmatic);
    }

    private void CancelCloseConfirmation()
    {
        var item = _pendingCloseItem;
        var index = _pendingCloseIndex;
        _pendingCloseItem = null;
        CloseConfirmOverlay.Visibility = Visibility.Collapsed;
        FocusSwitcherCard(item, index, FocusState.Programmatic);
    }

    private async void ConfirmCloseClick(object sender, RoutedEventArgs e)
    {
        var item = _pendingCloseItem;
        var index = _pendingCloseIndex;
        _pendingCloseItem = null;
        CloseConfirmOverlay.Visibility = Visibility.Collapsed;
        FocusSwitcherCard(item, index, FocusState.Programmatic);
        if (item is not null) await CloseAndRefocusAsync(item, index);
    }

    private void CancelCloseClick(object sender, RoutedEventArgs e) => CancelCloseConfirmation();

    /// <summary>Keep focus on the target while the close request is pending, then move to a neighbor if it closes.</summary>
    private async Task CloseAndRefocusAsync(SwitchWindowItem item, int index)
    {
        await ViewModel.CloseSwitcherWindowAsync(item);
        FocusSwitcherCard(item, index, FocusState.Keyboard);
    }

    /// <summary>Focus the requested card if it remains; otherwise the card now occupying its old position.</summary>
    private void FocusSwitcherCard(SwitchWindowItem? item, int oldIndex, FocusState focusState)
    {
        var target = item is not null && ViewModel.SwitcherWindows.Contains(item)
            ? item
            : ViewModel.SwitcherWindows.Count == 0 ? null
            : ViewModel.SwitcherWindows[Math.Clamp(oldIndex, 0, ViewModel.SwitcherWindows.Count - 1)];
        if (target is null)
        {
            FirstRow.Focus(focusState);
            return;
        }

        var next = ViewModel.SwitcherWindows.IndexOf(target);
        SwitcherList.UpdateLayout();
        if (SwitcherList.ContainerFromIndex(next) is { } container
            && FocusManager.FindFirstFocusableElement(container) is Control card
            && ReferenceEquals(card.DataContext, target))
        {
            card.Focus(focusState);
            return;
        }

        FirstRow.Focus(focusState);
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
                // Read the focus now: by the time the queued work runs, the picker already took it.
                if (ViewModel.IsSessionPickerOpen && Root.XamlRoot is { } xamlRoot)
                    _pickerOpener = FocusManager.GetFocusedElement(xamlRoot) as Control;
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsSessionPickerOpen) { PlayPickerEntrance(); FocusPickerSelection(); }
                    else (_pickerOpener ?? FirstRow).Focus(FocusState.Keyboard);   // back on the row that opened it
                });
                break;
        }
    }

    private void FocusPickerSelection()
    {
        var index = ViewModel.SessionPickerFocusIndex >= 0
            ? ViewModel.SessionPickerFocusIndex
            : Math.Max(ViewModel.SessionPickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
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
