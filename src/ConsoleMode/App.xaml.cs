using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;

namespace ConsoleMode;

public partial class App : Application
{
    private MainWindow? _window;
    private TrayService? _tray;

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
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppPaths.Initialize();
        ViewModel = new MainViewModel();
        _window = new MainWindow(ViewModel);
        MainWindowInstance = _window;
        ViewModel.Initialize();
        _tray = new TrayService(_window, ViewModel);
        _window.Activate();
    }
}
