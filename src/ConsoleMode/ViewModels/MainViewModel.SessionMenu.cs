using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ConsoleMode.ViewModels;

// The in-session menu (Select + Y): a window over the game with volume, resolution, audio,
// FPS, HDR and the two ways out. Everything here runs only while a session is active.
public partial class MainViewModel
{
    private SessionMenuWindow? _sessionMenu;
    private DispatcherQueueTimer? _sessionClockTimer;
    private string _sessionPickerKey = "";

    public ObservableCollection<PickerItem> SessionPickerOptions { get; } = [];

    [ObservableProperty] private bool _isSessionMenuOpen;
    [ObservableProperty] private bool _isSessionMenuBusy;
    [ObservableProperty] private bool _isSessionPickerOpen;
    [ObservableProperty] private string _sessionPickerTitle = "";
    [ObservableProperty] private string _sessionClock = "";
    [ObservableProperty] private string _sessionElapsed = "";
    [ObservableProperty] private string _sessionControllerText = "";
    [ObservableProperty] private int _volumePercent = -1;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _sessionHdrOn;
    [ObservableProperty] private string _sessionModeText = "";
    [ObservableProperty] private string _sessionAudioText = "";
    [ObservableProperty] private string _sessionFpsText = "";

    public bool IsFpsMenuAvailable => Engine.Rtss.IsReady;
    public string VolumeText => VolumePercent < 0 ? "—" : IsMuted ? LocalizationService.Get("Muted") : $"{VolumePercent}%";
    public string RecordRowText => LocalizationService.Get("RecordLast30") + LocalizationService.Get("ComingSoonSuffix");

    partial void OnVolumePercentChanged(int value) => OnPropertyChanged(nameof(VolumeText));
    partial void OnIsMutedChanged(bool value) => OnPropertyChanged(nameof(VolumeText));

    /// <summary>Select + Y, the tray, or consolemode://menu.</summary>
    public void ToggleSessionMenu()
    {
        if (!IsConsoleActive) return;
        if (IsSessionMenuOpen) { CloseSessionMenu(); return; }

        _sessionMenu = new SessionMenuWindow(this, Engine.State.FocusMonitorRect);
        _sessionMenu.Closed += (_, _) => { _sessionMenu = null; IsSessionMenuOpen = false; StopSessionClock(); };
        IsSessionMenuOpen = true;
        IsSessionPickerOpen = false;
        RefreshSessionHeader();
        StartSessionClock();
        _sessionMenu.Activate();
        _sessionMenu.BringToFront();
        _ = LoadSessionValuesAsync();
        AppLog.Write("Menu da sessão: aberto");
    }

