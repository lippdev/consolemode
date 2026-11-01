using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
        StyleCaptionButtons(appWindow.TitleBar);

        if (File.Exists(AppPaths.IconPath)) appWindow.SetIcon(AppPaths.IconPath);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            // The interface is laid out for ~860 DIPs; don't let the window go narrower.
            presenter.PreferredMinimumWidth = (int)(900 * scale);
            presenter.PreferredMinimumHeight = (int)(640 * scale);
        }

        // The interface fills the screen, like a console (restoring gives the window its size back). Done the first time
        // the window shows: maximizing a window started hidden (--start, the tray) would show it.
        var maximized = false;
        Activated += (_, _) =>
        {
            if (maximized) return;
            maximized = true;
            if (appWindow.Presenter is OverlappedPresenter p) p.Maximize();
        };

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
    }

    /// <summary>
    /// Minimize, maximize and close over the interface's dark background: white, with the same soft veil on hover and
    /// press as its own buttons.
    /// </summary>
    private static void StyleCaptionButtons(AppWindowTitleBar bar)
    {
        bar.ButtonForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
        bar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF);
        bar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        bar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
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
