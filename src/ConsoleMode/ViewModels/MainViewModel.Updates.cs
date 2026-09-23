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

    [ObservableProperty] private bool _checkUpdates = true;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _isUpdateOpen;
    [ObservableProperty] private string _updateTitle = "";
    [ObservableProperty] private string _updateMessage = "";
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private double _updateProgress;
    [ObservableProperty] private bool _isCheckingUpdates;
    [ObservableProperty] private string _updateStatusText = "";

    public string InstallKindText => AppPaths.IsInstalled
        ? $"Versão {UpdateService.CurrentVersion} · instalada"
        : $"Versão {UpdateService.CurrentVersion} · portátil";

    public string UpdateActionText => _update?.AssetUrl is null ? "Baixar" : "Atualizar agora";

    private void InitializeAppSettings()
    {
        StartWithWindows = StartupService.IsEnabled;
        UpdateStatusText = InstallKindText;
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
            SetStatus($"Não foi possível mudar a inicialização com o Windows: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    partial void OnCheckUpdatesChanged(bool value) => SaveQuietly();

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
        if (manual) UpdateStatusText = "Procurando atualizações…";
        try
        {
            var update = await UpdateService.CheckAsync();
            if (update is null)
            {
                UpdateStatusText = $"{InstallKindText} · você está na versão mais recente";
                if (manual) SetStatus("O Console Mode está atualizado.", InfoBarSeverity.Success);
                return;
            }

            AppLog.Write($"Atualização disponível: {update.Version} ({update.AssetName ?? "sem arquivo"})");
            UpdateStatusText = $"{InstallKindText} · {update.Version} disponível";
            if (!manual && string.Equals(update.Version, _loadedConfig.SkippedUpdateVersion, StringComparison.OrdinalIgnoreCase))
                return;

            _update = update;
            UpdateTitle = $"Console Mode {update.Version} disponível{(update.IsPrerelease ? " (beta)" : "")}";
            UpdateMessage = FirstNoteLine(update.Notes) ?? "Tem uma versão nova no GitHub.";
            OnPropertyChanged(nameof(UpdateActionText));
            IsUpdateOpen = true;
            if (manual) ShowPage(settings: false);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Atualizações: {ex.Message}");
            UpdateStatusText = $"{InstallKindText} · não foi possível verificar agora";
            if (manual) SetStatus($"Não foi possível procurar atualizações: {ex.Message}", InfoBarSeverity.Warning);
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
            SetStatus("Saia do modo console antes de atualizar.", InfoBarSeverity.Warning);
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
            SetStatus($"Não foi possível atualizar: {ex.Message}", InfoBarSeverity.Error);
            IsUpdating = false;
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = _update?.PageUrl ?? $"https://github.com/{UpdateService.Repository}/releases";
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { SetStatus($"Não foi possível abrir o navegador: {ex.Message}", InfoBarSeverity.Error); }
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

    private static string? FirstNoteLine(string notes)
    {
        foreach (var raw in notes.Split('\n'))
        {
            var line = raw.Trim().TrimStart('#', '-', '*', ' ').Trim();
            if (line.Length > 0) return line.Length > 140 ? line[..140] + "…" : line;
        }
        return null;
    }
}
