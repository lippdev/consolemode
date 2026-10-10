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
// FPS, HDR and the two ways out. In this alpha it also opens outside a session ("preview"), on the
// game screen or the primary one, so it can be tried without entering console mode: nothing that
// needs a restore afterwards (the FPS limit) is offered there.
public partial class MainViewModel
{
    private SessionMenuWindow? _sessionMenu;
    private DispatcherQueueTimer? _sessionClockTimer;
    private string _sessionPickerKey = "";

    public ObservableCollection<PickerItem> SessionPickerOptions { get; } = [];

    [ObservableProperty] private bool _isSessionMenuOpen;
    [ObservableProperty] private bool _isSessionMenuBusy;
    [ObservableProperty] private bool _isSessionMenuPreview;
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
    [ObservableProperty] private string _sessionOverlayText = "";

    public bool IsFpsMenuAvailable => Engine.Rtss.IsReady && !IsSessionMenuPreview;
    /// <summary>The FPS counter only needs RTSS itself (no rtss-cli); like the limit, it is a session thing.</summary>
    public bool IsFpsOverlayAvailable => Engine.Rtss.IsInstalled && !IsSessionMenuPreview;
    public bool IsSessionMenuLive => !IsSessionMenuPreview;

    /// <summary>"Back to the PC" in a session; "Close menu" in the preview, where there is no desk to restore.</summary>
    public string BackToPcText => LocalizationService.Get(IsSessionMenuPreview ? "CloseMenu" : "BackToPc");

    /// <summary>The screen the menu acts on: the session's game screen, or in the preview the chosen game screen if it is on, else the primary one.</summary>
    private string SessionMonitorName => IsConsoleActive
        ? Engine.State.FocusMonitor ?? ""
        : FocusRow?.Monitor is { IsActive: true } game ? game.Name
        // The chosen game screen is often the TV, which is off until a session starts: use a live one.
        : (Monitors.FirstOrDefault(m => m.Monitor.IsPrimary && m.Monitor.IsActive) ?? Monitors.FirstOrDefault(m => m.Monitor.IsActive))?.Monitor.Name ?? "";

    partial void OnIsSessionMenuPreviewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFpsMenuAvailable));
        OnPropertyChanged(nameof(IsFpsOverlayAvailable));
        OnPropertyChanged(nameof(IsSessionMenuLive));
        OnPropertyChanged(nameof(BackToPcText));
    }
    public string VolumeText => VolumePercent < 0 ? "—" : IsMuted ? LocalizationService.Get("Muted") : $"{VolumePercent}%";
    public string RecordRowText => LocalizationService.Get("RecordLast30") + LocalizationService.Get("ComingSoonSuffix");

    partial void OnVolumePercentChanged(int value) => OnPropertyChanged(nameof(VolumeText));
    partial void OnIsMutedChanged(bool value) => OnPropertyChanged(nameof(VolumeText));

    /// <summary>Select + Y, the tray, or consolemode://menu.</summary>
    public void ToggleSessionMenu()
    {
        if (IsSessionMenuOpen) { CloseSessionMenu(); return; }

        IsSessionMenuPreview = !IsConsoleActive;
        var screen = IsConsoleActive
            ? Engine.State.FocusMonitorRect
            : Engine.Monitors.GetMonitorRect(SessionMonitorName, Engine.State);
        try
        {
            _sessionMenu = new SessionMenuWindow(this, screen);
        }
        catch (Exception ex)
        {
            // A menu that fails to build must never take the app (and the session) down with it.
            AppLog.Write($"Menu da sessão: não abriu: {ex}");
            _sessionMenu = null;
            return;
        }
        _sessionMenu.Closed += (_, _) => { _sessionMenu = null; IsSessionMenuOpen = false; StopSessionClock(); DropSessionTour(); };
        IsSessionMenuOpen = true;
        IsSessionPickerOpen = false;
        ControlFsInstalled = ControlFsService.FindExe() is not null;
        // A failed install is offered again each time the menu opens (the connection may be back).
        if (!ControlFsInstalling) ControlFsInstallFailed = false;
        ControlFsJustInstalled = false;
        RefreshSessionHeader();
        StartSessionClock();
        _sessionMenu.Activate();
        _sessionMenu.BringToFront();
        _ = LoadSessionValuesAsync();
        _ = RefreshSwitcherAsync();
        AppLog.Write(IsSessionMenuPreview ? "Menu da sessão: aberto (prévia, fora da sessão)" : "Menu da sessão: aberto");
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
            if (!IsConsoleActive || IsSessionMenuOpen || !MenuShortcutSet) return;
            try
            {
                var combo = MenuShortcutText;
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
        _sessionMenu?.CloseAnimated();
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
        SessionElapsed = IsSessionMenuPreview ? "" : LocalizationService.Get("SessionElapsed", SessionMenuMath.FormatElapsed(DateTime.Now - started));
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
        var focus = SessionMonitorName;
        var (mode, hdr, audioName, volume) = await Task.Run(() =>
        {
            var current = NativeWindows.GetCurrentDisplayMode(focus);
            var modeText = current is null ? "" : $"{current.Width} × {current.Height} @ {current.Frequency} Hz";
            var hdrOn = Engine.Video.IsHdrOn(focus);
            var id = SessionAudioId;
            var name = Engine.Audio.GetDevices().FirstOrDefault(d => d.FriendlyId == id)?.Name ?? LocalizationService.Get("AudioNoChange");
            int? vol = null;
            try { if (id is not null) vol = Engine.Audio.GetVolumePercent(id); } catch { /* audio optional */ }
            return (modeText, hdrOn, name, vol);
        });
        SessionModeText = mode;
        SessionHdrOn = hdr;
        SessionAudioText = audioName;
        VolumePercent = volume ?? -1;
        SessionFpsText = state.FpsLimit > 0 ? $"{state.FpsLimit} FPS" : LocalizationService.Get("FpsNoLimit");
        SessionOverlayText = SelectedFpsOverlay?.Text ?? "";
        OnPropertyChanged(nameof(IsFpsMenuAvailable));
        OnPropertyChanged(nameof(IsFpsOverlayAvailable));
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
            try { Engine.Audio.SetVolumePercent(id, next); }
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
            try { Engine.Audio.SetMute(id, muted); }
            catch (Exception ex) { AppLog.Write($"Mudo: {ex.Message}"); }
        });
    }

    [RelayCommand]
    private void OpenSessionPicker(string key)
    {
        if (IsSessionMenuBusy) return;
        var state = Engine.State;
        var focus = SessionMonitorName;
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
            var screen = SessionMonitorName;
            switch (key)
            {
                case "mode":
                    var parts = item.Value.Split('x', '@');
                    Engine.Monitors.ApplyFocusMode(screen, new SavedDisplayMode
                    {
                        Width = int.Parse(parts[0]), Height = int.Parse(parts[1]), Frequency = int.Parse(parts[2])
                    });
                    Engine.Monitors.UpdateFocusRect(screen, state, true);
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
        var focus = SessionMonitorName;
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

    /// <summary>Whether ControlFS is installed; decides what the pinned row says. Read each time the menu opens.</summary>
    [ObservableProperty] private bool _controlFsInstalled;

    /// <summary>The install asked from the row is running (it goes on if the menu is closed meanwhile).</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ControlFsRowText))] private bool _controlFsInstalling;
    /// <summary>Download of the setup, 0 to 100; 100 while the setup itself runs.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ControlFsRowText))] private double _controlFsProgress;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ControlFsRowText))] private bool _controlFsSetupRunning;
    /// <summary>The install failed: the row says so and the next press opens the download page instead.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ControlFsRowText))] private bool _controlFsInstallFailed;
    /// <summary>Installed from the row while this menu was open: the row says it is ready to open.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ControlFsRowText))] private bool _controlFsJustInstalled;

    public string ControlFsRowText =>
        ControlFsSetupRunning ? Texts.ControlFsInstalling
        : ControlFsInstalling ? LocalizationService.Get("ControlFsDownloading", (int)ControlFsProgress)
        : ControlFsJustInstalled ? Texts.ControlFsInstalled
        : ControlFsInstalled ? Texts.ControlFsOpen
        : ControlFsInstallFailed ? Texts.ControlFsInstallFailed
        : Texts.ControlFsMissing;
    partial void OnControlFsInstalledChanged(bool value) => OnPropertyChanged(nameof(ControlFsRowText));

    /// <summary>
    /// The pinned "File explorer (ControlFS)" row: the menu closes and ControlFS (a separate app, controller-first)
    /// opens or comes to the front. Without it installed, the same press downloads and installs it (the row shows
    /// the progress) and the next press opens it; if that fails, the next press opens its download page. Outside a session it
    /// works the same, so it can be tried in the preview.
    /// </summary>
    [RelayCommand]
    private void OpenControlFs()
    {
        if (ControlFsInstalling) return;
        var installed = ControlFsService.FindExe() is not null;
        ControlFsInstalled = installed;
        if (!installed && !ControlFsInstallFailed)
        {
            _ = InstallControlFsAsync();
            return;
        }
        var screen = IsConsoleActive ? Engine.State.FocusMonitorRect : Engine.Monitors.GetMonitorRect(SessionMonitorName, Engine.State);
        CloseSessionMenu();
        if (installed && ControlFsService.Open())
        {
            _ = Task.Run(() => ControlFsService.EnsureFullScreen(screen));
            return;
        }
        AppLog.Write("ControlFS: não instalado, abrindo a página de download");
        ControlFsService.OpenDownloadPage();
    }

    private async Task InstallControlFsAsync()
    {
        ControlFsProgress = 0;
        ControlFsInstalling = true;
        AppLog.Write("ControlFS: não instalado, baixando e instalando a pedido do menu");
        try
        {
            var progress = new Progress<double>(p =>
            {
                if (!ControlFsInstalling) return;   // a late report, after it ended
                if (double.IsPositiveInfinity(p)) ControlFsSetupRunning = true;
                else ControlFsProgress = p * 100;
            });
            await Task.Run(() => ControlFsInstaller.InstallAsync(progress));
            AppLog.Write("ControlFS: instalado");
            ControlFsInstalled = true;
            ControlFsJustInstalled = true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"ControlFS: instalar: {ex.Message}");
            ControlFsInstallFailed = true;
        }
        finally
        {
            ControlFsSetupRunning = false;
            ControlFsInstalling = false;
        }
        // Not opened from here: by now the menu may have lost the foreground (or be gone), and ControlFS would come up
        // behind the game. The row says it is installed; the next A opens it like any other time.
    }

    [RelayCommand]
    private async Task RestoreFromMenuAsync()
    {
        var preview = IsSessionMenuPreview;
        CloseSessionMenu();
        // In the preview there is no session and no backup: closing is all there is to do.
        if (!preview) await RestoreNowAsync();
    }

    [RelayCommand]
    private async Task ExitFromMenuAsync()
    {
        var preview = IsSessionMenuPreview;
        CloseSessionMenu();
        if (!preview) await RestoreNowAsync();
        Application.Current.Exit();
    }
}
