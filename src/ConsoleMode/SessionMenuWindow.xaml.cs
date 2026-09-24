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
    private const double WidthDip = 760;
    private const double HeightDip = 680;

    private readonly nint _hwnd;
    private readonly GamepadNavigator _navigator;

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

        _navigator = new GamepadNavigator(DispatcherQueue, Root, _hwnd)
        {
            // Left/Right on the volume row adjust it instead of moving focus.
            BeforeMove = direction =>
            {
                if (Root.XamlRoot is not { } root || !ReferenceEquals(FocusManager.GetFocusedElement(root), VolumeRow)) return false;
                if (direction == FocusNavigationDirection.Left) { ViewModel.ChangeVolume(-1); return true; }
                if (direction == FocusNavigationDirection.Right) { ViewModel.ChangeVolume(1); return true; }
                return false;
            }
        };
        _navigator.BackRequested += ViewModel.SessionMenuBack;
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
            ViewModel.SessionMenuBack();
        };
        ViewModel.PropertyChanged += OnViewModelChanged;
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
