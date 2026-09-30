using ConsoleMode.Native;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ConsoleMode.Services;

/// <summary>
/// WinUI 3 desktop apps get keyboard focus navigation for free but nothing from a
/// controller. This turns controller presses into focus moves and invokes, scoped to one
/// root element, so the whole app is usable from the couch. Several rules come from
/// nextestudios' controller PR (#17): foreground gating by HWND, first press only shows
/// the focus ring, tab-order fallback when the spatial search finds nothing, ComboBox handling.
/// </summary>
public sealed class GamepadNavigator : IDisposable
{
    private readonly ControllerInput _input;
    private readonly DependencyObject _root;

    /// <summary>Back was pressed and no control consumed it.</summary>
    public event Action? BackRequested;
    public event Action? MenuRequested;
    public event Action? AltRequested;
    public event Action? OptionRequested;

    /// <summary>L1/LB (-1) and R1/RB (+1): switch tab.</summary>
    public event Action<int>? TabRequested;

    /// <summary>Return true to consume a direction (e.g. a slider row using Left/Right).</summary>
    public Func<FocusNavigationDirection, bool>? BeforeMove { get; set; }

    /// <summary>
    /// Where focus moves may land. Overlays sit on top of screens that stay visible, so the
    /// spatial search would otherwise pick controls hidden behind the open overlay.
    /// </summary>
    public Func<DependencyObject>? SearchRoot { get; set; }

    /// <summary>Play the interface sounds for moves, confirms and backs (the console interface).</summary>
    public bool Sounds { get; set; }

    private void Play(UiSound sound)
    {
        if (Sounds) UiSounds.Play(sound);
    }

    /// <summary>Handles a press before focus logic; return true to swallow it (e.g. tour tips).</summary>
    public Func<ControllerAction, bool>? Intercept { get; set; }

    /// <param name="hwnd">When given, presses only count while this window is in the foreground.</param>
    /// <param name="readSonyHid">Also read PlayStation pads over HID, for windows that may not get the foreground.</param>
    public GamepadNavigator(DispatcherQueue dispatcher, DependencyObject root, nint hwnd = 0, bool readSonyHid = false)
    {
        _root = root;
        _input = new ControllerInput(dispatcher) { ReadSonyHid = readSonyHid };
        if (hwnd != 0) _input.IsActive = () => NativeWindows.GetForegroundWindow() == hwnd;
        _input.Pressed += OnPressed;
    }

    public bool IsRunning => _input.IsRunning;

    public void Start() => _input.Start();

    public void Stop() => _input.Stop();

    public void Dispose()
    {
        _input.Pressed -= OnPressed;
        _input.Dispose();
    }

