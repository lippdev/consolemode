using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ConsoleMode;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(880, 820));
        if (File.Exists(AppPaths.IconPath))
            appWindow.SetIcon(AppPaths.IconPath);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        TryTintTitleBar(appWindow);

        appWindow.Closing += (_, e) =>
        {
            if (!ViewModel.TryCloseToTray()) return;
            e.Cancel = true;
            appWindow.Hide();
        };
    }

    private static void TryTintTitleBar(AppWindow appWindow)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported()) return;
        var bar = appWindow.TitleBar;
        bar.BackgroundColor = ColorHelper.FromArgb(255, 0x14, 0x14, 0x17);
        bar.ForegroundColor = ColorHelper.FromArgb(255, 0xED, 0xED, 0xF0);
        bar.InactiveBackgroundColor = ColorHelper.FromArgb(255, 0x14, 0x14, 0x17);
        bar.InactiveForegroundColor = ColorHelper.FromArgb(255, 0x9B, 0x9B, 0xA6);
        bar.ButtonBackgroundColor = ColorHelper.FromArgb(255, 0x14, 0x14, 0x17);
        bar.ButtonForegroundColor = ColorHelper.FromArgb(255, 0xED, 0xED, 0xF0);
        bar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(255, 0x2C, 0x2C, 0x33);
        bar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(255, 0x37, 0x37, 0x3F);
    }
}
