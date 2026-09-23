using System.Runtime.InteropServices;
using ConsoleMode.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ConsoleMode;

/// <summary>
/// "Está vendo esta tela?" on the game screen, like Windows' "Keep display settings?".
/// Result is false on timeout, on "Voltar ao normal" or if the window is closed.
/// </summary>
public sealed partial class ConfirmWindow : Window
{
    private const double WidthDip = 520;
    private const double HeightDip = 260;

    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherQueueTimer _timer;
    private int _secondsLeft;

    public Task<bool> Result => _result.Task;

    public ConfirmWindow(ScreenRect? target, int seconds)
    {
        InitializeComponent();
        _secondsLeft = seconds;
        CountdownBar.Maximum = seconds;
        UpdateCountdown();

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }
        CenterOn(appWindow, target);

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            _secondsLeft--;
            UpdateCountdown();
            if (_secondsLeft <= 0) Finish(false);
        };
        _timer.Start();

        Activated += (_, _) => KeepButton.Focus(FocusState.Programmatic);
        Closed += (_, _) =>
        {
            _timer.Stop();
            _result.TrySetResult(false);
        };
    }

    private void OnKeep(object sender, RoutedEventArgs e) => Finish(true);

    private void OnRevert(object sender, RoutedEventArgs e) => Finish(false);

    private void Finish(bool keep)
    {
        _timer.Stop();
        _result.TrySetResult(keep);
        Close();
    }

    private void UpdateCountdown()
    {
        CountdownBar.Value = _secondsLeft;
        CountdownText.Text = $"Voltando ao normal em {_secondsLeft} s";
    }

    private static void CenterOn(AppWindow appWindow, ScreenRect? target)
    {
        if (target is null || target.Width <= 0 || target.Height <= 0)
        {
            appWindow.Resize(new Windows.Graphics.SizeInt32((int)WidthDip, (int)HeightDip));
            return;
        }

        // Size in the game screen's DPI (a 4K TV is usually scaled 150-300%).
        var cx = target.X + target.Width / 2;
        var cy = target.Y + target.Height / 2;
        var scale = DpiAt(cx, cy);
        var w = (int)(WidthDip * scale);
        var h = (int)(HeightDip * scale);
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(cx - w / 2, cy - h / 2, w, h));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    private static double DpiAt(int x, int y)
    {
        try
        {
            var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, 2 /* MONITOR_DEFAULTTONEAREST */);
            return GetDpiForMonitor(monitor, 0, out var dpi, out _) == 0 && dpi > 0 ? dpi / 96.0 : 1.0;
        }
        catch
        {
            return 1.0;
        }
    }
}
