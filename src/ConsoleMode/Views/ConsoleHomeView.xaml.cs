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
        _navigator ??= new GamepadNavigator(DispatcherQueue, this,
            App.MainWindowInstance is { } w ? WinRT.Interop.WindowNative.GetWindowHandle(w) : 0);
        _navigator.BackRequested += GoBack;
        _navigator.MenuRequested += () => { if (ViewModel.CanStart && !ViewModel.IsRolePanelOpen && !ViewModel.IsConsoleSettingsOpen && !ViewModel.IsPickerOpen) ViewModel.StartCommand.Execute(null); };
        _navigator.AltRequested += () =>
        {
            if (ViewModel.IsRolePanelOpen || ViewModel.IsPickerOpen || ViewModel.IsControllerTestOpen) return;
            if (ViewModel.IsConsoleSettingsOpen) ViewModel.CloseConsoleSettingsCommand.Execute(null);
            else ViewModel.OpenFullSettingsCommand.Execute(null);
        };
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
            case nameof(MainViewModel.IsPickerOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsPickerOpen) FocusPickerSelection();
                    else if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsControllerTestOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsControllerTestOpen) CopyDiagnosticsButton.Focus(FocusState.Keyboard);
                    else if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsConsoleSettingsOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
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
        // Not Visibility: this runs from the IsConsoleUi change, before the binding has updated it.
        var listen = ViewModel.IsConsoleUi && _windowActive;
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

    /// <summary>Land on the current value so A confirms it and the stick moves from there.</summary>
    private void FocusPickerSelection()
    {
        var index = Math.Max(ViewModel.PickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
        if (TryFocusPickerRow(index)) return;
        // Containers appear on the next layout pass; try once more then.
        void OnLayout(object? s, object e)
        {
            PickerList.LayoutUpdated -= OnLayout;
            TryFocusPickerRow(index);
        }
        PickerList.LayoutUpdated += OnLayout;
    }

    private bool TryFocusPickerRow(int index)
    {
        if (PickerList.ContainerFromIndex(index) is not { } container) return false;
        return (FocusManager.FindFirstFocusableElement(container) as Control)?.Focus(FocusState.Keyboard) == true;
    }

    private void GoBack()
    {
        if (ViewModel.IsControllerTestOpen) ViewModel.CloseControllerTestCommand.Execute(null);
        else if (ViewModel.IsPickerOpen) ViewModel.ClosePickerCommand.Execute(null);
        else if (ViewModel.IsRolePanelOpen) ViewModel.CloseRolePanelCommand.Execute(null);
        else if (ViewModel.IsConsoleSettingsOpen) ViewModel.CloseConsoleSettingsCommand.Execute(null);
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
