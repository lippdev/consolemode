using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.ViewModels;

// Update notices (GitHub Releases) and "start with Windows".
public partial class MainViewModel
{
    private UpdateInfo? _update;
    private UpdateStatusKind _updateStatusKind;

    private enum UpdateStatusKind { InstallKind, Searching, Latest, Available, Failed }

    [ObservableProperty] private bool _checkUpdates = true;
    [ObservableProperty] private bool _homeButtonLaunch = true;
    [ObservableProperty] private bool _homeButtonShortPress;
    [ObservableProperty] private bool _autoStartOnController;
    [ObservableProperty] private string _homeButtonHint = "";
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _isUpdateOpen;
    [ObservableProperty] private string _updateTitle = "";
    [ObservableProperty] private string _updateMessage = "";
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private double _updateProgress;
    [ObservableProperty] private bool _isCheckingUpdates;
    [ObservableProperty] private string _updateStatusText = "";

    public string InstallKindText => LocalizationService.Get(
        AppPaths.IsInstalled ? "InstallKindInstalled" : "InstallKindPortable", UpdateService.CurrentVersion);

    public string UpdateActionText => LocalizationService.Get(_update?.AssetUrl is null ? "Download" : "UpdateNow");

    private void InitializeAppSettings()
    {
        StartWithWindows = StartupService.IsEnabled;
        _updateStatusKind = UpdateStatusKind.InstallKind;
        RefreshUpdateStatusText();
    }

    private void RefreshUpdateStatusText()
    {
        UpdateStatusText = _updateStatusKind switch
        {
            UpdateStatusKind.InstallKind => InstallKindText,
            UpdateStatusKind.Searching => LocalizationService.Get("SearchingUpdates"),
            UpdateStatusKind.Latest => LocalizationService.Get("LatestVersionStatus", InstallKindText),
            UpdateStatusKind.Available => LocalizationService.Get("UpdateAvailableStatus", InstallKindText, _update?.Version ?? ""),
            UpdateStatusKind.Failed => LocalizationService.Get("UpdateCheckFailedStatus", InstallKindText),
            _ => InstallKindText
        };
    }

    private void RefreshUpdateStrings()
    {
        RefreshUpdateStatusText();
        if (_update is null) return;
        UpdateTitle = LocalizationService.Get("UpdateAvailableTitle", _update.Version,
            _update.IsPrerelease ? LocalizationService.Get("BetaSuffix") : "");
        UpdateMessage = FirstNoteLine(_update.Notes) ?? LocalizationService.Get("NewVersionOnGithub");
        OnPropertyChanged(nameof(UpdateActionText));
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_applying || value == StartupService.IsEnabled) return;
        try
        {
            StartupService.SetEnabled(value);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Iniciar com o Windows: {ex}");
            SetStatus(LocalizationService.Get("StartupSettingError", ex.Message), InfoBarSeverity.Error);
        }
    }

    partial void OnCheckUpdatesChanged(bool value) => SaveQuietly();

    partial void OnHomeButtonLaunchChanged(bool value) => SaveQuietly();

    partial void OnAutoStartOnControllerChanged(bool value) => SaveQuietly();

    partial void OnHomeButtonShortPressChanged(bool value)
    {
        if (_applying) return;
        try
        {
            GameBarService.SetGuideOpensGameBar(!value);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Game Bar: {ex}");
            SetStatus(LocalizationService.Get("GameBarSettingError", ex.Message), InfoBarSeverity.Error);
        }
        SaveQuietly();
        RefreshHomeButtonHint();
    }

    /// <summary>Steam's desktop client also grabs the Guide button; only the user can change that.</summary>
    private void RefreshHomeButtonHint()
    {
        HomeButtonHint = HomeButtonShortPress && GameBarService.IsSteamRunning()
            ? LocalizationService.Get("HomeButtonSteamHint")
            : LocalizationService.Get("HomeButtonShortPressDescription");
    }

    /// <summary>Startup check: quiet on failure, respects "ignorar esta versão".</summary>
    private async Task CheckForUpdatesOnStartupAsync()
    {
        if (!CheckUpdates) return;
        await CheckForUpdatesAsync(manual: false);
    }

    [RelayCommand]
    private Task CheckNowAsync() => CheckForUpdatesAsync(manual: true);

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (IsCheckingUpdates) return;
        IsCheckingUpdates = true;
        if (manual)
        {
            _updateStatusKind = UpdateStatusKind.Searching;
            RefreshUpdateStatusText();
        }
        try
        {
            var update = await UpdateService.CheckAsync();
            if (update is null)
            {
                _updateStatusKind = UpdateStatusKind.Latest;
                RefreshUpdateStatusText();
                if (manual) SetStatus(LocalizationService.Get("AppUpToDate"), InfoBarSeverity.Success);
                return;
            }

            AppLog.Write($"Atualização disponível: {update.Version} ({update.AssetName ?? "sem arquivo"})");
            _updateStatusKind = UpdateStatusKind.Available;
            RefreshUpdateStatusText();
            if (!manual && string.Equals(update.Version, _loadedConfig.SkippedUpdateVersion, StringComparison.OrdinalIgnoreCase))
                return;

            _update = update;
            UpdateTitle = LocalizationService.Get("UpdateAvailableTitle", update.Version,
                update.IsPrerelease ? LocalizationService.Get("BetaSuffix") : "");
            UpdateMessage = FirstNoteLine(update.Notes) ?? LocalizationService.Get("NewVersionOnGithub");
            OnPropertyChanged(nameof(UpdateActionText));
            IsUpdateOpen = true;
            if (manual) ShowPage(settings: false);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Atualizações: {ex.Message}");
            _updateStatusKind = UpdateStatusKind.Failed;
            RefreshUpdateStatusText();
            if (manual) SetStatus(LocalizationService.Get("UpdateSearchFailure", ex.Message), InfoBarSeverity.Warning);
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        if (_update is null || IsUpdating) return;
        if (IsConsoleActive)
        {
            // The app restarts during an update; never leave the desk screens off.
            SetStatus(LocalizationService.Get("ExitConsoleBeforeUpdate"), InfoBarSeverity.Warning);
            return;
        }
        if (_update.AssetUrl is null)
        {
            OpenReleasePage();
            return;
        }

        IsUpdating = true;
        UpdateProgress = 0;
        try
        {
            var progress = new Progress<double>(p => UpdateProgress = p * 100);
            await UpdateService.ApplyAsync(_update, progress);
            AppLog.Write($"Atualização {_update.Version}: saindo para instalar");
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            AppLog.Write($"Atualização: {ex}");
            SetStatus(LocalizationService.Get("UpdateFailure", ex.Message), InfoBarSeverity.Error);
            IsUpdating = false;
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = _update?.PageUrl ?? $"https://github.com/{UpdateService.Repository}/releases";
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { SetStatus(LocalizationService.Get("BrowserOpenFailure", ex.Message), InfoBarSeverity.Error); }
    }

    [RelayCommand]
    private void SkipUpdate()
    {
        if (_update is null) return;
        var config = BuildConfig();
        config.SkippedUpdateVersion = _update.Version;
        TrySave(config);
        IsUpdateOpen = false;
    }

    /// <summary>
    /// First item of the release notes in the interface language. Releases without notes in
    /// that language (pt-BR-only ones before 1.5.0) get the generic message instead.
    /// </summary>
    private static string? FirstNoteLine(string notes) =>
        ReleaseNotes.FirstLine(notes, LocalizationService.Language);
}
