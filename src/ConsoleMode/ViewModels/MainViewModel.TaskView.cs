using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The Task View shortcut: Windows' Win + Tab from the controller, in a session or not, driven with the pad while it
// is open (TaskViewDriver). The controller mouse waits meanwhile, so the stick moves the selection, not the pointer.
public partial class MainViewModel
{
    private readonly TaskViewDriver _taskView = new();
    private bool _taskViewHooked;

    /// <summary>Task View is open from the shortcut and the pad is driving it.</summary>
    [ObservableProperty] private bool _isTaskViewOpen;

    partial void OnIsTaskViewOpenChanged(bool value) => UpdateSessionPointer();

    /// <summary>The shortcut: opens Task View, or closes it when it was opened from here.</summary>
    public void ToggleTaskView()
    {
        if (!_taskViewHooked)
        {
            _taskViewHooked = true;
            _taskView.Stopped += () => _dispatcher.TryEnqueue(() => IsTaskViewOpen = _taskView.IsRunning);
        }
        // The session menu sits on top of everything: Task View opens instead of it.
        if (!_taskView.IsRunning && IsSessionMenuOpen) CloseSessionMenu();
        try
        {
            _taskView.Toggle();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Visão de tarefas: {ex.Message}");
        }
        IsTaskViewOpen = _taskView.IsRunning;
    }
}