    /// <summary>
    /// Once per session, a few seconds in (the game UI is up by then): a corner toast telling
    /// how to open this menu, with the combo written for the pad in use.
    /// </summary>
    private async Task ShowSessionHintAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        _dispatcher.TryEnqueue(() =>
        {
            if (!IsConsoleActive || IsSessionMenuOpen) return;
            try
            {
                var combo = IsPlayStationHints ? "Create + △" : "Select + Y";
                _ = new SessionHintWindow(LocalizationService.Get("SessionHintTitle"),
                    LocalizationService.Get("SessionHintBody", combo),
                    Engine.State.FocusMonitorRect, TimeSpan.FromSeconds(7));
                AppLog.Write("Menu da sessão: aviso exibido");
            }
            catch (Exception ex)
            {
                AppLog.Write($"Menu da sessão: aviso: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private void CloseSessionMenu()
    {
        IsSessionPickerOpen = false;
        _sessionMenu?.Close();
    }

    /// <summary>B: closes the picker first, then the menu.</summary>
    public void SessionMenuBack()
    {
        if (IsSessionPickerOpen) IsSessionPickerOpen = false;
        else CloseSessionMenu();
    }

    private void StartSessionClock()
    {
        _sessionClockTimer ??= _dispatcher.CreateTimer();
        _sessionClockTimer.Interval = TimeSpan.FromSeconds(1);
        _sessionClockTimer.Tick += OnSessionClockTick;
        _sessionClockTimer.Start();
    }

    private void StopSessionClock()
    {
        if (_sessionClockTimer is null) return;
        _sessionClockTimer.Stop();
        _sessionClockTimer.Tick -= OnSessionClockTick;
    }

    private void OnSessionClockTick(DispatcherQueueTimer sender, object args) => RefreshSessionHeader();

    private void RefreshSessionHeader()
    {
        SessionClock = DateTime.Now.ToString("HH:mm");
        var started = Engine.State.SessionStartedAt ?? DateTime.Now;
        SessionElapsed = LocalizationService.Get("SessionElapsed", SessionMenuMath.FormatElapsed(DateTime.Now - started));
        SessionControllerText = ControllerInput.DetectFamily() switch
        {
            ControllerFamily.Xbox => LocalizationService.Get("ControllerXbox"),
            ControllerFamily.PlayStation => LocalizationService.Get("ControllerPlaystation"),
            ControllerFamily.Other => LocalizationService.Get("ControllerOther"),
            _ => LocalizationService.Get("ControllerNone")
        };
    }

    private string? SessionAudioId => string.IsNullOrWhiteSpace(Engine.State.AudioDeviceId) ? Engine.Audio.GetDefaultId() : Engine.State.AudioDeviceId;

    private async Task LoadSessionValuesAsync()
    {
        var state = Engine.State;
        var focus = state.FocusMonitor ?? "";
        var (mode, hdr, audioName, volume) = await Task.Run(() =>
        {
            var current = NativeWindows.GetCurrentDisplayMode(focus);
            var modeText = current is null ? "" : $"{current.Width} × {current.Height} @ {current.Frequency} Hz";
            var hdrOn = Engine.Video.IsHdrOn(focus);
            var id = SessionAudioId;
            var name = Engine.Audio.GetDevices().FirstOrDefault(d => d.FriendlyId == id)?.Name ?? LocalizationService.Get("AudioNoChange");
            int? vol = null;
            try { if (id is not null) vol = SessionMenuMath.ParseVolumeExitCode(Engine.Audio.InvokeSvv("/GetPercent", id)); } catch { /* svv optional */ }
            return (modeText, hdrOn, name, vol);
        });
        SessionModeText = mode;
        SessionHdrOn = hdr;
        SessionAudioText = audioName;
        VolumePercent = volume ?? -1;
        SessionFpsText = state.FpsLimit > 0 ? $"{state.FpsLimit} FPS" : LocalizationService.Get("FpsNoLimit");
        OnPropertyChanged(nameof(IsFpsMenuAvailable));
    }

    /// <summary>Left/Right on the volume row.</summary>
    public void ChangeVolume(int direction)
    {
        if (IsSessionMenuBusy || SessionAudioId is not { } id) return;
        var next = SessionMenuMath.StepVolume(Math.Max(VolumePercent, 0), direction);
        VolumePercent = next;
        IsMuted = false;
        _ = Task.Run(() =>
        {
            try { Engine.Audio.InvokeSvv("/SetVolume", id, next.ToString()); Engine.Audio.InvokeSvv("/Unmute", id); }
            catch (Exception ex) { AppLog.Write($"Volume: {ex.Message}"); }
        });
    }

    [RelayCommand]
    private void ToggleMute()
    {
        if (IsSessionMenuBusy || SessionAudioId is not { } id) return;
        IsMuted = !IsMuted;
        var muted = IsMuted;
        _ = Task.Run(() =>
        {
            try { Engine.Audio.InvokeSvv(muted ? "/Mute" : "/Unmute", id); }
            catch (Exception ex) { AppLog.Write($"Mudo: {ex.Message}"); }
        });
    }

    [RelayCommand]
    private void OpenSessionPicker(string key)
    {
        if (IsSessionMenuBusy) return;
        var state = Engine.State;
        var focus = state.FocusMonitor ?? "";
        IEnumerable<(string Text, string Value, bool Selected)> options;
        string title;
        switch (key)
        {
            case "mode":
                title = LocalizationService.Get("ResolutionRefreshCard");
                var current = NativeWindows.GetCurrentDisplayMode(focus);
                options = NativeWindows.EnumerateDisplayModes(focus)
                    .Select(m => ($"{m.Width} × {m.Height} @ {m.Frequency} Hz", $"{m.Width}x{m.Height}@{m.Frequency}",
                        current is not null && m.Width == current.Width && m.Height == current.Height && m.Frequency == current.Frequency));
                break;
            case "audio":
                title = LocalizationService.Get("AudioOutputCard");
                var id = SessionAudioId;
                options = Engine.Audio.GetDevices().Where(d => d.IsActive).Select(d => (d.Name, d.FriendlyId, d.FriendlyId == id));
                break;
            case "fps":
                title = LocalizationService.Get("FpsCard");
                options = new[] { (LocalizationService.Get("FpsNoLimit"), "0", state.FpsLimit == 0) }
                    .Concat(FpsPresets.Select(f => ($"{f} FPS", f.ToString(), state.FpsLimit == f)));
                break;
            default:
                return;
        }
        _sessionPickerKey = key;
        SessionPickerTitle = title;
        SessionPickerOptions.Clear();
        foreach (var (text, value, selected) in options) SessionPickerOptions.Add(new PickerItem(text, value, selected));
        IsSessionPickerOpen = true;
    }

    [RelayCommand]
    private async Task PickSessionOptionAsync(PickerItem? item)
    {
        if (item is null) return;
        IsSessionPickerOpen = false;
        var key = _sessionPickerKey;
        await RunSessionActionAsync(() =>
        {
            var state = Engine.State;
            switch (key)
            {
                case "mode":
                    var parts = item.Value.Split('x', '@');
                    Engine.Monitors.ApplyFocusMode(state.FocusMonitor ?? "", new SavedDisplayMode
                    {
                        Width = int.Parse(parts[0]), Height = int.Parse(parts[1]), Frequency = int.Parse(parts[2])
                    });
                    Engine.Monitors.UpdateFocusRect(state.FocusMonitor ?? "", state, true);
                    break;
                case "audio":
                    Engine.Audio.SetOutput(item.Value);
                    state.AudioDeviceId = item.Value;
                    state.AudioWatchComplete = true;   // the auto-switch must not undo a manual choice
                    break;
                case "fps":
                    var result = Engine.Rtss.SetLimit(int.Parse(item.Value), state);
                    if (!result.Success) throw new InvalidOperationException(result.Message);
                    break;
            }
        });
        await LoadSessionValuesAsync();
    }

    [RelayCommand]
    private async Task ToggleSessionHdrAsync()
    {
        var focus = Engine.State.FocusMonitor ?? "";
        var target = !SessionHdrOn;
        await RunSessionActionAsync(() =>
        {
            if (!Engine.Video.SetHdrFromMenu(focus, target, Engine.State))
                throw new InvalidOperationException($"HDR {focus}");
        });
        await LoadSessionValuesAsync();
    }

    /// <summary>Blocking engine call off the UI thread, never overlapping the session Tick.</summary>
    private async Task RunSessionActionAsync(Action action)
    {
        if (IsSessionMenuBusy || _busy) return;
        IsSessionMenuBusy = true;
        _busy = true;
        try { await Task.Run(action); }
        catch (Exception ex) { AppLog.Write($"Menu da sessão: {ex.Message}"); }
        finally { _busy = false; IsSessionMenuBusy = false; }
    }

    [RelayCommand]
    private async Task RestoreFromMenuAsync()
    {
        CloseSessionMenu();
        await RestoreNowAsync();
    }

    [RelayCommand]
    private async Task ExitFromMenuAsync()
    {
        CloseSessionMenu();
        await RestoreNowAsync();
        Application.Current.Exit();
    }
}
