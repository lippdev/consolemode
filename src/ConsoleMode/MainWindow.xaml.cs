using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;

namespace ConsoleMode;

public sealed partial class MainWindow : Window
{
    private readonly ControllerNavigator _controller;

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

        _controller = new ControllerNavigator(this, ViewModel, DefaultFocusTarget);
        Closed += (_, _) => _controller.Dispose();
    }

    /// <summary>Where a controller starts on each page.</summary>
    private UIElement? DefaultFocusTarget() => ViewModel.IsSettingsPage
        ? FocusManager.FindFirstFocusableElement(SettingsPage) as UIElement
        : HomePage.DefaultFocusTarget;

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
