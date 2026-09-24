using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The console (10-foot) interface: which interface is showing, the quick-setting cards
// that cycle their value on A, and the role panel for a screen.
public partial class MainViewModel
{
    public ObservableCollection<ComboOption> UiModeOptions { get; } = [];

    [ObservableProperty] private bool _isConsoleUi;
    [ObservableProperty] private ComboOption? _selectedUiMode;
    [ObservableProperty] private bool _isRolePanelOpen;
    [ObservableProperty] private MonitorRowViewModel? _rolePanelMonitor;
    [ObservableProperty] private bool _isPlayStationHints;

    /// <summary>Set by App when the session was requested from the controller's Home button.</summary>
    public bool LaunchedByController { get; set; }

    public bool IsDesktopHome => !IsConsoleUi && IsHomePage;
    public bool IsDesktopSettings => !IsConsoleUi && IsSettingsPage;
    public string FocusScreenName => FocusRow?.Name ?? "";
    public string RolePanelTitle => LocalizationService.Get("ChooseRoleFor", RolePanelMonitor?.Name ?? "");
    public string HdrText => LocalizationService.Get(HdrEnable ? "ToggleOn" : "ToggleOff");
    public string VrrText => LocalizationService.Get(VrrEnable ? "ToggleOn" : "ToggleOff");
    public string HintConfirm => IsPlayStationHints ? "✕" : "A";
    public string HintBack => IsPlayStationHints ? "○" : "B";
    public string HintAlt => IsPlayStationHints ? "△" : "Y";
    public string HintMenu => IsPlayStationHints ? "OPTIONS" : "☰";

