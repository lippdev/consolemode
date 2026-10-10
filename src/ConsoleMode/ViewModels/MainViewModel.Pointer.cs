using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The mouse pointer in a session, both from the session menu and per session (off when it ends):
// "Hide the mouse pointer" hides it until turned off, nothing automatic; "Control the mouse" lets
// the controller drive it. One excludes the other: an invisible pointer can't be steered.
public partial class MainViewModel
{
    private readonly ControllerMouse _controllerMouse = new();

    /// <summary>Session menu: the pointer is hidden until this is turned off or the session ends. Never saved.</summary>
    [ObservableProperty] private bool _isCursorHidden;

    /// <summary>Session menu: the sticks and A drive the mouse. Never saved; every session starts with it off.</summary>
    [ObservableProperty] private bool _isControllerMouseOn;

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

    /// <summary>Brings the pointer and the controller mouse in line with the session, the menu and the choices.</summary>
    private void UpdateSessionPointer()
    {
        if (!IsConsoleActive && (IsControllerMouseOn || IsCursorHidden))
        {
            // The session ended: both go back to off (re-enters here).
            IsControllerMouseOn = false;
            IsCursorHidden = false;
            return;
        }

        CursorHider.SetHidden(IsConsoleActive && IsCursorHidden);

        // While the menu is open the pad drives the menu, not the mouse.
        _controllerMouse.Paused = IsSessionMenuOpen;
        if (IsConsoleActive && IsControllerMouseOn)
            _controllerMouse.Start(Engine.State.FocusMonitorRect?.Height ?? 1080);
        else
            _controllerMouse.Stop();
    }
}
