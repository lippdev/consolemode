using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace ConsoleMode;

/// <summary>
/// A toast in the top-right corner of the game screen, shown once when the session starts:
/// "hold Select + Y to open the panel". It never takes the focus from the game (shown without
/// activation, WS_EX_NOACTIVATE) and closes by itself.
/// </summary>
public sealed class SessionHintWindow : Window
{
    private const double WidthDip = 470;
    private const double HeightDip = 96;
    private const double MarginDip = 28;

    public SessionHintWindow(string title, string body, ScreenRect? target, TimeSpan duration)
    {
        var icon = new FontIcon { Glyph = "", FontSize = 30, VerticalAlignment = VerticalAlignment.Center };
        var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        text.Children.Add(new TextBlock { Text = body, FontSize = 15, TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
        var row = new Grid { ColumnSpacing = 16 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(text, 1);
        row.Children.Add(icon);
        row.Children.Add(text);
        Content = new Border
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(0xF2, 0x0F, 0x14, 0x18)),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20, 12, 20, 12),
            RequestedTheme = ElementTheme.Dark,
            Child = row
        };

        var hwnd = WindowNative.GetWindowHandle(this);
        // Tool window (no taskbar button) that never activates.
        SetWindowLongPtr(hwnd, GwlExStyle, GetWindowLongPtr(hwnd, GwlExStyle) | WsExNoActivate | WsExToolWindow);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        WindowPlacement.TopRightOn(appWindow, target, WidthDip, HeightDip, MarginDip);
        appWindow.Show(activateWindow: false);

        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = duration;
        timer.IsRepeating = false;
        timer.Tick += (_, _) => Close();
        timer.Start();
    }

    private const int GwlExStyle = -20;
    private const nint WsExNoActivate = 0x08000000;
    private const nint WsExToolWindow = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
}
