using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The mouse pointer in a session: hidden while it sits still (setting, on by default), and driven by the
// controller when the session menu's "Control the mouse" is on (per session, off when it ends).
public partial class MainViewModel
{
    private CursorHider? _cursorHider;
    private readonly ControllerMouse _controllerMouse = new();

    /// <summary>Settings: hide the pointer during a session after a few seconds still.</summary>
    [ObservableProperty] private bool _hideCursor = true;

    /// <summary>Session menu: the sticks and A drive the mouse. Never saved; every session starts with it off.</summary>
    [ObservableProperty] private bool _isControllerMouseOn;

    /// <summary>The help line under the menu row, with the confirm button of the pad in use (A or ✕).</summary>
    public string ControllerMouseHint => LocalizationService.Get("ControllerMouseHint", HintConfirm);

    partial void OnHideCursorChanged(bool value)
    {
        SaveQuietly();
        UpdateSessionPointer();
    }

    partial void OnIsControllerMouseOnChanged(bool value) => UpdateSessionPointer();

    partial void OnIsSessionMenuOpenChanged(bool value)
    {
        if (value) OnPropertyChanged(nameof(ControllerMouseHint));
        UpdateSessionPointer();
    }

    [RelayCommand]
    private void ToggleControllerMouse() => IsControllerMouseOn = !IsControllerMouseOn;

    /// <summary>Brings the hider and the controller mouse in line with the session, the menu and the choices.</summary>
    private void UpdateSessionPointer()
    {
        if (!IsConsoleActive && IsControllerMouseOn)
        {
            IsControllerMouseOn = false;   // re-enters here
            return;
        }

        if (IsConsoleActive && HideCursor) (_cursorHider ??= new CursorHider(_dispatcher)).Start();
        else _cursorHider?.Stop();

        // While the menu is open the pad drives the menu, not the mouse.
        _controllerMouse.Paused = IsSessionMenuOpen;
        if (IsConsoleActive && IsControllerMouseOn)
            _controllerMouse.Start(Engine.State.FocusMonitorRect?.Height ?? 1080);
        else
            _controllerMouse.Stop();
    }
}
