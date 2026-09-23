using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;

namespace ConsoleMode;

public partial class App : Application
{
    private const string InstanceMutexName = @"Local\ConsoleMode.Instance";
    private const string ShowSignalName = @"Local\ConsoleMode.Show";
    private const string StartSignalName = @"Local\ConsoleMode.Start";

    private MainWindow? _window;
    private TrayService? _tray;
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSignal;
    private EventWaitHandle? _startSignal;
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
        var autoStart = HasArg(ShortcutService.StartArgument);
        // --tray: launched with Windows; stay in the tray until the user opens the window.
        var trayOnly = !autoStart && HasArg(StartupService.TrayArgument);

        _instanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // Hand the request to the running instance (tray) instead of fighting over the screens.
            if (!trayOnly) SignalRunningInstance(autoStart ? StartSignalName : ShowSignalName);
            Exit();
            return;
        }

        try
        {
            AppPaths.Initialize();
            LocalizationService.SetLanguage(ConfigService.Load().AppLanguage);
            ControllerInput.Warmup();
            AppLog.Write($"Startup: exe={Environment.ProcessPath}, args={string.Join(' ', Environment.GetCommandLineArgs().Skip(1))}, " +
                         $"mmt={AppPaths.HasMmt}, svv={AppPaths.HasSvv}, rtss-cli={AppPaths.HasRtssCli}");

            ViewModel = new MainViewModel();
            _window = new MainWindow(ViewModel);
            MainWindowInstance = _window;
            _tray = new TrayService(_window, ViewModel);
            ListenForSignals();

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

        _signalWaits.Add(ThreadPool.RegisterWaitForSingleObject(_showSignal, (_, _) =>
            _window?.DispatcherQueue.TryEnqueue(() => _tray?.ShowWindow()), null, Timeout.Infinite, executeOnlyOnce: false));

        _signalWaits.Add(ThreadPool.RegisterWaitForSingleObject(_startSignal, (_, _) =>
            _window?.DispatcherQueue.TryEnqueue(async () =>
            {
                if (ViewModel is null) return;
                if (ViewModel.IsConsoleActive || !await ViewModel.TryAutoStartAsync())
                    _tray?.ShowWindow();
            }), null, Timeout.Infinite, executeOnlyOnce: false));
    }
}
