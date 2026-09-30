using System.ComponentModel;
using System.Numerics;
using Microsoft.UI.Xaml.Hosting;
using ConsoleMode.Services;
using ConsoleMode.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ConsoleMode.Views;

public sealed partial class ConsoleHomeView : UserControl
{
    public MainViewModel ViewModel { get; }

    private GamepadNavigator? _navigator;
    private bool _windowActive = true;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _clock;
    private static Style? _cardStyle;
    private int _shownTab;
    private static readonly Windows.UI.ViewManagement.UISettings Ui = new();

    public ConsoleHomeView()
    {
        ViewModel = App.ViewModel!;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => { _navigator?.Dispose(); _navigator = null; _clock?.Stop(); };
        // Preview (tunnelling): a ScrollViewer would otherwise swallow the arrows to scroll the page.
        Root.PreviewKeyDown += OnKeyDown;
        // The focused card grows a little, like the Xbox home.
        Root.GotFocus += (_, e) => ScaleTile(e.OriginalSource, 1.07f);
        Root.LostFocus += (_, e) => ScaleTile(e.OriginalSource, 1f);
        // Mouse clicks on buttons sound like a confirm (the pad has its own sounds in the navigator).
        Root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
        ViewModel.PropertyChanged += OnViewModelChanged;
        ViewModel.PickBackgroundImageRequested += () => _ = PickBackgroundImageAsync();
    }

