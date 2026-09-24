using System.ComponentModel;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.Views;

public sealed partial class HomeView : UserControl
{
    public MainViewModel ViewModel { get; } = App.ViewModel!;

    // The desktop interface ignores the pad except for one thing: A or Start on a detected
    // controller jumps to the console interface, so nobody is stuck with a mouse-only screen.
    private ControllerInput? _pad;
    private bool _windowActive = true;

    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => { _pad?.Dispose(); _pad = null; };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_pad is not null) return;
        _pad = new ControllerInput(DispatcherQueue);
        _pad.Pressed += action =>
        {
            if (action is ControllerAction.Confirm or ControllerAction.Menu && !ViewModel.IsConsoleUi && !ViewModel.IsConsoleActive)
                ViewModel.SwitchUiCommand.Execute("console");
        };
        ViewModel.PropertyChanged += OnViewModelChanged;
        if (App.MainWindowInstance is { } window)
            window.Activated += (_, args) =>
            {
                _windowActive = args.WindowActivationState != WindowActivationState.Deactivated;
                RefreshListener();
            };
        RefreshListener();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsConsoleUi) or nameof(MainViewModel.IsConsoleActive)
            or nameof(MainViewModel.ControllerDetected))
            RefreshListener();
    }

    /// <summary>XInput reads without focus, so only listen while this window is in front.</summary>
    private void RefreshListener()
    {
        if (_pad is null) return;
        var listen = !ViewModel.IsConsoleUi && !ViewModel.IsConsoleActive && ViewModel.ControllerDetected && _windowActive;
        if (listen && !_pad.IsRunning) _pad.Start();
        else if (!listen && _pad.IsRunning) _pad.Stop();
    }
}
