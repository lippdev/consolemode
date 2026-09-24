using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace ConsoleMode.Services;

/// <summary>
/// WinUI 3 desktop apps get keyboard focus navigation for free but nothing from a
/// controller. This turns controller presses into focus moves and invokes, scoped to one
/// root element, so the console interface is fully usable from the couch.
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

    public GamepadNavigator(DispatcherQueue dispatcher, DependencyObject root)
    {
        _root = root;
        _input = new ControllerInput(dispatcher);
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
        switch (action)
        {
            case ControllerAction.Up: Move(FocusNavigationDirection.Up); break;
            case ControllerAction.Down: Move(FocusNavigationDirection.Down); break;
            case ControllerAction.Left: Move(FocusNavigationDirection.Left); break;
            case ControllerAction.Right: Move(FocusNavigationDirection.Right); break;
            case ControllerAction.Confirm: Invoke(FocusManager.GetFocusedElement(GetXamlRoot())); break;
            case ControllerAction.Back: BackRequested?.Invoke(); break;
            case ControllerAction.Menu: MenuRequested?.Invoke(); break;
            case ControllerAction.Alt: AltRequested?.Invoke(); break;
            case ControllerAction.Option: OptionRequested?.Invoke(); break;
        }
    }

    private XamlRoot? GetXamlRoot() => (_root as UIElement)?.XamlRoot;

    private void Move(FocusNavigationDirection direction)
    {
        var options = new FindNextElementOptions { SearchRoot = _root };
        if (FocusManager.GetFocusedElement(GetXamlRoot()) is null)
        {
            // Nothing focused yet: land on the first focusable control.
            (FocusManager.FindFirstFocusableElement(_root) as Control)?.Focus(FocusState.Keyboard);
            return;
        }
        FocusManager.TryMoveFocus(direction, options);
    }

    /// <summary>A = click the focused control, or flip it when it is a switch.</summary>
    public static void Invoke(object? focused)
    {
        switch (focused)
        {
            case ToggleSwitch sw:
                sw.IsOn = !sw.IsOn;
                break;
            case ButtonBase button:
                if (FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
                    invoke.Invoke();
                else if (FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
                    toggle.Toggle();
                break;
        }
    }
}
