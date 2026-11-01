using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The Task View shortcut: Windows' Win + Tab from the controller, in a session or not. Windows moves Task View with an
// Xbox controller itself; a PlayStation one is driven from here while it is open (TaskViewDriver, optional). The
// controller mouse waits meanwhile, so the stick moves the selection, not the pointer.
// The first few times, a toast shows how to drive it before it opens: nothing of ours can show over Task View
// (Windows draws it above every always-on-top window), so the hint comes first and Task View a moment later.
public partial class MainViewModel
{
    /// <summary>The first uses that show how to drive Task View before it opens; after them it opens at once.</summary>
    private const int TaskViewHintUses = 3;
    private static readonly TimeSpan TaskViewHintLead = TimeSpan.FromMilliseconds(1600);

    private readonly TaskViewDriver _taskView = new();
    private bool _taskViewHooked;
    private bool _taskViewPending;
    private int _taskViewHintsShown;

    /// <summary>Task View is open from the shortcut and the pad is driving it.</summary>
    [ObservableProperty] private bool _isTaskViewOpen;

    partial void OnIsTaskViewOpenChanged(bool value) => UpdateSessionPointer();

    /// <summary>System → Controller: a PlayStation controller drives Task View (Windows already does it for Xbox ones).</summary>
    [ObservableProperty] private bool _taskViewPlayStation = true;

    partial void OnTaskViewPlayStationChanged(bool value)
    {
        _taskView.DrivePlayStation = value;
        SaveQuietly();
    }

    /// <summary>The shortcut: opens Task View, or closes it when it was opened from here.</summary>
    public void ToggleTaskView()
    {
        if (!_taskViewHooked)
        {
            _taskViewHooked = true;
            _taskView.Stopped += () => _dispatcher.TryEnqueue(() => IsTaskViewOpen = _taskView.IsRunning);
        }
        // Already on its way (the hint is showing): one press is enough.
        if (_taskViewPending) return;
        if (_taskView.IsRunning)
        {
            RunTaskView();
            return;
        }
        // The session menu sits on top of everything: Task View opens instead of it.
        if (IsSessionMenuOpen) CloseSessionMenu();
        if (_taskViewHintsShown >= TaskViewHintUses)
        {
            RunTaskView();
            return;
        }

        _taskViewHintsShown++;
        SaveQuietly();
        ShowTaskViewHint();
        _taskViewPending = true;
        var timer = _dispatcher.CreateTimer();
        timer.Interval = TaskViewHintLead;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _taskViewPending = false;
            RunTaskView();
        };
        timer.Start();
    }

    private void RunTaskView()
    {
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

    /// <summary>"Task View: the D-pad or stick picks a window · A opens · B closes", with the buttons of the pad in use.</summary>
    private void ShowTaskViewHint()
    {
        try
        {
            var screen = IsConsoleActive
                ? Engine.State.FocusMonitorRect
                : Engine.Monitors.GetMonitorRect(SessionMonitorName, Engine.State);
            // Long enough to read before Task View covers it; it slides out behind Task View.
            _ = new SessionHintWindow(LocalizationService.Get("ShortcutTaskView"),
                LocalizationService.Get("TaskViewHint", HintConfirm, HintBack),
                screen, TaskViewHintLead + TimeSpan.FromMilliseconds(400), glyph: "");
            AppLog.Write($"Visão de tarefas: dica exibida ({_taskViewHintsShown}/{TaskViewHintUses})");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Visão de tarefas: dica: {ex.Message}");
        }
    }
}
