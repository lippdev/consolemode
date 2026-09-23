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
    private readonly MenuFlyoutItem _showItem = new();
    private readonly MenuFlyoutItem _startItem = new();
    private readonly MenuFlyoutItem _restoreItem = new();
    private readonly MenuFlyoutItem _exitItem = new();

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
        _showItem.Text = LocalizationService.Get("ShowWindow");
        _showItem.Click += (_, _) => ShowWindow();
        _startItem.Text = LocalizationService.Get("TrayEnterConsole");
        _startItem.Click += async (_, _) =>
        {
            if (!await _vm.TryAutoStartAsync()) ShowWindow();
        };
        _restoreItem.Text = LocalizationService.Get("RestoreSetup");
        _restoreItem.Click += async (_, _) => await _vm.RestoreNowAsync();
        _exitItem.Text = LocalizationService.Get("Exit");
        _exitItem.Click += (_, _) =>
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
        menu.Items.Add(_showItem);
        menu.Items.Add(_startItem);
        menu.Items.Add(_restoreItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(_exitItem);
        _icon.ContextFlyout = menu;
        _icon.LeftClickCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowWindow);
        LocalizationService.LanguageChanged += OnLanguageChanged;
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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _showItem.Text = LocalizationService.Get("ShowWindow");
        _startItem.Text = LocalizationService.Get("TrayEnterConsole");
        _restoreItem.Text = LocalizationService.Get("RestoreSetup");
        _exitItem.Text = LocalizationService.Get("Exit");
    }

    public void Dispose()
    {
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        _icon.Dispose();
    }
}
