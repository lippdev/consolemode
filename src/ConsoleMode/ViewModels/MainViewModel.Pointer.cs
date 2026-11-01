using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The mouse pointer, from the session menu, in a session or outside one (both go back to off when a
// session starts or ends): "Hide the mouse pointer" hides it until turned off, nothing automatic;
// "Control the mouse" lets the controller drive it. One excludes the other: an invisible pointer
// can't be steered.
public partial class MainViewModel
{
    private readonly ControllerMouse _controllerMouse = new();

    /// <summary>Session menu: the pointer is hidden until this is turned off or a session starts or ends. Never saved.</summary>
    [ObservableProperty] private bool _isCursorHidden;

    /// <summary>Session menu: the sticks and A drive the mouse. Never saved; every session starts with it off.</summary>
    [ObservableProperty] private bool _isControllerMouseOn;

    /// <summary>The main window has the focus: the pad drives its interface then, not the mouse (set by the window).</summary>
    [ObservableProperty] private bool _isMainWindowActive;

    partial void OnIsMainWindowActiveChanged(bool value) => UpdateSessionPointer();

    /// <summary>A session started or ended: what the menu turned on for the pointer goes back to off.</summary>
    private void ResetSessionPointer()
    {
        IsControllerMouseOn = false;
        IsCursorHidden = false;
        UpdateSessionPointer();
    }

    /// <summary>The help line under the menu row, with the confirm button of the pad in use (A or ✕).</summary>
    public string ControllerMouseHint => LocalizationService.Get("ControllerMouseHint", HintConfirm);

    partial void OnIsCursorHiddenChanged(bool value)
    {
        if (value) IsControllerMouseOn = false;
        UpdateSessionPointer();
    }

    partial void OnIsControllerMouseOnChanged(bool value)
    {
        if (value) IsCursorHidden = false;
        UpdateSessionPointer();
    }

    partial void OnIsSessionMenuOpenChanged(bool value)
    {
        if (value) OnPropertyChanged(nameof(ControllerMouseHint));
        UpdateSessionPointer();
    }

    [RelayCommand]
    private void ToggleCursorHidden() => IsCursorHidden = !IsCursorHidden;

    [RelayCommand]
    private void ToggleControllerMouse() => IsControllerMouseOn = !IsControllerMouseOn;

    /// <summary>Brings the pointer and the controller mouse in line with the menu and the choices.</summary>
    private void UpdateSessionPointer()
    {
        CursorHider.SetHidden(IsCursorHidden);

        // While the menu, the main window or Task View (from the shortcut) is up the pad drives it, not the mouse.
        _controllerMouse.Paused = IsSessionMenuOpen || IsMainWindowActive || IsTaskViewOpen;
        if (IsControllerMouseOn)
            _controllerMouse.Start((IsConsoleActive ? Engine.State.FocusMonitorRect : Engine.Monitors.GetMonitorRect(SessionMonitorName, Engine.State))?.Height ?? 1080);
        else
            _controllerMouse.Stop();
    }
}
