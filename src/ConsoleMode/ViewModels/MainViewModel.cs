using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ConsoleEngine Engine { get; } = new();

    public ObservableCollection<MonitorRowViewModel> Monitors { get; } = [];
    public ObservableCollection<ComboOption> HideStrategies { get; } = [];
    public ObservableCollection<ComboOption> FpsOptions { get; } = [];
    public ObservableCollection<ComboOption> AudioOptions { get; } = [];

    private static readonly int[] FpsPresets = [30, 48, 50, 59, 60, 72, 75, 90, 120, 144];
    private const int FpsCustomValue = -1;
    private const double TilesMaxWidth = 760;
    private const double TilesMaxHeight = 170;

    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private CancellationTokenSource? _loopCts;
    private volatile bool _busy;
    private bool _applying;

    /// <summary>Last config read from disk; monitor fields survive a failed monitor listing.</summary>
    private AppConfig _loadedConfig = new();

    [ObservableProperty] private bool _isHomePage = true;
    [ObservableProperty] private bool _isSettingsPage;
    [ObservableProperty] private bool _isFirstRun;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasMonitors;
    [ObservableProperty] private bool _isConsoleActive;
    [ObservableProperty] private bool _isStarting;
    [ObservableProperty] private string _statusText = "Pronto.";
    [ObservableProperty] private bool _statusIsWarning;
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private string _activeDescText = "";
    [ObservableProperty] private string _playniteDesc = "Abre o Playnite em tela cheia na tela de jogo. Restauração automática ao sair.";
    [ObservableProperty] private bool _isPlayniteAvailable = true;
    [ObservableProperty] private string _fpsStatusText = "";
    [ObservableProperty] private string _audioHintText = "A saída escolhida é aplicada ao entrar e restaurada ao sair. 'Usar áudio ao conectar' troca sozinho quando a TV conectar.";
    [ObservableProperty] private ComboOption? _selectedHideStrategy;
    [ObservableProperty] private ComboOption? _selectedFps;
    [ObservableProperty] private ComboOption? _selectedAudio;
    [ObservableProperty] private string _customFpsText = "60";
    [ObservableProperty] private bool _showCustomFps;
    [ObservableProperty] private bool _isFpsAvailable;
    [ObservableProperty] private bool _modeBigPicture = true;
    [ObservableProperty] private bool _modePlaynite;
    [ObservableProperty] private bool _modeXbox;
    [ObservableProperty] private bool _hdrEnable;
    [ObservableProperty] private bool _vrrEnable;

    public bool IsIdle => !IsConsoleActive;
    public bool CanStart => HasMonitors && !IsLoading && !IsStarting && !IsConsoleActive;
    public bool ShowTiles => HasMonitors && !IsConsoleActive;
    public bool ShowEmpty => !HasMonitors && !IsLoading && !IsConsoleActive;
    public string StatusGlyph => StatusIsWarning ? "" : "";
    public string StartButtonText => IsStarting ? "Entrando no modo console…" : "Entrar no modo console";

    partial void OnHasMonitorsChanged(bool value) => NotifyStartState();
    partial void OnIsLoadingChanged(bool value) => NotifyStartState();
    partial void OnIsStartingChanged(bool value) => NotifyStartState();
    partial void OnIsConsoleActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        NotifyStartState();
    }
    partial void OnStatusIsWarningChanged(bool value) => OnPropertyChanged(nameof(StatusGlyph));

    private void NotifyStartState()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(ShowTiles));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(StartButtonText));
    }

    public async Task InitializeAsync()
    {
        Engine.UiInvoker = RunOnUi;

        HideStrategies.Clear();
        HideStrategies.Add(new ComboOption { Text = "Desconectar no Windows (recomendado)", Value = "disconnect" });
        HideStrategies.Add(new ComboOption { Text = "Cortinas pretas (overlay)", Value = "blackCurtain" });
        HideStrategies.Add(new ComboOption { Text = "Apagar painel via DDC/CI", Value = "turnOff" });

        FpsOptions.Clear();
        FpsOptions.Add(new ComboOption { Text = "Não limitar", Value = "0" });
        foreach (var fps in FpsPresets)
            FpsOptions.Add(new ComboOption { Text = $"{fps} FPS", Value = fps.ToString() });
        FpsOptions.Add(new ComboOption { Text = "Personalizado…", Value = FpsCustomValue.ToString() });

        IsPlayniteAvailable = Engine.Launch.IsPlayniteAvailable();
        if (!IsPlayniteAvailable)
            PlayniteDesc = "Playnite não encontrado. Instale o Playnite para usar esta opção.";

        IsFpsAvailable = Engine.Rtss.IsReady;
        FpsStatusText = IsFpsAvailable
            ? "RTSS encontrado. O limite vale para todos os jogos enquanto o modo console estiver ativo e é desfeito ao sair."
            : "Opcional. Instale o RivaTuner Statistics Server (RTSS) para limitar o FPS. Sem ele o modo console funciona normalmente, só sem limite.";

        IsFirstRun = !ConfigService.Exists;
        if (IsFirstRun) ShowPage(settings: true);

        await ReloadAsync();
        if (IsFirstRun && HasMonitors)
            SetStatus("Bem-vindo! Escolha a tela onde você joga e clique em Concluir.");
    }

    /// <summary>--start: enter console mode with the saved config, window stays hidden.</summary>
    public async Task<bool> TryAutoStartAsync()
    {
        if (IsFirstRun || !HasMonitors)
        {
            SetStatus(IsFirstRun
                ? "Configure o Console Mode uma vez antes de usar o atalho."
                : "Nenhuma tela encontrada; o atalho não pôde entrar no modo console.", warning: true);
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
                    ? "Nenhuma tela encontrada. Clique em Atualizar."
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
            HdrEnable = config.HdrEnable;
            VrrEnable = config.VrrEnable;
            ModeBigPicture = config.FullscreenMode is "bigPicture" or "" || (config.FullscreenMode == "playnite" && !IsPlayniteAvailable);
            ModePlaynite = config.FullscreenMode == "playnite" && IsPlayniteAvailable;
            ModeXbox = config.FullscreenMode == "xboxMode";

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

        if (data.MonitorError is not null) SetStatus(data.MonitorError, warning: true);
        UpdateSummary();
    }

    private void ApplyMonitors(AppConfig config, List<LoadedMonitor> loaded)
    {
        Monitors.Clear();
        HasMonitors = loaded.Count > 0;
        if (loaded.Count == 0) return;

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
                new() { Text = "Atual (não alterar)", Key = "current", UseCurrent = true }
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

            Monitors.Add(new MonitorRowViewModel(monitor, role, selected, modes, OnRoleChanged));
        }

        LayoutTiles();
    }

    /// <summary>Sizes the home-screen tiles in proportion to each screen's resolution.</summary>
    private void LayoutTiles()
    {
        if (Monitors.Count == 0) return;
        const double gap = 12;
        var sizes = Monitors.Select(r =>
        {
            var w = r.Monitor.Width > 0 ? r.Monitor.Width : r.Monitor.MaxWidth;
            var h = r.Monitor.Height > 0 ? r.Monitor.Height : r.Monitor.MaxHeight;
            return (W: w > 0 ? w : 1920.0, H: h > 0 ? h : 1080.0);
        }).ToList();

        var scale = Math.Min(
            (TilesMaxWidth - gap * (Monitors.Count - 1)) / sizes.Sum(s => s.W),
            TilesMaxHeight / sizes.Max(s => s.H));

        for (var i = 0; i < Monitors.Count; i++)
        {
            Monitors[i].TileWidth = Math.Max(150, sizes[i].W * scale);
            Monitors[i].TileHeight = Math.Max(92, sizes[i].H * scale);
        }
    }

    private void ApplyAudio(AppConfig config, List<AudioDevice> devices)
    {
        AudioOptions.Clear();
        AudioOptions.Add(new ComboOption { Text = "Não alterar", Value = "" });
        AudioOptions.Add(new ComboOption { Text = "Usar áudio ao conectar (TV/monitor)", Value = ConsoleEngine.AudioOnConnectId });

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

        UpdateSummary();
        if (IsHomePage) SaveQuietly();
    }

    partial void OnSelectedFpsChanged(ComboOption? value)
    {
        ShowCustomFps = value?.Value == FpsCustomValue.ToString();
        UpdateSummary();
    }

    partial void OnSelectedAudioChanged(ComboOption? value) => UpdateSummary();
    partial void OnModeBigPictureChanged(bool value) => UpdateSummary();
    partial void OnModePlayniteChanged(bool value) => UpdateSummary();
    partial void OnModeXboxChanged(bool value) => UpdateSummary();

    [RelayCommand]
    private void OpenSettings()
    {
        if (IsConsoleActive) return;
        ShowPage(settings: true);
    }

    [RelayCommand]
    private void CloseSettings()
    {
        var config = BuildConfig();
        if (string.IsNullOrWhiteSpace(config.FocusMonitor) && HasMonitors)
        {
            SetStatus("Escolha a tela onde você quer jogar.", warning: true);
            return;
        }

        TrySave(config);
        IsFirstRun = false;
        ShowPage(settings: false);
        SetStatus("Tudo pronto. Clique em Entrar no modo console quando quiser jogar.");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsConsoleActive || IsLoading) return;
        if (HasMonitors) TrySave(BuildConfig());
        Engine.Monitors.ClearCache();
        Engine.Audio.ClearCache();
        SetStatus("Atualizando telas e áudio…");
        await ReloadAsync();
        if (!StatusIsWarning) SetStatus($"{Monitors.Count} telas encontradas.");
    }

    [RelayCommand]
    private void CreateShortcut()
    {
        try
        {
            TrySave(BuildConfig());
            var path = ShortcutService.CreateDesktopShortcut();
            SetStatus($"Atalho criado: {Path.GetFileName(path)} na Área de Trabalho. Ele entra no modo console direto.");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Atalho: {ex}");
            SetStatus($"Não foi possível criar o atalho: {ex.Message}", warning: true);
        }
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (_busy || IsConsoleActive) return;
        var config = BuildConfig();
        if (string.IsNullOrWhiteSpace(config.FocusMonitor))
        {
            SetStatus("Escolha a tela onde você quer jogar.", warning: true);
            return;
        }

        TrySave(config);
        _busy = true;
        IsStarting = true;
        SetStatus("Entrando no modo console…");
        try
        {
            var focus = Monitors.FirstOrDefault(m => m.IsFocus)?.Monitor;
            await Task.Run(() => Engine.Start(config, focus));
            IsConsoleActive = true;
            ShowPage(settings: false);
            ActiveDescText = DescribeActive(config);
            SetStatus("Modo console ativo. O app fica na bandeja.");
            StartLoop();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Start: {ex}");
            SetStatus($"Falha ao entrar no modo console: {ex.Message}", warning: true);
            if (Engine.State.IsActive)
            {
                // Half-applied setup: put the desktop back instead of leaving it broken.
                try { await Task.Run(() => Engine.Stop()); }
                catch (Exception stopEx) { AppLog.Write($"Start rollback: {stopEx}"); }
            }
        }
        finally
        {
            IsStarting = false;
            _busy = false;
        }
    }

    [RelayCommand]
    public async Task RestoreNowAsync()
    {
        if (_busy && !Engine.State.IsActive) return;
        StopLoop();
        _busy = true;
        SetStatus("Restaurando suas telas…");
        try
        {
            await Task.Run(() => Engine.Stop());
            IsConsoleActive = false;
            SetStatus("Setup restaurado.");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Restore: {ex}");
            SetStatus($"Falha ao restaurar: {ex.Message}", warning: true);
        }
        finally
        {
            _busy = false;
        }

        // Screens were renumbered/re-enabled; refresh names and resolutions.
        Engine.Monitors.ClearCache();
        await ReloadAsync();
    }

    public bool TryCloseToTray()
    {
        if (!IsConsoleActive) return false;
        SetStatus("O modo console segue ativo na bandeja.");
        return true;
    }

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
            FullscreenMode = ModePlaynite ? "playnite" : ModeXbox ? "xboxMode" : "bigPicture",
            AudioDeviceId = auto ? "" : audioId,
            AudioDeviceName = auto ? "" : SelectedAudio?.Text ?? "",
            AudioAutoSwitch = auto,
            FpsLimit = ReadFpsLimit(),
            HdrEnable = HdrEnable,
            VrrEnable = VrrEnable
        };

        if (Monitors.Count == 0) return config;

        config.FocusMonitor = Monitors.FirstOrDefault(m => m.IsFocus)?.Monitor.StableId ?? "";
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
            SetStatus($"Não foi possível salvar a configuração: {ex.Message}", warning: true);
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
        var focus = Monitors.FirstOrDefault(m => m.IsFocus);
        var hidden = Monitors.Where(m => m.IsHide).Select(m => m.Name).ToList();
        var app = ModePlaynite ? "Playnite" : ModeXbox ? "Modo Xbox" : "Big Picture";

        var parts = new List<string> { app };
        var audio = SelectedAudio?.Value switch
        {
            null or "" => null,
            ConsoleEngine.AudioOnConnectId => "áudio ao conectar",
            _ => $"áudio: {SelectedAudio.Text}"
        };
        if (audio is not null) parts.Add(audio);
        var fps = ReadFpsLimit();
        if (IsFpsAvailable && fps > 0) parts.Add($"{fps} FPS");

        var where = focus is null ? "Escolha a tela de jogo" : $"Jogar em {focus.Name}";
        var off = hidden.Count > 0 ? $" · desliga {string.Join(" e ", hidden)}" : "";
        SummaryText = $"{where}{off}\n{string.Join(" · ", parts)}";
    }

    private static string DescribeActive(AppConfig config)
    {
        var mode = config.FullscreenMode switch
        {
            "playnite" => "Feche o Playnite para voltar ao seu setup automaticamente.",
            "xboxMode" => "No Modo Xbox a volta é manual: use Restaurar agora ou o menu da bandeja.",
            _ => "Feche o Big Picture para voltar ao seu setup automaticamente."
        };
        return $"{mode}\nO app continua na bandeja do sistema.";
    }

    private void ShowPage(bool settings)
    {
        IsSettingsPage = settings;
        IsHomePage = !settings;
    }

    private void SetStatus(string text, bool warning = false)
    {
        StatusText = text;
        StatusIsWarning = warning;
    }

    private static int ParseLeft(string? leftTop)
    {
        if (string.IsNullOrWhiteSpace(leftTop)) return int.MaxValue;
        var m = Regex.Match(leftTop, @"(-?\d+)");
        return m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ? x : int.MaxValue;
    }
}
