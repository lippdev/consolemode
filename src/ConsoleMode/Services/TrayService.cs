using ConsoleMode.ViewModels;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.Services;

public sealed class TrayService : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly Window _window;
    private readonly MainViewModel _vm;

    public TrayService(Window window, MainViewModel vm)
    {
        _window = window;
        _vm = vm;
        _icon = new TaskbarIcon
        {
            ToolTipText = "Console Mode"
        };

        var iconPath = AppPaths.IconPath;
        if (File.Exists(iconPath))
            _icon.IconSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

        var menu = new MenuFlyout();
        var show = new MenuFlyoutItem { Text = "Mostrar janela" };
        show.Click += (_, _) => ShowWindow();
        var restore = new MenuFlyoutItem { Text = "Restaurar setup" };
        restore.Click += async (_, _) => await _vm.RestoreNowAsync();
        var exit = new MenuFlyoutItem { Text = "Sair" };
        exit.Click += (_, _) =>
        {
            if (_vm.IsConsoleActive)
            {
                _ = _vm.RestoreNowAsync().ContinueWith(_ =>
                {
                    window.DispatcherQueue.TryEnqueue(Application.Current.Exit);
                });
            }
            else
            {
                Application.Current.Exit();
            }
        };
        menu.Items.Add(show);
        menu.Items.Add(restore);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exit);
        _icon.ContextFlyout = menu;
        _icon.LeftClickCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowWindow);
        try
        {
            _icon.ForceCreate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Bandeja: {ex.Message}");
        }
    }

    public void ShowWindow()
    {
        _window.DispatcherQueue.TryEnqueue(() =>
        {
            _window.Activate();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Native.NativeWindows.ShowWindow(hwnd, 9);
        });
    }

    public void Dispose() => _icon.Dispose();
}
