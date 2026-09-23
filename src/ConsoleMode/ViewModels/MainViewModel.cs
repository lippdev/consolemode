using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ConsoleEngine Engine { get; } = new();

    public ObservableCollection<MonitorRowViewModel> Monitors { get; } = [];
    public ObservableCollection<ComboOption> HideStrategies { get; } = [];
    public ObservableCollection<ComboOption> LaunchOptions { get; } = [];
    public ObservableCollection<ComboOption> FpsOptions { get; } = [];
    public ObservableCollection<ComboOption> AudioOptions { get; } = [];
    public ObservableCollection<string> SummaryItems { get; } = [];

    private static readonly int[] FpsPresets = [30, 48, 50, 59, 60, 72, 75, 90, 120, 144];
    private const int FpsCustomValue = -1;
    private const double LayoutMaxWidth = 720;
    private const double LayoutMaxHeight = 220;
    private const double TileInset = 3;

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _loopCts;
    private volatile bool _busy;
    private bool _applying;

    /// <summary>Last config read from disk; monitor fields survive a failed monitor listing.</summary>
    private AppConfig _loadedConfig = new();

    [ObservableProperty] private bool _isHomePage = true;
    [ObservableProperty] private bool _isSettingsPage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasMonitors;
    [ObservableProperty] private bool _isConsoleActive;
    [ObservableProperty] private bool _isStarting;
    [ObservableProperty] private bool _isRestoring;
    [ObservableProperty] private bool _isStatusOpen;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
    [ObservableProperty] private string _summaryTitle = "";
    [ObservableProperty] private string _activeDescText = "";
    [ObservableProperty] private bool _isPlayniteAvailable = true;
    [ObservableProperty] private string _fpsStatusText = "";
    [ObservableProperty] private string _audioHintText = "Aplicada ao entrar e restaurada ao sair. \"Ao conectar\" troca sozinho quando a TV aparecer.";
    [ObservableProperty] private ComboOption? _selectedHideStrategy;
    [ObservableProperty] private ComboOption? _selectedLaunch;
    [ObservableProperty] private ComboOption? _selectedFps;
    [ObservableProperty] private ComboOption? _selectedAudio;
    [ObservableProperty] private string _customFpsText = "60";
    [ObservableProperty] private bool _showCustomFps;
    [ObservableProperty] private bool _isFpsAvailable;
    [ObservableProperty] private bool _hdrEnable;
    [ObservableProperty] private bool _vrrEnable;
    [ObservableProperty] private MonitorRowViewModel? _selectedMonitor;
    [ObservableProperty] private MonitorRowViewModel? _focusRow;

    public string AppVersion => UpdateService.CurrentVersion;

    public bool HasSelectedMonitor => SelectedMonitor is not null;
    public bool HasFocusRow => FocusRow is not null;
    public bool IsIdle => !IsConsoleActive;
    public bool CanStart => HasMonitors && FocusRow is not null && !IsLoading && !IsStarting && !IsConsoleActive;
    public bool ShowTiles => HasMonitors && !IsConsoleActive;
    public bool ShowEmpty => !HasMonitors && !IsLoading && !IsConsoleActive;
    public string StartButtonText => IsStarting ? "Entrando…" : "Jogar agora";
    public string LaunchDescription => SelectedLaunch?.Value switch
    {
        "playnite" => "Abre o Playnite em tela cheia. Ao fechar, suas telas voltam sozinhas.",
        "xboxMode" => "Experimental: envia Win+F11. A volta é manual, pelo app ou pela bandeja.",
        _ => "Abre o Steam em Big Picture. Ao fechar, suas telas voltam sozinhas."
    };
    public string FocusModeDescription => FocusRow is null
        ? "Escolha uma tela para jogar na tela inicial."
        : $"Aplicada em {FocusRow.Name} ao entrar e desfeita ao sair.";

    partial void OnHasMonitorsChanged(bool value) => NotifyStartState();
    partial void OnIsLoadingChanged(bool value) => NotifyStartState();
    partial void OnIsStartingChanged(bool value) => NotifyStartState();
    partial void OnIsConsoleActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        NotifyStartState();
    }
    partial void OnFocusRowChanged(MonitorRowViewModel? value)
    {
        OnPropertyChanged(nameof(HasFocusRow));
        OnPropertyChanged(nameof(FocusModeDescription));
        NotifyStartState();
    }

    private void NotifyStartState()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(ShowTiles));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(StartButtonText));
    }

    /// <param name="interactive">False for --start: no tour while the window stays hidden.</param>
    public async Task InitializeAsync(bool interactive = true)
    {
        Engine.UiInvoker = RunOnUi;

        HideStrategies.Clear();
        HideStrategies.Add(new ComboOption { Text = "Desconectar no Windows", Value = "disconnect" });
        HideStrategies.Add(new ComboOption { Text = "Cobrir com tela preta", Value = "blackCurtain" });
        HideStrategies.Add(new ComboOption { Text = "Apagar o painel (DDC/CI)", Value = "turnOff" });

        IsPlayniteAvailable = Engine.Launch.IsPlayniteAvailable();
        LaunchOptions.Clear();
        LaunchOptions.Add(new ComboOption { Text = "Steam Big Picture", Value = "bigPicture" });
        if (IsPlayniteAvailable)
            LaunchOptions.Add(new ComboOption { Text = "Playnite (tela cheia)", Value = "playnite" });
        LaunchOptions.Add(new ComboOption { Text = "Modo Xbox (experimental)", Value = "xboxMode" });

        FpsOptions.Clear();
        FpsOptions.Add(new ComboOption { Text = "Sem limite", Value = "0" });
        foreach (var fps in FpsPresets)
            FpsOptions.Add(new ComboOption { Text = $"{fps} FPS", Value = fps.ToString() });
        FpsOptions.Add(new ComboOption { Text = "Personalizado", Value = FpsCustomValue.ToString() });

        IsFpsAvailable = Engine.Rtss.IsReady;
        FpsStatusText = IsFpsAvailable
            ? "Vale para todos os jogos enquanto o modo console estiver ativo. Usa o RTSS."
            : "Opcional. Instale o RivaTuner Statistics Server (RTSS) para usar.";

        var firstRun = !ConfigService.Exists;
        await ReloadAsync();
        // Save the detected defaults so the tray/shortcut work right away.
        if (firstRun && HasMonitors) TrySave(BuildConfig());
        InitializeAppSettings();
        if (interactive && HasMonitors && !_loadedConfig.TourDone) StartTour();
        _ = CheckForUpdatesOnStartupAsync();
    }

    /// <summary>--start / tray: enter console mode with the saved config.</summary>
    public async Task<bool> TryAutoStartAsync()
    {
        if (!HasMonitors || FocusRow is null)
        {
            SetStatus("Nenhuma tela de jogo configurada; não foi possível entrar no modo console.", InfoBarSeverity.Warning);
            return false;
        }
        await StartAsync();
        return IsConsoleActive;
    }

    private sealed record LoadedMonitor(MonitorInfo Info, List<DisplayModeOption> Modes);

    private sealed record LoadResult(AppConfig Config, List<LoadedMonitor> Monitors, List<AudioDevice> Audio, string? MonitorError);

    private static LoadResult LoadData(ConsoleEngine engine)
    {
        var config = ConfigService.Load();
        string? error = null;
        var monitors = new List<LoadedMonitor>();
        try
        {
            var list = engine.Monitors.GetMonitors(true);
            if (ConfigService.Exists && ConfigService.Migrate(config, list)) ConfigService.Save(config);
            foreach (var monitor in list)
                monitors.Add(new LoadedMonitor(monitor, engine.Monitors.GetDisplayModes(monitor.Name, monitor)));
            if (list.Count == 0)
            {
                error = AppPaths.HasMmt
                    ? "Nenhuma tela encontrada. Tente atualizar."
                    : $"MultiMonitorTool.exe não encontrado em {AppPaths.MmtPath}";
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Monitores: {ex}");
            error = $"Não foi possível listar as telas: {ex.Message}";
        }

        List<AudioDevice> audio = [];
        try
        {
            if (AppPaths.HasSvv) audio = [.. engine.Audio.GetDevices(true)];
        }
        catch (Exception ex)
        {
            AppLog.Write($"Áudio: {ex}");
        }

        AppLog.Write($"Carregado: {monitors.Count} telas ({string.Join(", ", monitors.Select(m => $"{m.Info.Name}={m.Info.FriendlyName}{(m.Info.IsActive ? "" : " [off]")}"))}), {audio.Count} saídas de áudio");
        return new LoadResult(config, monitors, audio, error);
    }

    private async Task ReloadAsync()
    {
        IsLoading = true;
        try
        {
            var data = await Task.Run(() => LoadData(Engine));
            Apply(data);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(LoadResult data)
    {
        _applying = true;
        try
        {
            var config = data.Config;
            _loadedConfig = config;
            ApplyMonitors(config, data.Monitors);
            ApplyAudio(config, data.Audio);

            SelectedHideStrategy = HideStrategies.FirstOrDefault(s => s.Value == config.HideStrategy) ?? HideStrategies[0];
            SelectedLaunch = LaunchOptions.FirstOrDefault(o => o.Value == config.FullscreenMode) ?? LaunchOptions[0];
            HdrEnable = config.HdrEnable;
            VrrEnable = config.VrrEnable;
            CheckUpdates = config.CheckUpdates;

            if (config.FpsLimit > 0 && FpsPresets.Contains(config.FpsLimit))
            {
                SelectedFps = FpsOptions.FirstOrDefault(o => o.Value == config.FpsLimit.ToString());
            }
            else if (config.FpsLimit > 0)
            {
                SelectedFps = FpsOptions.FirstOrDefault(o => o.Value == FpsCustomValue.ToString());
                CustomFpsText = config.FpsLimit.ToString();
            }
            else
            {
                SelectedFps = FpsOptions[0];
            }
        }
        finally
        {
            _applying = false;
        }

        if (data.MonitorError is not null) SetStatus(data.MonitorError, InfoBarSeverity.Warning);
        UpdateSummary();
    }

    private void ApplyMonitors(AppConfig config, List<LoadedMonitor> loaded)
    {
        foreach (var row in Monitors) row.PropertyChanged -= OnRowPropertyChanged;
        SelectedMonitor = null;
        Monitors.Clear();
        HasMonitors = loaded.Count > 0;
        if (loaded.Count == 0)
        {
            FocusRow = null;
            return;
        }

        var ordered = loaded.OrderBy(m => ParseLeft(m.Info.LeftTop)).ThenBy(m => m.Info.WindowsDisplayNumber).ToList();

        var focus = ordered.FirstOrDefault(m => m.Info.Matches(config.FocusMonitor))?.Info;
        var firstRun = string.IsNullOrWhiteSpace(config.FocusMonitor);
        if (focus is null)
        {
            // Typical setup: the game screen is the one that stays off while working.
            focus = ordered.FirstOrDefault(m => !m.Info.IsActive)?.Info
                    ?? ordered.FirstOrDefault(m => m.Info.IsPrimary)?.Info
                    ?? ordered[0].Info;
        }

        foreach (var (monitor, monitorModes) in ordered)
        {
            var modes = new List<DisplayModeOption>
            {
                new() { Text = "Não alterar", Key = "current", UseCurrent = true }
            };
            modes.AddRange(monitorModes);

            var selected = modes[0];
            if (config.MonitorModes.TryGetValue(monitor.StableId, out var saved) ||
                config.MonitorModes.TryGetValue(monitor.Name, out saved))
            {
                selected = modes.FirstOrDefault(m => m.Key == saved.Key) ?? modes[0];
            }

            MonitorRole role;
            if (ReferenceEquals(monitor, focus)) role = MonitorRole.Focus;
            else if (firstRun || config.HideMonitors.Any(monitor.Matches)) role = MonitorRole.Hide;
            else role = MonitorRole.Keep;

            var row = new MonitorRowViewModel(monitor, role, selected, modes, OnRoleChanged, OnMonitorSelected);
            row.PropertyChanged += OnRowPropertyChanged;
            Monitors.Add(row);
        }

        LayoutTiles();
        FocusRow = Monitors.FirstOrDefault(m => m.IsFocus);
        SelectedMonitor = FocusRow ?? Monitors[0];
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitorRowViewModel.SelectedMode)) SaveQuietly();
    }

    private readonly record struct DesktopRect(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
        public bool Overlaps(DesktopRect o) => X < o.Right && o.X < Right && Y < o.Bottom && o.Y < Bottom;
    }

    /// <summary>
    /// Maps each screen to its desktop position (like Windows display settings), scaled to fit.
    /// Screens that are off keep a stale position that often overlaps the live ones, so those
    /// are parked to the right of the arrangement instead.
    /// </summary>
    private void LayoutTiles()
    {
        if (Monitors.Count == 0) return;

        var rects = new Dictionary<MonitorRowViewModel, DesktopRect>();
        var parked = new List<MonitorRowViewModel>();
        foreach (var row in Monitors.OrderBy(r => r.IsOff))
        {
            var (w, h) = DesktopSize(row.Monitor);
            if (TryParsePoint(row.Monitor.LeftTop, out var x, out var y))
            {
                var rect = new DesktopRect(x, y, w, h);
                if (!row.IsOff || !rects.Values.Any(rect.Overlaps))
                {
                    rects[row] = rect;
                    continue;
                }
            }
            parked.Add(row);
        }

        foreach (var row in parked)
        {
            var (w, h) = DesktopSize(row.Monitor);
            var right = rects.Count > 0 ? rects.Values.Max(r => r.Right) : 0;
            var top = rects.Count > 0 ? rects.Values.Min(r => r.Y) : 0;
            rects[row] = new DesktopRect(right + 240, top, w, h);
        }

        var left = rects.Values.Min(r => r.X);
        var minY = rects.Values.Min(r => r.Y);
        var width = rects.Values.Max(r => r.Right) - left;
        var height = rects.Values.Max(r => r.Bottom) - minY;
        var scale = Math.Min(LayoutMaxWidth / width, LayoutMaxHeight / height);

        foreach (var (row, rect) in rects)
        {
            // Small inset so neighbouring screens read as separate tiles.
            row.LayoutX = (rect.X - left) * scale + TileInset;
            row.LayoutY = (rect.Y - minY) * scale + TileInset;
            row.TileWidth = Math.Max(24, rect.W * scale - TileInset * 2);
            row.TileHeight = Math.Max(24, rect.H * scale - TileInset * 2);
        }
    }

    private static (double W, double H) DesktopSize(MonitorInfo m)
    {
        var w = m.Width > 0 ? m.Width : m.MaxWidth;
        var h = m.Height > 0 ? m.Height : m.MaxHeight;
        return (w > 0 ? w : 1920, h > 0 ? h : 1080);
    }

    private static bool TryParsePoint(string? text, out double x, out double y)
    {
        x = y = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var m = Regex.Match(text, @"(-?\d+)\s*,\s*(-?\d+)");
        if (!m.Success) return false;
        x = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        y = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        return true;
    }

    private void OnMonitorSelected(MonitorRowViewModel row) => SelectedMonitor = row;

    partial void OnSelectedMonitorChanged(MonitorRowViewModel? oldValue, MonitorRowViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
        OnPropertyChanged(nameof(HasSelectedMonitor));
    }

    private void ApplyAudio(AppConfig config, List<AudioDevice> devices)
    {
        AudioOptions.Clear();
        AudioOptions.Add(new ComboOption { Text = "Não alterar", Value = "" });
        AudioOptions.Add(new ComboOption { Text = "A que aparecer ao conectar (TV)", Value = ConsoleEngine.AudioOnConnectId });

        if (AppPaths.HasSvv)
        {
            foreach (var device in devices)
                AudioOptions.Add(new ComboOption { Text = device.Name, Value = device.FriendlyId });
        }
        else
        {
            AudioHintText = "SoundVolumeView.exe não encontrado em ConsoleMode_Data/tools; a troca de áudio fica desativada.";
        }

        if (config.AudioAutoSwitch)
            SelectedAudio = AudioOptions.FirstOrDefault(a => a.Value == ConsoleEngine.AudioOnConnectId);
        else if (!string.IsNullOrWhiteSpace(config.AudioDeviceId))
            SelectedAudio = AudioOptions.FirstOrDefault(a => a.Value == config.AudioDeviceId) ?? AudioOptions[0];
        else
            SelectedAudio = AudioOptions[0];
    }

    private void OnRoleChanged(MonitorRowViewModel source)
    {
        if (_applying) return;
        if (source.IsFocus)
        {
            // One game screen; the previous one goes back to being hidden.
            foreach (var row in Monitors)
            {
                if (!ReferenceEquals(row, source) && row.IsFocus)
                    row.Role = MonitorRole.Hide;
            }
        }

        FocusRow = Monitors.FirstOrDefault(m => m.IsFocus);
        UpdateSummary();
        SaveQuietly();
    }

    // Every setting saves as soon as it changes; there is no Save button.
    partial void OnSelectedFpsChanged(ComboOption? value)
    {
        ShowCustomFps = value?.Value == FpsCustomValue.ToString();
        SettingChanged();
    }

    partial void OnCustomFpsTextChanged(string value) => SettingChanged();
    partial void OnSelectedAudioChanged(ComboOption? value) => SettingChanged();
    partial void OnSelectedHideStrategyChanged(ComboOption? value) => SettingChanged();
    partial void OnHdrEnableChanged(bool value) => SettingChanged();
    partial void OnVrrEnableChanged(bool value) => SettingChanged();
    partial void OnSelectedLaunchChanged(ComboOption? value)
    {
        OnPropertyChanged(nameof(LaunchDescription));
        SettingChanged();
    }

    private void SettingChanged()
    {
        UpdateSummary();
        SaveQuietly();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (IsConsoleActive) return;
        ShowPage(settings: true);
    }

    [RelayCommand]
    private void GoHome() => ShowPage(settings: false);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsConsoleActive || IsLoading) return;
        if (HasMonitors) TrySave(BuildConfig());
        Engine.Monitors.ClearCache();
        Engine.Audio.ClearCache();
        IsStatusOpen = false;
        await ReloadAsync();
    }

    [RelayCommand]
    private void CreateShortcut()
    {
        try
        {
            TrySave(BuildConfig());
            ShortcutService.CreateDesktopShortcut();
            SetStatus("Atalho \"Modo Console\" criado na Área de Trabalho.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Atalho: {ex}");
            SetStatus($"Não foi possível criar o atalho: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = AppPaths.DataDir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus($"Não foi possível abrir a pasta: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (_busy || IsConsoleActive) return;
        var config = BuildConfig();
        if (string.IsNullOrWhiteSpace(config.FocusMonitor))
        {
            SetStatus("Escolha a tela onde você quer jogar.", InfoBarSeverity.Warning);
            return;
        }

        TrySave(config);
        _busy = true;
        IsStarting = true;
        IsStatusOpen = false;
        TourStep = 0;

        // Safety net: a game screen/mode never seen working must be confirmed on the TV,
        // otherwise the desk screens come back by themselves.
        var setup = config.SetupKey;
        Func<ScreenRect?, bool>? confirm = config.ConfirmedSetup == setup ? null : ConfirmOnGameScreen;
        var rolledBack = false;
        try
        {
            var focus = FocusRow?.Monitor;
            await Task.Run(() => Engine.Start(config, focus, confirm));
            IsConsoleActive = true;
            ShowPage(settings: false);
            ActiveDescText = DescribeActive(config);
            StartLoop();
            if (confirm is not null)
            {
                config.ConfirmedSetup = setup;
                TrySave(config);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Start: {ex}");
            SetStatus(ex is OperationCanceledException
                    ? "Suas telas voltaram ao normal porque a tela de jogo não foi confirmada. Confira a tela escolhida e tente de novo."
                    : $"Falha ao entrar no modo console: {ex.Message}",
                ex is OperationCanceledException ? InfoBarSeverity.Warning : InfoBarSeverity.Error);
            if (Engine.State.IsActive)
            {
                // Half-applied setup: put the desktop back instead of leaving it broken.
                try { await Task.Run(() => Engine.Stop()); }
                catch (Exception stopEx) { AppLog.Write($"Start rollback: {stopEx}"); }
                rolledBack = true;
            }
        }
        finally
        {
            IsStarting = false;
            _busy = false;
        }

        if (rolledBack)
        {
            Engine.Monitors.ClearCache();
            await ReloadAsync();
        }
    }

    private const int ConfirmSeconds = 15;

    /// <summary>Runs on the Start worker thread; shows the prompt on the UI thread and waits.</summary>
    private bool ConfirmOnGameScreen(ScreenRect? gameScreen)
    {
        ConfirmWindow? window = null;
        RunOnUi(() =>
        {
            window = new ConfirmWindow(gameScreen, ConfirmSeconds);
            window.Activate();
        });
        if (window is null) return true; // could not show the prompt: don't block the user
        var answer = window.Result;
        var confirmed = answer.Wait(TimeSpan.FromSeconds(ConfirmSeconds + 10)) && answer.Result;
        AppLog.Write($"Confirmação na tela de jogo: {(confirmed ? "sim" : "não")} ({window.AnsweredBy})");
        return confirmed;
    }

    /// <summary>Settings: show the prompt over this window without touching any screen.</summary>
    [RelayCommand]
    private async Task TestConfirmAsync()
    {
        ScreenRect? here = null;
        if (App.MainWindowInstance is { } main)
        {
            var appWindow = main.AppWindow;
            here = new ScreenRect
            {
                X = appWindow.Position.X,
                Y = appWindow.Position.Y,
                Width = appWindow.Size.Width,
                Height = appWindow.Size.Height
            };
        }

        var window = new ConfirmWindow(here, ConfirmSeconds);
        window.Activate();
        var confirmed = await window.Result;
        var family = ControllerInput.DetectFamily() switch
        {
            ControllerFamily.Xbox => "controle Xbox detectado",
            ControllerFamily.PlayStation => "controle PlayStation detectado",
            ControllerFamily.Other => "controle detectado",
            _ => "nenhum controle detectado"
        };
        AppLog.Write($"Teste de confirmação: {(confirmed ? "sim" : "não")} ({window.AnsweredBy}; {family})");
        SetStatus(
            $"Teste: {(confirmed ? "confirmado" : "voltaria ao normal")} por {window.AnsweredBy} ({family}).",
            confirmed ? InfoBarSeverity.Success : InfoBarSeverity.Informational);
    }

    // ───────────── First-run tour ─────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTourMap), nameof(IsTourRole), nameof(IsTourPlay))]
    private int _tourStep;

    public bool IsTourMap => TourStep == 1;
    public bool IsTourRole => TourStep == 2;
    public bool IsTourPlay => TourStep == 3;

    public string TourMapText => FocusRow is null
        ? "Aqui estão as suas telas, na posição em que estão na mesa. Clique na tela onde você quer jogar."
        : $"Aqui estão as suas telas, na posição em que estão na mesa. Escolhemos {FocusRow.Name} para jogar porque ela está desligada agora; clique em outra se preferir.";

    [RelayCommand]
    private void StartTour()
    {
        if (!HasMonitors || IsConsoleActive) return;
        ShowPage(settings: false);
        IsStatusOpen = false;
        SelectedMonitor = FocusRow ?? SelectedMonitor;
        OnPropertyChanged(nameof(TourMapText));
        TourStep = 1;
    }

    [RelayCommand]
    private void TourNext()
    {
        if (TourStep >= 3) EndTour();
        else TourStep++;
    }

    /// <summary>"Pular tutorial" or the X; step changes close tips programmatically and are ignored.</summary>
    public void OnTourTipClosed(TeachingTip sender, TeachingTipClosedEventArgs args)
    {
        if (args.Reason != TeachingTipCloseReason.Programmatic) EndTour();
    }

    [RelayCommand]
    private void EndTour()
    {
        TourStep = 0;
        if (_loadedConfig.TourDone) return;
        var config = BuildConfig();
        config.TourDone = true;
        TrySave(config);
    }

    [RelayCommand]
    public async Task RestoreNowAsync()
    {
        if (_busy && !Engine.State.IsActive) return;
        StopLoop();
        _busy = true;
        IsRestoring = true;
        try
        {
            await Task.Run(() => Engine.Stop());
            IsConsoleActive = false;
            SetStatus("Suas telas voltaram ao normal.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Restore: {ex}");
            SetStatus($"Falha ao restaurar: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsRestoring = false;
            _busy = false;
        }

        // Screens were renumbered/re-enabled; refresh names and resolutions.
        Engine.Monitors.ClearCache();
        await ReloadAsync();
    }

    public bool TryCloseToTray() => IsConsoleActive;

    private void StartLoop()
    {
        StopLoop();
        var cts = new CancellationTokenSource();
        _loopCts = cts;
        // Tick shells out to MMT/SVV; keep it off the UI thread.
        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try { await Task.Delay(Engine.PollDelayMs(), cts.Token); }
                catch (OperationCanceledException) { return; }
                if (_busy) continue;

                string result;
                try { result = Engine.Tick(); }
                catch (Exception ex)
                {
                    AppLog.Write($"Loop: {ex.Message}");
                    continue;
                }

                if (result == "exit")
                {
                    _dispatcher.TryEnqueue(() => _ = RestoreNowAsync());
                    return;
                }
            }
        });
    }

    private void StopLoop()
    {
        _loopCts?.Cancel();
        _loopCts = null;
    }

    private void RunOnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess)
        {
            action();
            return;
        }

        using var done = new ManualResetEventSlim();
        Exception? error = null;
        var queued = _dispatcher.TryEnqueue(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
            finally { done.Set(); }
        });
        if (!queued)
        {
            action();
            return;
        }

        if (!done.Wait(TimeSpan.FromSeconds(10)))
            AppLog.Write("UI: ação na thread de UI não terminou em 10s");
        if (error is not null)
            AppLog.Write($"UI: {error}");
    }

    private AppConfig BuildConfig()
    {
        var audioId = SelectedAudio?.Value ?? "";
        var auto = audioId == ConsoleEngine.AudioOnConnectId;
        var config = new AppConfig
        {
            // Listing failed: keep the saved screens instead of wiping them.
            FocusMonitor = _loadedConfig.FocusMonitor,
            HideMonitors = [.. _loadedConfig.HideMonitors],
            MonitorModes = new Dictionary<string, SavedDisplayMode>(_loadedConfig.MonitorModes, StringComparer.OrdinalIgnoreCase),
            HideStrategy = SelectedHideStrategy?.Value ?? "disconnect",
            FullscreenMode = SelectedLaunch?.Value ?? "bigPicture",
            AudioDeviceId = auto ? "" : audioId,
            AudioDeviceName = auto ? "" : SelectedAudio?.Text ?? "",
            AudioAutoSwitch = auto,
            FpsLimit = ReadFpsLimit(),
            HdrEnable = HdrEnable,
            VrrEnable = VrrEnable,
            TourDone = _loadedConfig.TourDone,
            ConfirmedSetup = _loadedConfig.ConfirmedSetup,
            CheckUpdates = CheckUpdates,
            SkippedUpdateVersion = _loadedConfig.SkippedUpdateVersion
        };

        if (Monitors.Count == 0) return config;

        config.FocusMonitor = FocusRow?.Monitor.StableId ?? "";
        config.HideMonitors = [.. Monitors.Where(m => m.IsHide).Select(m => m.Monitor.StableId)];
        config.MonitorModes = new Dictionary<string, SavedDisplayMode>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Monitors)
        {
            if (row.SelectedMode is null || row.SelectedMode.UseCurrent) continue;
            config.MonitorModes[row.Monitor.StableId] = new SavedDisplayMode
            {
                Width = row.SelectedMode.Width,
                Height = row.SelectedMode.Height,
                Frequency = row.SelectedMode.Frequency
            };
        }
        return config;
    }

    private void SaveQuietly()
    {
        if (_applying || IsLoading) return;
        TrySave(BuildConfig());
    }

    private void TrySave(AppConfig config)
    {
        try
        {
            ConfigService.Save(config);
            _loadedConfig = config;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Config save: {ex}");
            SetStatus($"Não foi possível salvar a configuração: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private int ReadFpsLimit()
    {
        if (SelectedFps is null) return 0;
        if (SelectedFps.Value == FpsCustomValue.ToString())
            return int.TryParse(CustomFpsText, out var custom) && custom > 0 ? custom : 0;
        return int.TryParse(SelectedFps.Value, out var fps) ? fps : 0;
    }

    private void UpdateSummary()
    {
        if (_applying) return;
        var hidden = Monitors.Where(m => m.IsHide).Select(m => m.Name).ToList();
        SummaryTitle = FocusRow is null
            ? "Escolha a tela onde você quer jogar"
            : hidden.Count > 0
                ? $"Jogar em {FocusRow.Name} e desligar {string.Join(" e ", hidden)}"
                : $"Jogar em {FocusRow.Name}";

        SummaryItems.Clear();
        SummaryItems.Add(SelectedLaunch?.Text ?? "Steam Big Picture");
        switch (SelectedAudio?.Value)
        {
            case null or "":
                break;
            case ConsoleEngine.AudioOnConnectId:
                SummaryItems.Add("Áudio ao conectar");
                break;
            default:
                SummaryItems.Add($"Áudio: {SelectedAudio.Text}");
                break;
        }
        var fps = ReadFpsLimit();
        if (IsFpsAvailable && fps > 0) SummaryItems.Add($"{fps} FPS");
        if (HdrEnable) SummaryItems.Add("HDR");
        if (VrrEnable) SummaryItems.Add("VRR");
    }

    private static string DescribeActive(AppConfig config) => config.FullscreenMode switch
    {
        "playnite" => "Feche o Playnite e suas telas voltam sozinhas.",
        "xboxMode" => "No Modo Xbox a volta é manual: use Restaurar agora ou o menu da bandeja.",
        _ => "Feche o Big Picture e suas telas voltam sozinhas."
    };

    private void ShowPage(bool settings)
    {
        IsSettingsPage = settings;
        IsHomePage = !settings;
    }

    private void SetStatus(string text, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        StatusText = text;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }

    private static int ParseLeft(string? leftTop)
    {
        if (string.IsNullOrWhiteSpace(leftTop)) return int.MaxValue;
        var m = Regex.Match(leftTop, @"(-?\d+)");
        return m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ? x : int.MaxValue;
    }
}
