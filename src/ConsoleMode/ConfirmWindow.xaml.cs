using ConsoleMode.Native;
using ConsoleMode.Models;
using ConsoleMode.Services;
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
    private readonly ControllerInput _controller;
    private int _secondsLeft;

    public Task<bool> Result => _result.Task;
    public LocalizedStrings Texts => LocalizationService.Texts;

    /// <summary>How the prompt was answered, for the log and the test result.</summary>
    public string AnsweredBy { get; private set; } = "tempo esgotado";

    public ConfirmWindow(ScreenRect? target, int seconds)
    {
        InitializeComponent();
        _secondsLeft = seconds;
        CountdownBar.Maximum = seconds;
        UpdateCountdown();
        LocalizationService.LanguageChanged += OnLanguageChanged;

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
        WindowPlacement.CenterOn(appWindow, target, WidthDip, HeightDip);

        ShowControllerHints(ControllerInput.DetectFamily());
        _controller = new ControllerInput(DispatcherQueue);
        _controller.Pressed += action => Finish(action == ControllerAction.Confirm, "controle");
        _controller.Start();
        Root.KeyDown += (_, e) =>
        {
            if (e.Key != Windows.System.VirtualKey.Escape) return;
            e.Handled = true;
            Finish(false, "teclado");
        };

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            _secondsLeft--;
            UpdateCountdown();
            if (_secondsLeft <= 0) Finish(false, "tempo esgotado");
        };
        _timer.Start();

        Activated += (_, _) => KeepButton.Focus(FocusState.Programmatic);
        Closed += (_, _) =>
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            _timer.Stop();
            _controller.Dispose();
            _result.TrySetResult(false);
        };
    }

    private void ShowControllerHints(ControllerFamily family)
    {
        var xbox = family is ControllerFamily.Xbox or ControllerFamily.Other ? Visibility.Visible : Visibility.Collapsed;
        var ps = family == ControllerFamily.PlayStation ? Visibility.Visible : Visibility.Collapsed;
        KeepXboxGlyph.Visibility = BackXboxGlyph.Visibility = xbox;
        KeepPsGlyph.Visibility = BackPsGlyph.Visibility = ps;
    }

    private void OnKeep(object sender, RoutedEventArgs e) => Finish(true, "mouse/teclado");

    private void OnRevert(object sender, RoutedEventArgs e) => Finish(false, "mouse/teclado");

    private void Finish(bool keep, string answeredBy)
    {
        if (_result.Task.IsCompleted) return;
        _timer.Stop();
        _controller.Stop();
        AnsweredBy = answeredBy;
        _result.TrySetResult(keep);
        Close();
    }

    private void UpdateCountdown()
    {
        CountdownBar.Value = _secondsLeft;
        CountdownText.Text = LocalizationService.Get("Countdown", _secondsLeft);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) =>
        DispatcherQueue.TryEnqueue(UpdateCountdown);
}
