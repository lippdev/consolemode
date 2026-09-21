using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

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
    private DispatcherQueueTimer? _timer;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private bool _busy;

    [ObservableProperty] private int _wizardStep;
    [ObservableProperty] private bool _isConsoleActive;
    [ObservableProperty] private string _statusText = "Pronto.";
    [ObservableProperty] private string _statusIcon = "\uE73E";
    [ObservableProperty] private string _reviewText = "";
    [ObservableProperty] private string _activeDescText = "";
    [ObservableProperty] private string _playniteDesc = "Abre o Playnite em tela cheia no monitor de foco. Restauração automática ao sair.";
    [ObservableProperty] private string _fpsStatusText = "";
    [ObservableProperty] private string _audioHintText = "A saída escolhida é aplicada ao iniciar e restaurada ao sair. 'Usar áudio ao conectar' troca automaticamente quando a TV conectar.";
    [ObservableProperty] private ComboOption? _selectedHideStrategy;
    [ObservableProperty] private ComboOption? _selectedFps;
    [ObservableProperty] private ComboOption? _selectedAudio;
    [ObservableProperty] private string _customFpsText = "60";
    [ObservableProperty] private bool _showCustomFps;
    [ObservableProperty] private bool _modeBigPicture = true;
    [ObservableProperty] private bool _modePlaynite;
    [ObservableProperty] private bool _modeXbox;
    [ObservableProperty] private bool _hdrEnable;
    [ObservableProperty] private bool _vrrEnable;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _showNext = true;
    [ObservableProperty] private bool _showStart;
    [ObservableProperty] private bool _canRestore;
    [ObservableProperty] private bool _tab0Active = true;
    [ObservableProperty] private bool _tab1Active;
    [ObservableProperty] private bool _tab2Active;
    [ObservableProperty] private bool _tab3Active;
    [ObservableProperty] private Visibility _step0Visibility = Visibility.Visible;
    [ObservableProperty] private Visibility _step1Visibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _step2Visibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _step3Visibility = Visibility.Collapsed;
    [ObservableProperty] private Visibility _activePanelVisibility = Visibility.Collapsed;

    public void Initialize()
    {
        HideStrategies.Clear();
        HideStrategies.Add(new ComboOption { Text = "Desconectar no Windows (recomendado)", Value = "disconnect" });
        HideStrategies.Add(new ComboOption { Text = "Cortinas pretas (overlay)", Value = "blackCurtain" });
        HideStrategies.Add(new ComboOption { Text = "Apagar painel via DDC/CI", Value = "turnOff" });

        FpsOptions.Clear();
        FpsOptions.Add(new ComboOption { Text = "(nao limitar)", Value = "0" });
        foreach (var fps in FpsPresets)
            FpsOptions.Add(new ComboOption { Text = $"{fps} FPS", Value = fps.ToString() });
        FpsOptions.Add(new ComboOption { Text = "Personalizado…", Value = FpsCustomValue.ToString() });

        if (!Engine.Launch.IsPlayniteAvailable())
            PlayniteDesc = "Playnite não encontrado. Instale o Playnite para usar este modo.";

        FpsStatusText = Engine.Rtss.IsReady
            ? "RTSS encontrado. O limite é global enquanto o modo console estiver ativo."
            : "RTSS / rtss-cli não encontrado. O limite de FPS fica indisponível até instalar o RivaTuner e o rtss-cli.";

        ReloadFromConfig();
        ApplyStepUi();
        SetStatus("Pronto.");
    }

    public void ReloadFromConfig()
    {
        var config = ConfigService.Load();
        LoadMonitors(config);
        LoadAudio(config);

        SelectedHideStrategy = HideStrategies.FirstOrDefault(s => s.Value == config.HideStrategy) ?? HideStrategies[0];
        HdrEnable = config.HdrEnable;
        VrrEnable = config.VrrEnable;
        ModeBigPicture = config.FullscreenMode is "bigPicture" or "";
        ModePlaynite = config.FullscreenMode == "playnite";
        ModeXbox = config.FullscreenMode == "xboxMode";

        if (config.FpsLimit > 0 && FpsPresets.Contains(config.FpsLimit))
        {
            SelectedFps = FpsOptions.FirstOrDefault(o => o.Value == config.FpsLimit.ToString());
            ShowCustomFps = false;
        }
        else if (config.FpsLimit > 0)
        {
            SelectedFps = FpsOptions.FirstOrDefault(o => o.Value == FpsCustomValue.ToString());
            CustomFpsText = config.FpsLimit.ToString();
            ShowCustomFps = true;
        }
        else
        {
            SelectedFps = FpsOptions[0];
            ShowCustomFps = false;
        }
    }

    private void LoadMonitors(AppConfig config)
    {
        Monitors.Clear();
        IReadOnlyList<MonitorInfo> list;
        try { list = Engine.Monitors.GetMonitors(true); }
        catch (Exception ex)
        {
            SetStatus($"MultiMonitorTool indisponível: {ex.Message}", warning: true);
            return;
        }

        var savedFocus = config.FocusMonitor;
        if (string.IsNullOrWhiteSpace(savedFocus))
            savedFocus = list.FirstOrDefault(m => m.IsPrimary)?.Name ?? list.FirstOrDefault()?.Name ?? "";

        foreach (var monitor in list)
        {
            var modes = new List<DisplayModeOption>
            {
                new() { Text = "(atual / não alterar)", Key = "current", UseCurrent = true }
            };
            modes.AddRange(Engine.Monitors.GetDisplayModes(monitor.Name, monitor));

            DisplayModeOption selected = modes[0];
            if (config.MonitorModes.TryGetValue(monitor.Name, out var saved))
            {
                selected = modes.FirstOrDefault(m => m.Key == saved.Key) ?? modes[0];
            }

            var row = new MonitorRowViewModel(
                monitor,
                monitor.Name == savedFocus,
                config.HideMonitors.Contains(monitor.Name, StringComparer.OrdinalIgnoreCase),
                selected,
                modes,
                OnFocusChanged);
            Monitors.Add(row);
        }

        if (!Monitors.Any(m => m.IsFocus) && Monitors.Count > 0)
            Monitors[0].IsFocus = true;
    }

    private void LoadAudio(AppConfig config)
    {
        AudioOptions.Clear();
        AudioOptions.Add(new ComboOption { Text = "(não alterar)", Value = "" });
        AudioOptions.Add(new ComboOption { Text = "Usar áudio ao conectar (TV/monitor)", Value = ConsoleEngine.AudioOnConnectId });

        if (AppPaths.HasSvv)
        {
            foreach (var device in Engine.Audio.GetDevices(true))
                AudioOptions.Add(new ComboOption { Text = device.Name, Value = device.FriendlyId });
        }
        else
        {
            AudioHintText = "SoundVolumeView.exe não encontrado. Coloque-o em ConsoleMode_Data/tools para trocar o áudio.";
        }

        if (config.AudioAutoSwitch)
            SelectedAudio = AudioOptions.FirstOrDefault(a => a.Value == ConsoleEngine.AudioOnConnectId);
        else if (!string.IsNullOrWhiteSpace(config.AudioDeviceId))
            SelectedAudio = AudioOptions.FirstOrDefault(a => a.Value == config.AudioDeviceId) ?? AudioOptions[0];
        else
            SelectedAudio = AudioOptions[0];
    }

    private void OnFocusChanged(MonitorRowViewModel source)
    {
        foreach (var row in Monitors)
        {
            if (!ReferenceEquals(row, source) && row.IsFocus)
                row.IsFocus = false;
        }
    }

    partial void OnSelectedFpsChanged(ComboOption? value)
    {
        ShowCustomFps = value?.Value == FpsCustomValue.ToString();
    }

    partial void OnWizardStepChanged(int value) => ApplyStepUi();

    [RelayCommand]
    public void GoTab(string step)
    {
        if (IsConsoleActive) return;
        if (int.TryParse(step, out var n)) WizardStep = n;
    }

    [RelayCommand]
    public void Back()
    {
        if (WizardStep > 0) WizardStep--;
    }

    [RelayCommand]
    public void Next()
    {
        if (WizardStep < 3) WizardStep++;
        if (WizardStep == 3) RefreshReview();
    }

    [RelayCommand]
    public void HideOthers()
    {
        var focus = Monitors.FirstOrDefault(m => m.IsFocus);
        foreach (var row in Monitors)
            row.IsHide = focus is not null && !ReferenceEquals(row, focus);
    }

    [RelayCommand]
    public void Refresh()
    {
        var config = BuildConfig();
        ConfigService.Save(config);
        Engine.Monitors.ClearCache();
        Engine.Audio.ClearCache();
        ReloadFromConfig();
        SetStatus("Lista de monitores e áudio atualizada.");
    }

    [RelayCommand]
    public void Save()
    {
        ConfigService.Save(BuildConfig());
        SetStatus("Configuração salva.");
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (_busy) return;
        var config = BuildConfig();
        if (string.IsNullOrWhiteSpace(config.FocusMonitor))
        {
            SetStatus("Selecione o monitor de foco.", warning: true);
            return;
        }

        ConfigService.Save(config);
        _busy = true;
        SetStatus("Iniciando modo console…");
        try
        {
            var focus = Monitors.FirstOrDefault(m => m.IsFocus)?.Monitor;
            await Task.Run(() => Engine.Start(config, focus));
            IsConsoleActive = true;
            CanRestore = true;
            ApplyStepUi();
            ActiveDescText = DescribeActive(config);
            SetStatus("Modo console ativo. O app fica na bandeja.");
            StartLoop();
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao iniciar: {ex.Message}", warning: true);
        }
        finally
        {
            _busy = false;
        }
    }

    [RelayCommand]
    public async Task RestoreNowAsync()
    {
        if (_busy && !Engine.State.IsActive) return;
        StopLoop();
        _busy = true;
        SetStatus("Restaurando setup…");
        try
        {
            await Task.Run(() => Engine.Stop());
            IsConsoleActive = false;
            CanRestore = false;
            ApplyStepUi();
            SetStatus("Setup restaurado.");
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao restaurar: {ex.Message}", warning: true);
        }
        finally
        {
            _busy = false;
        }
    }

    public bool TryCloseToTray()
    {
        if (!IsConsoleActive) return false;
        SetStatus("Modo console segue ativo na bandeja.");
        return true;
    }

    private void StartLoop()
    {
        StopLoop();
        _timer = _dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(Engine.PollDelayMs());
        _timer.Tick += (_, _) =>
        {
            if (_busy) return;
            string result;
            try { result = Engine.Tick(); }
            catch (Exception ex)
            {
                AppLog.Write($"Loop: {ex.Message}");
                return;
            }

            if (result == "exit")
            {
                _ = RestoreNowAsync();
                return;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(Engine.PollDelayMs());
        };
        _timer.Start();
    }

    private void StopLoop()
    {
        if (_timer is null) return;
        _timer.Stop();
        _timer = null;
    }

    private AppConfig BuildConfig()
    {
        var focus = Monitors.FirstOrDefault(m => m.IsFocus)?.Monitor.Name ?? "";
        var hide = Monitors.Where(m => m.IsHide && m.Monitor.Name != focus).Select(m => m.Monitor.Name).ToList();
        var audioId = SelectedAudio?.Value ?? "";
        var auto = audioId == ConsoleEngine.AudioOnConnectId;
        var modes = new Dictionary<string, SavedDisplayMode>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Monitors)
        {
            if (row.SelectedMode is null || row.SelectedMode.UseCurrent) continue;
            modes[row.Monitor.Name] = new SavedDisplayMode
            {
                Width = row.SelectedMode.Width,
                Height = row.SelectedMode.Height,
                Frequency = row.SelectedMode.Frequency
            };
        }

        return new AppConfig
        {
            FocusMonitor = focus,
            HideMonitors = hide,
            HideStrategy = SelectedHideStrategy?.Value ?? "disconnect",
            FullscreenMode = ModePlaynite ? "playnite" : ModeXbox ? "xboxMode" : "bigPicture",
            AudioDeviceId = auto ? "" : audioId,
            AudioDeviceName = auto ? "" : SelectedAudio?.Text ?? "",
            AudioAutoSwitch = auto,
            FpsLimit = ReadFpsLimit(),
            MonitorModes = modes,
            HdrEnable = HdrEnable,
            VrrEnable = VrrEnable
        };
    }

    private int ReadFpsLimit()
    {
        if (SelectedFps is null) return 0;
        if (SelectedFps.Value == FpsCustomValue.ToString())
            return int.TryParse(CustomFpsText, out var custom) ? custom : 0;
        return int.TryParse(SelectedFps.Value, out var fps) ? fps : 0;
    }

    private void RefreshReview()
    {
        var config = BuildConfig();
        var focus = Monitors.FirstOrDefault(m => m.IsFocus);
        var hide = string.Join(", ", config.HideMonitors);
        if (string.IsNullOrWhiteSpace(hide)) hide = "(nenhum)";
        var mode = config.FullscreenMode switch
        {
            "playnite" => "Playnite (tela cheia)",
            "xboxMode" => "Modo Xbox (Win+F11) — Alpha",
            _ => "Steam Big Picture"
        };
        var audio = config.AudioAutoSwitch
            ? "Usar áudio ao conectar"
            : string.IsNullOrWhiteSpace(config.AudioDeviceName) ? "(não alterar)" : config.AudioDeviceName;
        var fps = config.FpsLimit > 0 ? $"{config.FpsLimit} FPS" : "(não limitar)";
        var hideLabel = SelectedHideStrategy?.Text ?? config.HideStrategy;
        ReviewText =
            $"Foco: {focus?.Title ?? config.FocusMonitor}\n" +
            $"Esconder: {hide}\n" +
            $"Estratégia: {hideLabel}\n" +
            $"Modo: {mode}\n" +
            $"Áudio: {audio}\n" +
            $"HDR: {(config.HdrEnable ? "sim" : "não")}    VRR: {(config.VrrEnable ? "sim" : "não")}\n" +
            $"Limite de FPS: {fps}";
    }

    private static string DescribeActive(AppConfig config)
    {
        var mode = config.FullscreenMode switch
        {
            "playnite" => "Playnite em tela cheia. Feche o Playnite para restaurar automaticamente.",
            "xboxMode" => "Modo Xbox ativo. A restauração é manual: use Restaurar agora ou o menu da bandeja.",
            _ => "Steam Big Picture no monitor de foco. Feche o Big Picture para restaurar automaticamente."
        };
        return $"O desktop foi ajustado para o modo console.\n{mode}\nO app permanece na bandeja do sistema.";
    }

    private void ApplyStepUi()
    {
        if (IsConsoleActive)
        {
            Step0Visibility = Step1Visibility = Step2Visibility = Step3Visibility = Visibility.Collapsed;
            ActivePanelVisibility = Visibility.Visible;
            ShowNext = false;
            ShowStart = false;
            CanGoBack = false;
            Tab0Active = Tab1Active = Tab2Active = Tab3Active = false;
            return;
        }

        ActivePanelVisibility = Visibility.Collapsed;
        Step0Visibility = WizardStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        Step1Visibility = WizardStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Visibility = WizardStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Visibility = WizardStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        Tab0Active = WizardStep == 0;
        Tab1Active = WizardStep == 1;
        Tab2Active = WizardStep == 2;
        Tab3Active = WizardStep == 3;
        CanGoBack = WizardStep > 0;
        ShowNext = WizardStep < 3;
        ShowStart = WizardStep == 3;
        if (WizardStep == 3) RefreshReview();
    }

    private void SetStatus(string text, bool warning = false)
    {
        StatusText = text;
        StatusIcon = warning ? "\uE7BA" : "\uE73E";
    }
}
