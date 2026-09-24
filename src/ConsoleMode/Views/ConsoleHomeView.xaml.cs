using System.ComponentModel;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ConsoleMode.Views;

public sealed partial class ConsoleHomeView : UserControl
{
    public MainViewModel ViewModel { get; }

    private GamepadNavigator? _navigator;
    private bool _windowActive = true;

    public ConsoleHomeView()
    {
        ViewModel = App.ViewModel!;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => { _navigator?.Dispose(); _navigator = null; };
        Root.KeyDown += OnKeyDown;
        ViewModel.PropertyChanged += OnViewModelChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigator ??= new GamepadNavigator(DispatcherQueue, this);
        _navigator.BackRequested += GoBack;
        _navigator.MenuRequested += () => { if (ViewModel.CanStart && !ViewModel.IsRolePanelOpen) ViewModel.StartCommand.Execute(null); };
        _navigator.AltRequested += () => { if (!ViewModel.IsRolePanelOpen) ViewModel.OpenFullSettingsCommand.Execute(null); };
        if (App.MainWindowInstance is { } window)
            window.Activated += (_, args) =>
            {
                _windowActive = args.WindowActivationState != WindowActivationState.Deactivated;
                RefreshNavigator();
            };
        RefreshNavigator();
        FocusDefault();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsConsoleUi):
            case nameof(MainViewModel.IsConsoleActive):
                RefreshNavigator();
                DispatcherQueue.TryEnqueue(FocusDefault);
                break;
            case nameof(MainViewModel.IsRolePanelOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsRolePanelOpen) RolePlay.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
        }
    }

    /// <summary>
    /// Only listen while this interface is showing and the window is in front: A pressed inside
    /// Big Picture must not click something in a hidden window. (XInput reads without focus.)
    /// </summary>
    private void RefreshNavigator()
    {
        if (_navigator is null) return;
        var listen = ViewModel.IsConsoleUi && _windowActive && Visibility == Visibility.Visible;
        if (listen && !_navigator.IsRunning) _navigator.Start();
        else if (!listen && _navigator.IsRunning) _navigator.Stop();
    }

    private void FocusDefault()
    {
        if (!ViewModel.IsConsoleUi) return;
        if (ViewModel.IsConsoleActive) { RestoreButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.CanStart) { PlayButton.Focus(FocusState.Keyboard); return; }
        (FocusManager.FindFirstFocusableElement(ScreenCards) as Control)?.Focus(FocusState.Keyboard);
    }

    private void GoBack()
    {
        if (ViewModel.IsRolePanelOpen) ViewModel.CloseRolePanelCommand.Execute(null);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape or VirtualKey.GamepadB)
        {
            GoBack();
            e.Handled = true;
        }
    }
}
