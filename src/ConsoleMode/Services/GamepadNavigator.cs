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

    /// <summary>Return true to consume a direction (e.g. a slider row using Left/Right).</summary>
    public Func<FocusNavigationDirection, bool>? BeforeMove { get; set; }

    /// <summary>Handles a press before focus logic; return true to swallow it (e.g. tour tips).</summary>
    public Func<ControllerAction, bool>? Intercept { get; set; }

    /// <param name="hwnd">When given, presses only count while this window is in the foreground.</param>
    public GamepadNavigator(DispatcherQueue dispatcher, DependencyObject root, nint hwnd = 0)
    {
        _root = root;
        _input = new ControllerInput(dispatcher);
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
                case ControllerAction.Back: Back(); break;
                case ControllerAction.Menu: MenuRequested?.Invoke(); break;
                case ControllerAction.Alt: AltRequested?.Invoke(); break;
                case ControllerAction.Option: OptionRequested?.Invoke(); break;
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

    private void FocusFirst() => (FocusManager.FindFirstFocusableElement(_root) as Control)?.Focus(FocusState.Keyboard);

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
                next.Focus(FocusState.Keyboard);
            return;
        }

        var options = new FindNextElementOptions { SearchRoot = _root };
        var target = FocusManager.FindNextElement(direction, options) as Control;
        // Scrolled lists: the spatial search can miss controls outside the viewport, so up/down
        // fall back to tab order, which does reach them (Focus scrolls them into view).
        target ??= direction switch
        {
            FocusNavigationDirection.Down => FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as Control,
            FocusNavigationDirection.Up => FocusManager.FindNextElement(FocusNavigationDirection.Previous, options) as Control,
            _ => null
        };
        target?.Focus(FocusState.Keyboard);
    }

    private void Confirm()
    {
        if (Focused() is not { } control) { FocusFirst(); return; }
        if (RevealFocus(control)) return;
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
