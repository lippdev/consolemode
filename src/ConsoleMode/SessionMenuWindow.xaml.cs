using System.ComponentModel;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;

namespace ConsoleMode;

/// <summary>
/// The Select + Y menu over the game (see MainViewModel.SessionMenu). Centered on the game
/// screen, always on top, driven by the controller through GamepadNavigator or by keyboard.
/// Exclusive-fullscreen games can't be covered; Big Picture and borderless games can.
/// </summary>
public sealed partial class SessionMenuWindow : Window
{
    private const double WidthDip = 1100;
    private const double HeightDip = 620;

    private readonly nint _hwnd;
    private readonly GamepadNavigator _navigator;
    private bool _editingVolume;

    public MainViewModel ViewModel { get; }

    public SessionMenuWindow(MainViewModel viewModel, ScreenRect? target)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        WindowPlacement.CenterOn(appWindow, target, WidthDip, HeightDip);

        // No foreground gate and HID for PlayStation pads: the game often keeps the focus even
        // with the menu on top, and then the menu never heard the controller.
        _navigator = new GamepadNavigator(DispatcherQueue, Root, readSonyHid: true)
        {
            // Adjust mode on the volume tile: Left/Right change it, Up/Down are swallowed.
            BeforeMove = direction =>
            {
                if (!_editingVolume) return false;
                if (direction == FocusNavigationDirection.Left) ViewModel.ChangeVolume(-1);
                else if (direction == FocusNavigationDirection.Right) ViewModel.ChangeVolume(1);
                return true;
            }
        };
        _navigator.OptionRequested += () => { if (IsVolumeFocused()) ViewModel.ToggleMuteCommand.Execute(null); };
        _navigator.BackRequested += () =>
        {
            if (_editingVolume) SetEditingVolume(false);
            else ViewModel.SessionMenuBack();
        };
        _navigator.Start();

        // PreviewKeyDown (tunneling): the ScrollViewer would otherwise handle the arrows as scrolling
        // before they ever bubble up to Root.
        Root.PreviewKeyDown += (_, e) =>
        {
            // Arrows go through the same path as the D-pad.
            var direction = e.Key switch
            {
                Windows.System.VirtualKey.Up => FocusNavigationDirection.Up,
                Windows.System.VirtualKey.Down => FocusNavigationDirection.Down,
                Windows.System.VirtualKey.Left => FocusNavigationDirection.Left,
                Windows.System.VirtualKey.Right => FocusNavigationDirection.Right,
                _ => FocusNavigationDirection.None
            };
            if (direction != FocusNavigationDirection.None)
            {
                e.Handled = true;
                if (_navigator.BeforeMove?.Invoke(direction) != true)
                    FocusManager.TryMoveFocus(direction, new FindNextElementOptions { SearchRoot = Root });
                return;
            }
            if (e.Key is not (Windows.System.VirtualKey.Escape or Windows.System.VirtualKey.GamepadB)) return;
            e.Handled = true;
            if (_editingVolume) { SetEditingVolume(false); return; }
            ViewModel.SessionMenuBack();
        };
        ViewModel.PropertyChanged += OnViewModelChanged;
        VolumeRow.LostFocus += (_, _) => SetEditingVolume(false);
        // BringToFront runs before the tree exists; the real first focus happens here.
        Root.Loaded += (_, _) => FirstRow.Focus(FocusState.Keyboard);
        Activated += (_, args) =>
        {
            // XamlRoot is null on the very first activation; GetFocusedElement(null) throws.
            if (args.WindowActivationState != WindowActivationState.Deactivated && Root.XamlRoot is { } root
                && FocusManager.GetFocusedElement(root) is null)
                FirstRow.Focus(FocusState.Keyboard);
        };
        Closed += (_, _) =>
        {
            ViewModel.PropertyChanged -= OnViewModelChanged;
            _navigator.Dispose();
        };
    }

    private bool IsVolumeFocused() =>
        Root.XamlRoot is { } root && ReferenceEquals(FocusManager.GetFocusedElement(root), VolumeRow);

    /// <summary>A on the volume tile toggles adjust mode; the arrows show while it is on.</summary>
    private void OnVolumeClick(object sender, RoutedEventArgs e) => SetEditingVolume(!_editingVolume);

    private void SetEditingVolume(bool on)
    {
        _editingVolume = on;
        VolumeLeft.Visibility = VolumeRight.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Windows won't hand a background process the foreground; this forces it (PlayStation pads need it).</summary>
    public void BringToFront()
    {
        if (!NativeWindows.ForceForeground(_hwnd)) AppLog.Write("Menu da sessão: não conseguiu vir para frente");
        FirstRow.Focus(FocusState.Keyboard);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsConsoleActive) when !ViewModel.IsConsoleActive:
                Close();
                break;
            case nameof(MainViewModel.IsSessionPickerOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsSessionPickerOpen) FocusPickerSelection();
                    else FirstRow.Focus(FocusState.Keyboard);
                });
                break;
        }
    }

    private void FocusPickerSelection()
    {
        var index = Math.Max(ViewModel.SessionPickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
        if (TryFocusPickerRow(index)) return;
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
}
