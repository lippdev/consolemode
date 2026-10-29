using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;

namespace ConsoleMode;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        var scale = Win32Dpi.GetScale(hwnd);
        appWindow.Resize(new Windows.Graphics.SizeInt32((int)(960 * scale), (int)(760 * scale)));
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        // Tall caption buttons: as high as the 48 px bar, instead of 32 px ones floating at the top.
        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        StyleCaptionButtons(appWindow.TitleBar, ViewModel.IsConsoleUi);

        if (File.Exists(AppPaths.IconPath))
        {
            appWindow.SetIcon(AppPaths.IconPath);
            TitleIcon.Source = new BitmapImage(new Uri(AppPaths.IconPath));
        }

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            // The screen map is laid out for ~860 DIPs; don't let the window go narrower.
            presenter.PreferredMinimumWidth = (int)(900 * scale);
            presenter.PreferredMinimumHeight = (int)(640 * scale);
        }

        appWindow.Closing += (_, e) =>
        {
            if (!ViewModel.TryCloseToTray()) return;
            e.Cancel = true;
            appWindow.Hide();
        };

        // With "Minimize to the system tray" on, minimizing leaves the taskbar; the tray icon brings it back.
        appWindow.Changed += (_, e) =>
        {
            if (!e.DidPresenterChange || !ViewModel.MinimizeToTray) return;
            if (appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized }) appWindow.Hide();
        };

        // The console interface fills the screen, like a console; the desktop one gets its size back.
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsConsoleUi)) StyleCaptionButtons(appWindow.TitleBar, ViewModel.IsConsoleUi);
            if (e.PropertyName == nameof(MainViewModel.IsConsoleUi) && appWindow.Presenter is OverlappedPresenter p)
            {
                if (ViewModel.IsConsoleUi) p.Maximize();
                else if (p.State == OverlappedPresenterState.Maximized) p.Restore();
            }
            if (e.PropertyName is nameof(MainViewModel.IsConsoleUi) or nameof(MainViewModel.IsConsoleActive))
                RefreshDesktopNavigator();
        };
        WatchDesktopPad(hwnd);
    }

    /// <summary>
    /// Minimize, maximize and close over the console interface's dark background: white, with the same soft veil on hover
    /// and press as its own buttons. The desktop interface keeps Windows' colours for its theme.
    /// </summary>
    private static void StyleCaptionButtons(AppWindowTitleBar bar, bool console)
    {
        Windows.UI.Color? Argb(byte a, byte r, byte g, byte b) => console ? Windows.UI.Color.FromArgb(a, r, g, b) : null;
        bar.ButtonForegroundColor = Argb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonInactiveForegroundColor = Argb(0x80, 0xFF, 0xFF, 0xFF);
        bar.ButtonHoverForegroundColor = Argb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonHoverBackgroundColor = Argb(0x1F, 0xFF, 0xFF, 0xFF);
        bar.ButtonPressedForegroundColor = Argb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonPressedBackgroundColor = Argb(0x33, 0xFF, 0xFF, 0xFF);
    }

    // The desktop interface, driven by the pad like a console menu (ported from PR #17):
    // D-pad/stick move, A activates (lists, toggles, cards), B closes a list or leaves Settings,
    // Start toggles Settings, Y jumps to the console interface, tour: A next / B skip.
    // The console interface has its own navigator inside ConsoleHomeView.
    private GamepadNavigator? _desktopPad;

    private void WatchDesktopPad(nint hwnd)
    {
        _desktopPad = new GamepadNavigator(DispatcherQueue, (DependencyObject)Content, hwnd)
        {
            Intercept = action =>
            {
                // Picking a shortcut presses buttons on purpose: they must not also navigate.
                if (ViewModel.IsCapturingShortcut) return true;
                if (ViewModel.TourStep <= 0) return false;
                if (action == ControllerAction.Confirm) ViewModel.TourNextCommand.Execute(null);
                else if (action == ControllerAction.Back) ViewModel.EndTourCommand.Execute(null);
                return true;
            }
        };
        _desktopPad.MenuRequested += () =>
        {
            if (ViewModel.IsSettingsPage) ViewModel.GoHomeCommand.Execute(null);
            else ViewModel.OpenSettingsCommand.Execute(null);
        };
        _desktopPad.BackRequested += () => { if (ViewModel.IsSettingsPage) ViewModel.GoHomeCommand.Execute(null); };
        _desktopPad.AltRequested += () => ViewModel.SwitchUiCommand.Execute("console");
        RefreshDesktopNavigator();
    }

    private void RefreshDesktopNavigator()
    {
        if (_desktopPad is null) return;
        var listen = !ViewModel.IsConsoleUi && !ViewModel.IsConsoleActive;
        if (listen && !_desktopPad.IsRunning) _desktopPad.Start();
        else if (!listen && _desktopPad.IsRunning) _desktopPad.Stop();
    }

    private static class Win32Dpi
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(nint hwnd);

        public static double GetScale(nint hwnd)
        {
            var dpi = GetDpiForWindow(hwnd);
            return dpi > 0 ? dpi / 96.0 : 1.0;
        }
    }
}