    partial void OnIsConsoleUiChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDesktopHome));
        OnPropertyChanged(nameof(IsDesktopSettings));
        if (value) IsRolePanelOpen = false;
        else { IsConsoleSettingsOpen = false; IsPickerOpen = false; }
    }

    partial void OnIsHomePageChanged(bool value) => OnPropertyChanged(nameof(IsDesktopHome));
    partial void OnIsSettingsPageChanged(bool value) => OnPropertyChanged(nameof(IsDesktopSettings));
    partial void OnRolePanelMonitorChanged(MonitorRowViewModel? value) => OnPropertyChanged(nameof(RolePanelTitle));
    partial void OnIsPlayStationHintsChanged(bool value)
    {
        OnPropertyChanged(nameof(HintConfirm));
        OnPropertyChanged(nameof(HintBack));
        OnPropertyChanged(nameof(HintAlt));
        OnPropertyChanged(nameof(HintMenu));
    }

    partial void OnSelectedUiModeChanged(ComboOption? value)
    {
        if (_applying || value is null) return;
        SaveQuietly();
        ResolveUi();
    }

    private void BuildUiModeOptions()
    {
        var current = SelectedUiMode?.Value ?? _loadedConfig.UiMode;
        UiModeOptions.Clear();
        UiModeOptions.Add(new ComboOption { Text = LocalizationService.Get("UiModeAuto"), Value = UiModeResolver.Auto });
        UiModeOptions.Add(new ComboOption { Text = LocalizationService.Get("UiModeDesktop"), Value = UiModeResolver.Desktop });
        UiModeOptions.Add(new ComboOption { Text = LocalizationService.Get("UiModeConsole"), Value = UiModeResolver.Console });
        SelectedUiMode = UiModeOptions.FirstOrDefault(o => o.Value == current) ?? UiModeOptions[0];
    }

    [ObservableProperty] private bool _controllerDetected;

    /// <summary>Called after the config is applied and whenever the choice changes.</summary>
    private void ResolveUi()
    {
        var family = ControllerInput.DetectFamily();
        IsPlayStationHints = family == ControllerFamily.PlayStation;
        ControllerDetected = family != ControllerFamily.None;
        IsConsoleUi = UiModeResolver.IsConsole(SelectedUiMode?.Value, ControllerDetected, LaunchedByController);
    }

    /// <summary>
    /// A pad appeared or went away. Windows lists HID pads asynchronously, so the startup check
    /// often misses them; re-resolving here is what makes "Automatic" pick the console interface.
    /// Never flips the interface during a session.
    /// </summary>
    public void OnControllerPresenceChanged()
    {
        if (IsConsoleActive) return;
        ResolveUi();
    }

    /// <summary>Startup safety net for pads that enumerate late (Bluetooth, DualSense).</summary>
    private async Task RecheckControllerAsync()
    {
        foreach (var delay in new[] { 1500, 4000 })
        {
            await Task.Delay(delay);
            if (ControllerDetected) return;
            OnControllerPresenceChanged();
        }
    }

    private void RefreshConsoleTexts()
    {
        OnPropertyChanged(nameof(FocusScreenName));
        OnPropertyChanged(nameof(RolePanelTitle));
        OnPropertyChanged(nameof(HdrText));
        OnPropertyChanged(nameof(VrrText));
    }

    /// <summary>"desktop" / "console": the on-screen switch, remembered as an explicit choice.</summary>
    [RelayCommand]
    private void SwitchUi(string mode)
    {
        if (IsConsoleActive && mode == UiModeResolver.Desktop) return;
        SelectedUiMode = UiModeOptions.FirstOrDefault(o => o.Value == mode) ?? SelectedUiMode;
        if (mode == UiModeResolver.Desktop) ShowPage(settings: false);
    }

    [ObservableProperty] private bool _isConsoleSettingsOpen;

    /// <summary>x:Bind helper for on/off rows.</summary>
    public string OnOff(bool value) => LocalizationService.Get(value ? "ToggleOn" : "ToggleOff");

    /// <summary>Console "All settings": the console-styled settings list.</summary>
    [RelayCommand]
    private void OpenFullSettings()
    {
        if (IsConsoleActive) return;
        IsRolePanelOpen = false;
        IsConsoleSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseConsoleSettings() => IsConsoleSettingsOpen = false;

    /// <summary>"launch:+1", "audio:-1", "fps:+1", "mode:+1": cycle a quick-setting card.</summary>
    [RelayCommand]
    private void CycleSetting(string spec)
    {
        var parts = spec.Split(':');
        var step = parts.Length > 1 && parts[1] == "-1" ? -1 : 1;
        switch (parts[0])
        {
            case "launch": SelectedLaunch = Next(LaunchOptions, SelectedLaunch, step); break;
            case "audio": SelectedAudio = Next(AudioOptions, SelectedAudio, step); break;
            case "fps":
                // Custom FPS needs a keyboard; the console cards skip it.
                var presets = FpsOptions.Where(o => o.Value != FpsCustomValue.ToString()).ToList();
                SelectedFps = Next(presets, SelectedFps, step);
                break;
            case "mode":
                if (FocusRow is null) return;
                var index = UiModeResolver.Cycle(FocusRow.Modes.Count, FocusRow.Modes.ToList().IndexOf(FocusRow.SelectedMode), step);
                if (index >= 0) FocusRow.SelectedMode = FocusRow.Modes[index];
                SaveQuietly();
                break;
            case "hdr": HdrEnable = !HdrEnable; break;
            case "vrr": VrrEnable = !VrrEnable; break;
            case "hide": SelectedHideStrategy = Next(HideStrategies, SelectedHideStrategy, step); break;
            case "language": SelectedLanguage = Next(LanguageOptions, SelectedLanguage, step); break;
            case "ui": SelectedUiMode = Next(UiModeOptions, SelectedUiMode, step); break;
            case "home": HomeButtonLaunch = !HomeButtonLaunch; break;
            case "shortpress": HomeButtonShortPress = !HomeButtonShortPress; break;
            case "autostart": AutoStartOnController = !AutoStartOnController; break;
            case "startup": StartWithWindows = !StartWithWindows; break;
            case "updates": CheckUpdates = !CheckUpdates; break;
        }
    }

    private static ComboOption? Next(IList<ComboOption> options, ComboOption? current, int step)
    {
        var index = UiModeResolver.Cycle(options.Count, current is null ? -1 : options.IndexOf(current), step);
        return index < 0 ? current : options[index];
    }

    // ── Option picker (modal list): A on a card opens it, A on a row picks, B closes ──


    public ObservableCollection<PickerItem> PickerOptions { get; } = [];

    [ObservableProperty] private bool _isPickerOpen;
    [ObservableProperty] private string _pickerTitle = "";
    private string _pickerKey = "";

    /// <summary>"launch", "audio", "mode", "fps", "hide", "language", "ui".</summary>
    [RelayCommand]
    private void OpenPicker(string key)
    {
        if (IsConsoleActive) return;
        var (title, options, selected) = key switch
        {
            "launch" => (LocalizationService.Get("LaunchCard"), LaunchOptions.Select(o => (o.Text, o.Value)), SelectedLaunch?.Value),
            "audio" => (LocalizationService.Get("AudioOutputCard"), AudioOptions.Select(o => (o.Text, o.Value)), SelectedAudio?.Value),
            "fps" => (LocalizationService.Get("FpsCard"), FpsOptions.Where(o => o.Value != FpsCustomValue.ToString()).Select(o => (o.Text, o.Value)), SelectedFps?.Value),
            "hide" => (LocalizationService.Get("HideOtherScreensCard"), HideStrategies.Select(o => (o.Text, o.Value)), SelectedHideStrategy?.Value),
            "language" => (LocalizationService.Get("LanguageCard"), LanguageOptions.Select(o => (o.Text, o.Value)), SelectedLanguage?.Value),
            "ui" => (LocalizationService.Get("UiModeCard"), UiModeOptions.Select(o => (o.Text, o.Value)), SelectedUiMode?.Value),
            "mode" when FocusRow is not null => (LocalizationService.Get("ResolutionRefreshCard"), FocusRow.Modes.Select(m => (m.Text, m.Key)), FocusRow.SelectedMode?.Key),
            _ => (null, null, null)
        };
        if (title is null || options is null) return;

        _pickerKey = key;
        PickerTitle = title;
        PickerOptions.Clear();
        foreach (var (text, value) in options)
            PickerOptions.Add(new PickerItem(text, value, value == selected));
        IsRolePanelOpen = false;
        IsPickerOpen = true;
    }

    [RelayCommand]
    private void PickOption(PickerItem? item)
    {
        if (item is null) return;
        switch (_pickerKey)
        {
            case "launch": SelectedLaunch = LaunchOptions.FirstOrDefault(o => o.Value == item.Value) ?? SelectedLaunch; break;
            case "audio": SelectedAudio = AudioOptions.FirstOrDefault(o => o.Value == item.Value) ?? SelectedAudio; break;
            case "fps": SelectedFps = FpsOptions.FirstOrDefault(o => o.Value == item.Value) ?? SelectedFps; break;
            case "hide": SelectedHideStrategy = HideStrategies.FirstOrDefault(o => o.Value == item.Value) ?? SelectedHideStrategy; break;
            case "language": SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Value == item.Value) ?? SelectedLanguage; break;
            case "ui": SelectedUiMode = UiModeOptions.FirstOrDefault(o => o.Value == item.Value) ?? SelectedUiMode; break;
            case "mode":
                if (FocusRow?.Modes.FirstOrDefault(m => m.Key == item.Value) is { } mode)
                {
                    FocusRow.SelectedMode = mode;
                    SaveQuietly();
                }
                break;
        }
        IsPickerOpen = false;
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    [RelayCommand]
    private void OpenRolePanel(MonitorRowViewModel? row)
    {
        if (row is null || IsConsoleActive) return;
        RolePanelMonitor = row;
        IsRolePanelOpen = true;
    }

    /// <summary>"0" play here, "1" turn off, "2" keep on.</summary>
    [RelayCommand]
    private void SetRole(string index)
    {
        if (RolePanelMonitor is not null && int.TryParse(index, out var role)) RolePanelMonitor.RoleIndex = role;
        IsRolePanelOpen = false;
    }

    [RelayCommand]
    private void CloseRolePanel() => IsRolePanelOpen = false;
}

/// <summary>One row of the console option picker.</summary>
public sealed record PickerItem(string Text, string Value, bool IsSelected);