    /// <summary>"My own image": the system file picker (needs the window handle in WinUI 3).</summary>
    private async Task PickBackgroundImageAsync()
    {
        try
        {
            if (App.MainWindowInstance is not { } window) return;
            var picker = new Windows.Storage.Pickers.FileOpenPicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary };
            foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" }) picker.FileTypeFilter.Add(extension);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            var file = await picker.PickSingleFileAsync();
            if (file is not null) ViewModel.SetBackgroundImage(file.Path);
            else if (!SteamArt.IsSupportedImage(ViewModel.ConsoleBackgroundImage)) ViewModel.CycleBackgroundMode();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Fundo: seletor de imagem: {ex.Message}");
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _navigator ??= new GamepadNavigator(DispatcherQueue, this,
            App.MainWindowInstance is { } w ? WinRT.Interop.WindowNative.GetWindowHandle(w) : 0);
        _navigator.SearchRoot = () =>
            ViewModel.IsControllerTestOpen ? ControllerTestPanel
            : ViewModel.IsPickerOpen ? PickerPanel
            : ViewModel.IsConsoleSettingsOpen ? SettingsPanel
            : ViewModel.IsRolePanelOpen ? RolePanel
            : Root;
        _navigator.Sounds = true;
        _navigator.BackRequested += GoBack;
        _navigator.TabRequested += StepTab;
        _navigator.MenuRequested += () => { if (ViewModel.CanStart && !ViewModel.IsRolePanelOpen && !ViewModel.IsConsoleSettingsOpen && !ViewModel.IsPickerOpen && !ViewModel.IsUpdateOpen) ViewModel.StartCommand.Execute(null); };
        _navigator.AltRequested += () =>
        {
            if (ViewModel.IsRolePanelOpen || ViewModel.IsPickerOpen || ViewModel.IsControllerTestOpen || ViewModel.IsUpdateOpen) return;
            if (ViewModel.IsConsoleSettingsOpen) ViewModel.CloseConsoleSettingsCommand.Execute(null);
            else ViewModel.OpenFullSettingsCommand.Execute(null);
        };
        if (App.MainWindowInstance is { } window)
            window.Activated += (_, args) =>
            {
                _windowActive = args.WindowActivationState != WindowActivationState.Deactivated;
                RefreshNavigator();
            };
        RefreshNavigator();
        StartClock();
        FocusDefault();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsConsoleUi):
            case nameof(MainViewModel.IsConsoleActive):
                RefreshNavigator();
                DispatcherQueue.TryEnqueue(FocusDefault);
                break;
            case nameof(MainViewModel.IsUpdateOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsUpdateOpen) UpdateNowButton.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsRolePanelOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsRolePanelOpen) RolePlay.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsPickerOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsPickerOpen) FocusPickerSelection();
                    else if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsControllerTestOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsControllerTestOpen) CopyDiagnosticsButton.Focus(FocusState.Keyboard);
                    else if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
            case nameof(MainViewModel.ConsoleTabIndex):
                DispatcherQueue.TryEnqueue(() =>
                {
                    // The new page was just made visible: lay it out first, or nothing in it can take the focus yet
                    // (the focus would stay on whatever is left visible, like a button in the top bar).
                    TabPanel(ViewModel.ConsoleTabIndex).UpdateLayout();
                    AnimateTab(_shownTab, ViewModel.ConsoleTabIndex);
                    _shownTab = ViewModel.ConsoleTabIndex;
                    FocusDefault();
                });
                break;
            case nameof(MainViewModel.IsConsoleSettingsOpen):
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsConsoleSettingsOpen) FirstSettingsRow.Focus(FocusState.Keyboard);
                    else FocusDefault();
                });
                break;
        }
    }

    /// <summary>
    /// Only listen while this interface is showing and the window is in front: A pressed inside
    /// Big Picture must not click something in a hidden window. (XInput reads without focus.)
    /// </summary>
    private void RefreshNavigator()
    {
        if (_navigator is null) return;
        // Not Visibility: this runs from the IsConsoleUi change, before the binding has updated it.
        var listen = ViewModel.IsConsoleUi && _windowActive;
        if (listen && !_navigator.IsRunning) _navigator.Start();
        else if (!listen && _navigator.IsRunning) _navigator.Stop();
    }

    private void FocusDefault()
    {
        if (!ViewModel.IsConsoleUi) return;
        if (ViewModel.IsConsoleActive) { RestoreButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsUpdateOpen) { UpdateNowButton.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsSystemTab) { FirstSettingsRow.Focus(FocusState.Keyboard); return; }
        if (ViewModel.IsSessionTab) { (FocusManager.FindFirstFocusableElement(ScreenCards) as Control)?.Focus(FocusState.Keyboard); return; }
        if (ViewModel.CanStart) { PlayButton.Focus(FocusState.Keyboard); return; }
        (FocusManager.FindFirstFocusableElement(ScreenCards) as Control)?.Focus(FocusState.Keyboard);
    }

    /// <summary>Land on the current value so A confirms it and the stick moves from there.</summary>
    private void FocusPickerSelection()
    {
        var index = Math.Max(ViewModel.PickerOptions.ToList().FindIndex(o => o.IsSelected), 0);
        if (TryFocusPickerRow(index)) return;
        // Containers appear on the next layout pass; try once more then.
        void OnLayout(object? s, object e)
        {
            PickerList.LayoutUpdated -= OnLayout;
            TryFocusPickerRow(index);
        }
        PickerList.LayoutUpdated += OnLayout;
    }

    private bool TryFocusPickerRow(int index)
    {
        if (PickerList.ContainerFromIndex(index) is not { } container) return false;
        return (FocusManager.FindFirstFocusableElement(container) as Control)?.Focus(FocusState.Keyboard) == true;
    }

    private FrameworkElement TabPanel(int tab) => tab switch
    {
        MainViewModel.HomeTabIndex => HomePanel,
        MainViewModel.SessionTabIndex => SessionPanel,
        _ => SettingsPanel
    };

    /// <summary>
    /// The page that just appeared slides in from the side the tab came from (RB: right, LB: left)
    /// while it fades in. Skipped when Windows has animations turned off.
    /// </summary>
    private void AnimateTab(int from, int to)
    {
        if (from == to || !Ui.AnimationsEnabled) return;
        var panel = TabPanel(to);
        try
        {
            ElementCompositionPreview.SetIsTranslationEnabled(panel, true);
            var visual = ElementCompositionPreview.GetElementVisual(panel);
            var compositor = visual.Compositor;
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

            var slide = compositor.CreateScalarKeyFrameAnimation();
            slide.InsertKeyFrame(0f, to > from ? 56f : -56f);
            slide.InsertKeyFrame(1f, 0f, easing);
            slide.Duration = TimeSpan.FromMilliseconds(260);
            visual.StartAnimation("Translation.X", slide);

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, 0f);
            fade.InsertKeyFrame(1f, 1f, easing);
            fade.Duration = TimeSpan.FromMilliseconds(220);
            visual.StartAnimation("Opacity", fade);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Abas: animação: {ex.Message}");
        }
    }

    /// <summary>LB / RB: the previous / next tab, unless a panel is open over the page.</summary>
    private void StepTab(int step)
    {
        if (ViewModel.IsConsoleActive || ViewModel.IsRolePanelOpen || ViewModel.IsPickerOpen || ViewModel.IsControllerTestOpen) return;
        if (ViewModel.StepConsoleTab(step)) UiSounds.Play(UiSound.Move);
    }

    private void GoBack()
    {
        if (ViewModel.IsControllerTestOpen) ViewModel.CloseControllerTestCommand.Execute(null);
        else if (ViewModel.IsPickerOpen) ViewModel.ClosePickerCommand.Execute(null);
        else if (ViewModel.IsRolePanelOpen) ViewModel.CloseRolePanelCommand.Execute(null);
        else if (!ViewModel.IsHomeTab) ViewModel.SelectConsoleTabCommand.Execute("0");
        else if (ViewModel.IsUpdateOpen && !ViewModel.IsUpdating) ViewModel.DismissUpdateCommand.Execute(null);
    }

    private void StartClock()
    {
        _clock ??= DispatcherQueue.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(15);
        _clock.Tick -= OnClockTick;
        _clock.Tick += OnClockTick;
        OnClockTick(null!, null!);
        _clock.Start();
    }

    private void OnClockTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args) =>
        ClockText.Text = DateTime.Now.ToString("HH:mm");

    /// <summary>Cards (not settings rows) grow when focused and shrink back when it leaves.</summary>
    private static void ScaleTile(object source, float scale)
    {
        if (source is not Button button) return;
        _cardStyle ??= Application.Current.Resources["ConsoleCard"] as Style;
        if (!ReferenceEquals(button.Style, _cardStyle)) return;
        button.CenterPoint = new Vector3((float)button.ActualWidth / 2, (float)button.ActualHeight / 2, 0);
        button.ScaleTransition ??= new Vector3Transition { Duration = TimeSpan.FromMilliseconds(140) };
        button.Scale = new Vector3(scale, scale, 1);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        for (var node = e.OriginalSource as DependencyObject; node is not null; node = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node))
        {
            if (node is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase { IsEnabled: true })
            {
                UiSounds.Play(UiSound.Confirm);
                return;
            }
        }
    }

    private static Microsoft.UI.Xaml.Input.FocusNavigationDirection? Arrow(VirtualKey key) => key switch
    {
        VirtualKey.Up => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Up,
        VirtualKey.Down => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Down,
        VirtualKey.Left => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Left,
        VirtualKey.Right => Microsoft.UI.Xaml.Input.FocusNavigationDirection.Right,
        _ => null
    };

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // The pad is read by the navigator (polling), never through these key events, or every press
        // would count twice. The keyboard goes through the same navigator, so both move the same way.
        if (e.Key is VirtualKey.Escape)
        {
            GoBack();
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.PageUp)
        {
            StepTab(-1);
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.PageDown)
        {
            StepTab(1);
            e.Handled = true;
        }
        else if (_navigator is not null && Arrow(e.Key) is { } direction)
        {
            _navigator.Navigate(direction);
            e.Handled = true;
        }
    }
}
