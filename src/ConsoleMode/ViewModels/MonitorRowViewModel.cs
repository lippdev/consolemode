using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

public partial class MonitorRowViewModel : ObservableObject
{
    private readonly Action<MonitorRowViewModel> _onFocusChanged;

    public MonitorInfo Monitor { get; }

    public MonitorRowViewModel(MonitorInfo monitor, bool isFocus, bool isHide, DisplayModeOption selected, IReadOnlyList<DisplayModeOption> modes, Action<MonitorRowViewModel> onFocusChanged)
    {
        Monitor = monitor;
        _isFocus = isFocus;
        _isHide = isHide;
        Modes = modes;
        _selectedMode = selected;
        _onFocusChanged = onFocusChanged;
    }

    public string Title => Monitor.DisplayTitle;

    [ObservableProperty]
    private bool _isFocus;

    [ObservableProperty]
    private bool _isHide;

    public IReadOnlyList<DisplayModeOption> Modes { get; }

    [ObservableProperty]
    private DisplayModeOption _selectedMode;

    partial void OnIsFocusChanged(bool value)
    {
        if (value) _onFocusChanged(this);
    }
}
