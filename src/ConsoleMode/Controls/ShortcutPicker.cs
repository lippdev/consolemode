using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ConsoleMode.Controls;

/// <summary>
/// One controller shortcut: what it is now, "Set" to press the buttons you want (they are read
/// live from the pad and recorded when let go) and "Remove". A combo another shortcut already
/// uses, or a single face button, is refused with the reason. Used in Settings and in the
/// first-run setup.
/// </summary>
public sealed class ShortcutPicker : StackPanel
{
    private readonly MainViewModel _vm;
    private readonly ShortcutSlot _slot;
    private readonly TextBlock _value = new() { VerticalAlignment = VerticalAlignment.Center, MinWidth = 120 };
    private readonly TextBlock _error = new()
    {
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 460,
        Foreground = new SolidColorBrush(Color.FromArgb(255, 232, 88, 88)),
        Visibility = Visibility.Collapsed
    };
    private readonly TextBlock _captureHint = new()
    {
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 460,
        Opacity = 0.7,
        Visibility = Visibility.Collapsed
    };
    private readonly Button _set = new();
    private readonly Button _clear = new();
    private readonly DispatcherQueueTimer _timer;
    private ShortcutCapture? _capture;

    /// <param name="stacked">In the setup dialog: left-aligned under the text instead of a right-hand column.</param>
    public ShortcutPicker(MainViewModel vm, ShortcutSlot slot, bool stacked = false, bool consoleStyle = false)
    {
        _vm = vm;
        _slot = slot;
        Spacing = 4;
        var side = stacked || consoleStyle ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        HorizontalAlignment = consoleStyle ? HorizontalAlignment.Stretch : side;

        if (consoleStyle)
        {
            _value.FontSize = 23;
            _value.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            _value.TextAlignment = TextAlignment.Center;
            _value.TextWrapping = TextWrapping.Wrap;
            _value.MinWidth = 0;
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Child = _value
            };
            Children.Add(badge);
            _set.Style = Application.Current.Resources["ConsoleSmallButton"] as Style;
            _clear.Style = Application.Current.Resources["ConsoleSmallButton"] as Style;
            _set.Height = 52;
            _clear.Height = 52;
            _set.FontSize = 18;
            _clear.FontSize = 18;
            _set.HorizontalAlignment = HorizontalAlignment.Stretch;
            _clear.HorizontalAlignment = HorizontalAlignment.Stretch;
            Children.Add(_set);
            Children.Add(_clear);
        }
        else
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = side };
            row.Children.Add(_value);
            row.Children.Add(_set);
            row.Children.Add(_clear);
            Children.Add(row);
        }
        Children.Add(_captureHint);
        Children.Add(_error);
        _captureHint.MaxWidth = consoleStyle ? 280 : 460;
        _error.MaxWidth = consoleStyle ? 280 : 460;
        if (consoleStyle)
        {
            _captureHint.FontSize = 18;
            _error.FontSize = 18;
        }

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(40);
        _timer.Tick += (_, _) => Poll();
        _set.Click += (_, _) => { if (_capture is null) StartCapture(); else StopCapture(); };
        _clear.Click += (_, _) => { _error.Visibility = Visibility.Collapsed; _vm.ClearShortcut(_slot); };

        // Subscribed for good, not on Loaded/Unloaded: inside a ContentDialog those events can
        // leave the picker unsubscribed and its text stale. The pickers are few and live as long
        // as their page or dialog, so there is nothing to leak of note.
        _vm.PropertyChanged += OnViewModelChanged;
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Unloaded += (_, _) => StopCapture();
        Refresh();
    }

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == MainViewModel.ShortcutsProperty) Refresh();
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(Refresh);

    private void Refresh()
    {
        var isSet = _vm.GetShortcut(_slot) != 0;
        if (_capture is null) _value.Text = _vm.ShortcutText(_slot);
        _set.Content = LocalizationService.Get(_capture is not null ? "ShortcutCancel" : isSet ? "ShortcutChange" : "ShortcutSet");
        _clear.Content = LocalizationService.Get("ShortcutClear");
        _clear.Visibility = isSet && _capture is null ? Visibility.Visible : Visibility.Collapsed;
        _value.Opacity = isSet || _capture is not null ? 1 : 0.6;
        _captureHint.Text = LocalizationService.Get("ShortcutCaptureHint", _vm.HintBack);
        _captureHint.Visibility = _capture is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void StartCapture()
    {
        _error.Visibility = Visibility.Collapsed;
        _capture = new ShortcutCapture();
        SonyHidReader.Acquire();
        _vm.BeginShortcutCapture();
        _value.Text = LocalizationService.Get("ShortcutPressButtons");
        Refresh();
        _timer.Start();
    }

    private void StopCapture()
    {
        if (_capture is null) return;
        _capture = null;
        _timer.Stop();
        SonyHidReader.Release();
        _vm.EndShortcutCapture();
        Refresh();
    }

    /// <summary>Stops the active capture when its containing console overlay is dismissed.</summary>
    public void CancelCapture() => StopCapture();

    /// <summary>Moves controller focus to this shortcut's Set/Change/Cancel action.</summary>
    public bool FocusCaptureButton() => _set.Focus(FocusState.Keyboard);

    private void Poll()
    {
        if (_capture is null) return;
        var held = ControllerHoldWatcher.ReadHeldByDevice();
        var captured = _capture.Feed(held);
        var live = ControllerShortcuts.Format(_capture.CurrentHeld, _vm.IsPlayStationHints);
        _value.Text = live.Length > 0 ? live : LocalizationService.Get("ShortcutPressButtons");
        if (captured is not { } mask) return;

        StopCapture();
        // A lone B/Circle is never a valid shortcut, so use its release to cancel capture.
        // Combinations containing B still reach TrySetShortcut unchanged.
        if (mask == ControllerShortcuts.B) return;
        var error = _vm.TrySetShortcut(_slot, mask);
        _error.Text = error ?? "";
        _error.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// First run of the version that made shortcuts configurable: nothing is set, and the user
    /// chooses each one here (or takes the suggestions), or skips and does it later in Settings.
    /// </summary>
    public static async Task ShowOnboardingAsync(XamlRoot root, MainViewModel vm)
    {
        var panel = new StackPanel { Spacing = 16 };
        panel.Children.Add(new TextBlock
        {
            Text = LocalizationService.Get("ShortcutsOnboardingBody"),
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var (slot, descriptionKey) in new[]
                 {
                     (ShortcutSlot.Home, "HomeButtonDescription"),
                     (ShortcutSlot.Menu, "SessionMenuDescription"),
                     (ShortcutSlot.Exit, "ExitShortcutDescription")
                 })
        {
            var text = new StackPanel { Spacing = 2 };
            text.Children.Add(new TextBlock { Text = vm.ShortcutName(slot), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            text.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get(descriptionKey),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.7
            });
            // Stacked: the dialog is narrow, a right-hand column would be clipped.
            var row = new StackPanel { Spacing = 8 };
            row.Children.Add(text);
            row.Children.Add(new ShortcutPicker(vm, slot, stacked: true));
            panel.Children.Add(row);
        }

        var suggest = new Button { Content = LocalizationService.Get("ShortcutsUseSuggested") };
        suggest.Click += (_, _) => vm.UseSuggestedShortcuts();
        panel.Children.Add(suggest);

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = LocalizationService.Get("ShortcutsOnboardingTitle"),
            Content = new ScrollViewer { Content = panel },
            CloseButtonText = LocalizationService.Get("ShortcutsOnboardingDone"),
            DefaultButton = ContentDialogButton.Close
        };
        try
        {
            await dialog.ShowAsync();
        }
        finally
        {
            vm.CompleteShortcutOnboarding();
        }
    }
}
