using CommunityToolkit.Mvvm.ComponentModel;

namespace ConsoleMode.ViewModels;

// Settings that tune a session without changing how it works: the user's own scripts around it, the TV's
// exact (NTSC) refresh rate, and parking the mouse pointer. Each saves as soon as it changes.
public partial class MainViewModel
{
    /// <summary>A command that runs when a session starts, before the screens change.</summary>
    [ObservableProperty] private string _startScript = "";

    /// <summary>A command that runs after the desk is restored.</summary>
    [ObservableProperty] private string _stopScript = "";

    /// <summary>119.88 Hz instead of 120 Hz, 59.94 instead of 60, on the game screen.</summary>
    [ObservableProperty] private bool _ntscRefresh;

    /// <summary>Move the mouse pointer to a corner of the game screen once the launcher is up.</summary>
    [ObservableProperty] private bool _parkPointer;

    partial void OnStartScriptChanged(string value) => SaveQuietly();
    partial void OnStopScriptChanged(string value) => SaveQuietly();
    partial void OnNtscRefreshChanged(bool value) => SaveQuietly();
    partial void OnParkPointerChanged(bool value) => SaveQuietly();
}