    private void OnPressed(ControllerAction action)
    {
        try
        {
            if (Intercept?.Invoke(action) == true) return;
            switch (action)
            {
                case ControllerAction.Up: Move(FocusNavigationDirection.Up); break;
                case ControllerAction.Down: Move(FocusNavigationDirection.Down); break;
                case ControllerAction.Left: Move(FocusNavigationDirection.Left); break;
                case ControllerAction.Right: Move(FocusNavigationDirection.Right); break;
                case ControllerAction.Confirm: Confirm(); break;
                case ControllerAction.Back: Play(UiSound.Back); Back(); break;
                case ControllerAction.Menu: MenuRequested?.Invoke(); break;
                case ControllerAction.Alt: AltRequested?.Invoke(); break;
                case ControllerAction.Option: OptionRequested?.Invoke(); break;
                case ControllerAction.PreviousTab: TabRequested?.Invoke(-1); break;
                case ControllerAction.NextTab: TabRequested?.Invoke(1); break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controles: navegação: {ex.Message}");
        }
    }

    private XamlRoot? GetXamlRoot() => (_root as UIElement)?.XamlRoot;

    private Control? Focused() =>
        GetXamlRoot() is { } root && FocusManager.GetFocusedElement(root) is Control c && IsShown(c) ? c : null;

    /// <summary>After a mouse click or at startup focus has no ring; the first press only reveals it.</summary>
    private bool RevealFocus(Control control)
    {
        if (control is ComboBoxItem || control.FocusState == FocusState.Keyboard) return false;
        control.Focus(FocusState.Keyboard);
        return true;
    }

    private DependencyObject CurrentRoot() => SearchRoot?.Invoke() ?? _root;

    private void FocusFirst() => (FocusManager.FindFirstFocusableElement(CurrentRoot()) as Control)?.Focus(FocusState.Keyboard);

    /// <summary>Move the focus one step (also used by the keyboard arrows, so both behave the same).</summary>
    public void Navigate(FocusNavigationDirection direction) => Move(direction);

    private Control? FindSpatial(FocusNavigationDirection direction) =>
        FocusManager.FindNextElement(direction, new FindNextElementOptions { SearchRoot = CurrentRoot() }) as Control;

    /// <summary>The closest control in the direction even when it doesn't overlap the current one's row or column.</summary>
    private Control? FindNearest(FocusNavigationDirection direction) =>
        // Left/right stay on their own row: jumping to another row from the end of one is confusing.
        direction is not (FocusNavigationDirection.Up or FocusNavigationDirection.Down) ? null :
        FocusManager.FindNextElement(direction, new FindNextElementOptions
        {
            SearchRoot = CurrentRoot(),
            XYFocusNavigationStrategyOverride = XYFocusNavigationStrategyOverride.NavigationDirectionDistance
        }) as Control;

    /// <summary>
    /// Up/down through everything that can take focus, top to bottom then left to right. (The
    /// Next/Previous directions can't be used with a search root: they throw.)
    /// </summary>
    private Control? FindInReadingOrder(Control current, FocusNavigationDirection direction)
    {
        if (direction is not (FocusNavigationDirection.Up or FocusNavigationDirection.Down)) return null;
        var root = CurrentRoot();
        var all = new List<Control>();
        Collect(root, all);
        if (root is not UIElement rootElement) return null;

        (double Y, double X) Position(Control c)
        {
            var point = c.TransformToVisual(rootElement).TransformPoint(new Windows.Foundation.Point(0, 0));
            return (Math.Round(point.Y / 8) * 8, point.X);
        }
        var ordered = all.OrderBy(c => Position(c).Y).ThenBy(c => Position(c).X).ToList();
        var index = ordered.IndexOf(current);
        if (index < 0) return null;
        var next = index + (direction == FocusNavigationDirection.Down ? 1 : -1);
        return next >= 0 && next < ordered.Count ? ordered[next] : null;
    }

    private static void Collect(DependencyObject node, List<Control> into)
    {
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is UIElement { Visibility: Visibility.Collapsed }) continue;
            if (child is Control { IsTabStop: true, IsEnabled: true, ActualWidth: > 0, ActualHeight: > 0 } control
                && control is not ComboBoxItem)
                into.Add(control);
            Collect(child, into);
        }
    }

    private void Move(FocusNavigationDirection direction)
    {
        if (BeforeMove?.Invoke(direction) == true) return;
        if (Focused() is not { } control) { FocusFirst(); return; }
        if (RevealFocus(control)) return;

        // Inside an open ComboBox the arrows walk its items instead of leaving the popup.
        if (control is ComboBoxItem item && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo)
        {
            if (direction is not (FocusNavigationDirection.Up or FocusNavigationDirection.Down)) return;
            var index = combo.IndexFromContainer(item) + (direction == FocusNavigationDirection.Down ? 1 : -1);
            if (index >= 0 && index < combo.Items.Count && combo.ContainerFromIndex(index) is Control next)
            {
                next.Focus(FocusState.Keyboard);
                Play(UiSound.Move);
            }
            return;
        }

        var target = FindSpatial(direction)
                     // Nothing lines up (a tile far to the right, going up to a row that only reaches the left):
                     // take the nearest one in that direction instead of doing nothing.
                     ?? FindNearest(direction)
                     // Scrolled lists: controls outside the viewport aren't in a spatial search at all.
                     ?? FindInReadingOrder(control, direction);
        if (target is null) return;
        target.Focus(FocusState.Keyboard);
        Play(UiSound.Move);
    }

    private void Confirm()
    {
        if (Focused() is not { } control) { FocusFirst(); return; }
        if (RevealFocus(control)) return;
        Play(UiSound.Confirm);
        Invoke(control);
    }

    private void Back()
    {
        if (Focused() is ComboBoxItem item && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo)
        {
            combo.IsDropDownOpen = false;
            combo.Focus(FocusState.Keyboard);
            return;
        }
        BackRequested?.Invoke();
    }

    /// <summary>A = activate whatever has focus: pick a list item, open a list, flip a switch, click a button.</summary>
    public static void Invoke(object? focused)
    {
        switch (focused)
        {
            case ComboBoxItem item when ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo:
                combo.SelectedIndex = combo.IndexFromContainer(item);
                combo.IsDropDownOpen = false;
                combo.Focus(FocusState.Keyboard);
                break;
            case ComboBox combo:
                combo.IsDropDownOpen = true;
                break;
            case ToggleSwitch sw:
                sw.IsOn = !sw.IsOn;
                break;
            case SelectorItem selectorItem:
                selectorItem.IsSelected = true;
                break;
            case ButtonBase button:
                var peer = FrameworkElementAutomationPeer.FromElement(button) ?? FrameworkElementAutomationPeer.CreatePeerForElement(button);
                if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke) invoke.Invoke();
                else if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle) toggle.Toggle();
                // e.g. SettingsCard: a ButtonBase whose peer has no Invoke pattern.
                else if (button.Command?.CanExecute(button.CommandParameter) == true) button.Command.Execute(button.CommandParameter);
                break;
        }
    }

    private static bool IsShown(DependencyObject element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: Visibility.Collapsed }) return false;
        }
        return true;
    }
}
