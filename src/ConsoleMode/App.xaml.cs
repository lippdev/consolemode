using System.ComponentModel;
using System.Globalization;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;

namespace ConsoleMode;

public partial class App : Application
{
    private const string InstanceMutexName = @"Local\ConsoleMode.Instance";
    private const string ShowSignalName = @"Local\ConsoleMode.Show";
    private const string StartSignalName = @"Local\ConsoleMode.Start";
    private const string StopSignalName = @"Local\ConsoleMode.Stop";

    private MainWindow? _window;
    private TrayService? _tray;
    private ControllerHoldWatcher? _guide;
    private ControllerHoldWatcher? _exitChord;
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSignal;
    private EventWaitHandle? _startSignal;
    private EventWaitHandle? _stopSignal;
    private readonly List<RegisteredWaitHandle> _signalWaits = [];

    public static MainWindow? MainWindowInstance { get; private set; }
    public static MainViewModel? ViewModel { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            AppLog.Write($"Unhandled: {e.Exception}");
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => AppLog.Write($"Fatal: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Write($"Task: {e.Exception}");
            e.SetObserved();
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToList();
        bool HasArg(string name) => cliArgs.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        // consolemode://start|stop|show arrives as the only argument (ProtocolService).
        var protocolAction = cliArgs.Select(ProtocolService.ParseAction).FirstOrDefault(a => a is not null);
        var autoStart = HasArg(ShortcutService.StartArgument) || protocolAction == ProtocolService.StartAction;
        var stopRequest = protocolAction == ProtocolService.StopAction;
        // --tray: launched with Windows; stay in the tray until the user opens the window.
        var trayOnly = !autoStart && HasArg(StartupService.TrayArgument);

        _instanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // Hand the request to the running instance (tray) instead of fighting over the screens.
            if (!trayOnly) SignalRunningInstance(autoStart ? StartSignalName : stopRequest ? StopSignalName : ShowSignalName);
            Exit();
            return;
        }

        try
        {
            AppPaths.Initialize();
            LocalizationService.SetLanguage(LocalizationService.ResolveInitial(
                ConfigService.Load().AppLanguage, StartupService.ReadInstallerLanguage(), CultureInfo.CurrentUICulture.Name));
            ControllerInput.Warmup();
            ProtocolService.EnsureRegistered();
            AppLog.Write($"Startup: exe={Environment.ProcessPath}, args={string.Join(' ', Environment.GetCommandLineArgs().Skip(1))}, " +
                         $"mmt={AppPaths.HasMmt}, svv={AppPaths.HasSvv}, rtss-cli={AppPaths.HasRtssCli}");

            ViewModel = new MainViewModel();
            _window = new MainWindow(ViewModel);
            MainWindowInstance = _window;
            _tray = new TrayService(_window, ViewModel);
            ListenForSignals();
            WatchGuideButton();

            // A stop request with nothing running just opens the window.
            if (!autoStart && !trayOnly) _window.Activate();
            await ViewModel.InitializeAsync(interactive: !autoStart && !trayOnly);

            if (autoStart && !await ViewModel.TryAutoStartAsync())
                _window.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Startup: {ex}");
            _window?.Activate();
        }
    }

    private static void SignalRunningInstance(string signalName)
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(signalName, out var handle))
            {
                using (handle) handle.Set();
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Instância: não foi possível avisar a instância aberta: {ex.Message}");
        }
    }

    private void ListenForSignals()
    {
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSignalName);
        _startSignal = new EventWaitHandle(false, EventResetMode.AutoReset, StartSignalName);
        _stopSignal = new EventWaitHandle(false, EventResetMode.AutoReset, StopSignalName);

        _signalWaits.Add(ThreadPool.RegisterWaitForSingleObject(_showSignal, (_, _) =>
            _window?.DispatcherQueue.TryEnqueue(() => _tray?.ShowWindow()), null, Timeout.Infinite, executeOnlyOnce: false));

        _signalWaits.Add(ThreadPool.RegisterWaitForSingleObject(_startSignal, (_, _) =>
            _window?.DispatcherQueue.TryEnqueue(() => _ = HandleStartRequestAsync()), null, Timeout.Infinite, executeOnlyOnce: false));

        _signalWaits.Add(ThreadPool.RegisterWaitForSingleObject(_stopSignal, (_, _) =>
            _window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ViewModel is null) return;
                if (ViewModel.IsConsoleActive) _ = ViewModel.RestoreNowAsync();
                else _tray?.ShowWindow();
            }), null, Timeout.Infinite, executeOnlyOnce: false));
    }

    /// <summary>Shortcut, protocol or Home button: enter console mode, or show why it couldn't.</summary>
    private async Task HandleStartRequestAsync()
    {
        if (ViewModel is null) return;
        if (ViewModel.IsConsoleActive || !await ViewModel.TryAutoStartAsync())
            _tray?.ShowWindow();
    }

    /// <summary>
    /// Idle: the Home button enters console mode (a hold, or a press once Game Bar's shortcut
    /// is off). In a session Big Picture owns the Home button, so Start + Back held together
    /// restores the desk instead.
    /// </summary>
    private void WatchGuideButton()
    {
        if (_window is null || ViewModel is null) return;
        _guide = new ControllerHoldWatcher(_window.DispatcherQueue, ControllerHoldWatcher.GuideButton, "botão Home");
        _guide.Held += () =>
        {
            if (ViewModel is not null) ViewModel.LaunchedByController = true;
            _ = HandleStartRequestAsync();
        };
        _exitChord = new ControllerHoldWatcher(_window.DispatcherQueue, (ushort)(ControllerHoldWatcher.StartButton | ControllerHoldWatcher.BackButton), "Start + Back");
        _exitChord.Held += () => { if (ViewModel?.IsConsoleActive == true) _ = ViewModel.RestoreNowAsync(); };
        ViewModel.PropertyChanged += OnViewModelChangedForGuide;
        RefreshGuideWatch();
    }

    private void OnViewModelChangedForGuide(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.HomeButtonLaunch) or nameof(MainViewModel.HomeButtonShortPress)
            or nameof(MainViewModel.IsConsoleActive))
            RefreshGuideWatch();
    }

    private void RefreshGuideWatch()
    {
        if (_guide is null || _exitChord is null || ViewModel is null) return;
        _guide.HoldDuration = ViewModel.HomeButtonShortPress ? TimeSpan.Zero : ControllerHoldWatcher.LongHold;
        if (!ViewModel.HomeButtonLaunch) { _guide.Stop(); _exitChord.Stop(); return; }
        if (ViewModel.IsConsoleActive) { _guide.Stop(); _exitChord.Start(); }
        else { _exitChord.Stop(); _guide.Start(); }
    }
}
