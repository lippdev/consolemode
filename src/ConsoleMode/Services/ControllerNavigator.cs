using System.Runtime.InteropServices;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ConsoleMode.Services;

/// <summary>
/// Drives the main window with a controller, like a console menu: D-pad / left stick move focus,
/// Cross (A) activates, Circle (B) closes a list or goes back, Options (Start) toggles Settings.
/// WinUI 3 desktop apps get no gamepad input from XAML, and PlayStation pads aren't gamepads to
/// Windows at all, so the pads are polled and focus is moved in-process (no simulated keys).
/// </summary>
public sealed class ControllerNavigator : IDisposable
{
    private readonly Window _window;
    private readonly MainViewModel _viewModel;
    private readonly Func<UIElement?> _defaultFocus;
    private readonly ControllerInput _input;
    private readonly nint _hwnd;

    public ControllerNavigator(Window window, MainViewModel viewModel, Func<UIElement?> defaultFocus)
    {
        _window = window;
        _viewModel = viewModel;
        _defaultFocus = defaultFocus;
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Only while this window is in front: Big Picture, the TV prompt and other apps keep the pad.
        _input = new ControllerInput(window.DispatcherQueue) { IsActive = () => GetForegroundWindow() == _hwnd };
        _input.ButtonDown += OnButtonDown;
        _input.Start();
    }

    public void Dispose() => _input.Dispose();

    private void OnButtonDown(ControllerButtons buttons)
    {
        if (_window.Content is not FrameworkElement { XamlRoot: { } root }) return;

        try
        {
            // Tour coach marks live in popups outside the focus tree: A goes on, B skips the tour.
            if (_viewModel.TourStep > 0)
            {
                if (Has(buttons, ControllerButtons.Confirm)) _viewModel.TourNextCommand.Execute(null);
                else if (Has(buttons, ControllerButtons.Back)) _viewModel.EndTourCommand.Execute(null);
                return;
            }

            if (Has(buttons, ControllerButtons.Menu))
            {
                if (_viewModel.IsSettingsPage) _viewModel.GoHomeCommand.Execute(null);
                else _viewModel.OpenSettingsCommand.Execute(null);
                FocusDefaultLater();
                return;
            }

            var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
            if (focused is not Control control || !IsShown(control))
            {
                FocusDefault();
                return;
            }

            // First press only shows where focus is (after a mouse click or at startup it has no ring).
            if (control is not ComboBoxItem && control.FocusState != FocusState.Keyboard)
            {
                control.Focus(FocusState.Keyboard);
                return;
            }

            if (Has(buttons, ControllerButtons.Confirm)) Activate(control);
            else if (Has(buttons, ControllerButtons.Back)) GoBack(control);
            else if (DirectionOf(buttons) is { } direction) Move(control, direction, root);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controles: navegação: {ex.Message}");
        }
    }

    private static void Activate(Control control)
    {
        switch (control)
        {
            case ComboBoxItem item when ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo:
                combo.SelectedIndex = combo.IndexFromContainer(item);
                combo.IsDropDownOpen = false;
                combo.Focus(FocusState.Keyboard);
                break;
            case ComboBox combo:
                combo.IsDropDownOpen = true;
                break;
            case ToggleSwitch toggle:
                toggle.IsOn = !toggle.IsOn;
                break;
            case SelectorItem selectorItem:
                selectorItem.IsSelected = true;
                break;
            case ButtonBase button:
                Invoke(button);
                break;
        }
    }

    private static void Invoke(ButtonBase button)
    {
        var peer = FrameworkElementAutomationPeer.FromElement(button) ?? FrameworkElementAutomationPeer.CreatePeerForElement(button);
        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoker)
        {
            invoker.Invoke();
        }
        else if (button.Command?.CanExecute(button.CommandParameter) == true)
        {
            // e.g. SettingsCard: a ButtonBase whose peer has no Invoke pattern.
            button.Command.Execute(button.CommandParameter);
        }
    }

    private void GoBack(Control control)
    {
        if (control is ComboBoxItem item && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo)
        {
            combo.IsDropDownOpen = false;
            combo.Focus(FocusState.Keyboard);
            return;
        }

        if (_viewModel.IsSettingsPage)
        {
            _viewModel.GoHomeCommand.Execute(null);
            FocusDefaultLater();
        }
    }

    private static void Move(Control control, FocusNavigationDirection direction, XamlRoot root)
    {
        // An open list scrolls through its items instead of leaving the popup.
        if (control is ComboBoxItem item && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox combo)
        {
            if (direction is not (FocusNavigationDirection.Up or FocusNavigationDirection.Down)) return;
            var index = combo.IndexFromContainer(item) + (direction == FocusNavigationDirection.Down ? 1 : -1);
            if (index >= 0 && index < combo.Items.Count && combo.ContainerFromIndex(index) is Control next)
                next.Focus(FocusState.Keyboard);
            return;
        }

        // Spatial (XY) search from the focused control, so closed ComboBoxes and the role selector
        // don't swallow the arrows the way they would with keyboard input.
        var options = new FindNextElementOptions { SearchRoot = root.Content };
        var target = FocusManager.FindNextElement(direction, options) as Control;

        // Settings scrolls: XY search can miss controls outside the viewport, so up/down fall
        // back to tab order, which is top-to-bottom there and does reach them (Focus scrolls).
        target ??= direction switch
        {
            FocusNavigationDirection.Down => FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as Control,
            FocusNavigationDirection.Up => FocusManager.FindNextElement(FocusNavigationDirection.Previous, options) as Control,
            _ => null
        };
        target?.Focus(FocusState.Keyboard);
    }

    private void FocusDefault()
    {
        if (_defaultFocus() is Control target) target.Focus(FocusState.Keyboard);
    }

    /// <summary>After a page switch the new page only becomes focusable once it is laid out.</summary>
    private void FocusDefaultLater() =>
        _window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, FocusDefault);

    private static bool IsShown(DependencyObject element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: Visibility.Collapsed }) return false;
        }
        return true;
    }

    private static FocusNavigationDirection? DirectionOf(ControllerButtons buttons)
    {
        // A diagonal on the D-pad or stick moves vertically, the common case in these lists.
        if (Has(buttons, ControllerButtons.Up)) return FocusNavigationDirection.Up;
        if (Has(buttons, ControllerButtons.Down)) return FocusNavigationDirection.Down;
        if (Has(buttons, ControllerButtons.Left)) return FocusNavigationDirection.Left;
        if (Has(buttons, ControllerButtons.Right)) return FocusNavigationDirection.Right;
        return null;
    }

    private static bool Has(ControllerButtons buttons, ControllerButtons flag) => (buttons & flag) != 0;

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
