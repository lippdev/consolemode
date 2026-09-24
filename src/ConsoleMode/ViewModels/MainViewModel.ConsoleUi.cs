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

    /// <summary>Called after the config is applied and whenever the choice changes.</summary>
    private void ResolveUi()
    {
        var family = ControllerInput.DetectFamily();
        IsPlayStationHints = family == ControllerFamily.PlayStation;
        IsConsoleUi = UiModeResolver.IsConsole(SelectedUiMode?.Value, family != ControllerFamily.None, LaunchedByController);
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

    /// <summary>Console "Full settings": the desktop settings page, since it has everything.</summary>
    [RelayCommand]
    private void OpenFullSettings()
    {
        if (IsConsoleActive) return;
        SelectedUiMode = UiModeOptions.FirstOrDefault(o => o.Value == UiModeResolver.Desktop) ?? SelectedUiMode;
        OpenSettings();
    }

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
        }
    }

    private static ComboOption? Next(IList<ComboOption> options, ComboOption? current, int step)
    {
        var index = UiModeResolver.Cycle(options.Count, current is null ? -1 : options.IndexOf(current), step);
        return index < 0 ? current : options[index];
    }

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
