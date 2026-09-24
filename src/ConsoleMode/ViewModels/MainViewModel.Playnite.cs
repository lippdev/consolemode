using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace ConsoleMode.ViewModels;

// Custom Playnite location (portable installs aren't in the default folders).
public partial class MainViewModel
{
    [ObservableProperty] private string _playnitePath = "";

    public bool HasPlaynitePath => !string.IsNullOrWhiteSpace(PlaynitePath);

    public string PlaynitePathText
    {
        get
        {
            if (HasPlaynitePath)
                return PlaynitePaths.ResolveCustom(PlaynitePath) is null
                    ? LocalizationService.Get("PlaynitePathInvalid", PlaynitePath)
                    : PlaynitePath;
            return Engine.Launch.GetPlaynitePath() is { } found
                ? LocalizationService.Get("PlaynitePathDetected", found)
                : LocalizationService.Get("PlaynitePathMissing");
        }
    }

    partial void OnPlaynitePathChanged(string value)
    {
        Engine.Launch.CustomPlaynitePath = value;
        OnPropertyChanged(nameof(HasPlaynitePath));
        OnPropertyChanged(nameof(PlaynitePathText));
        if (_applying) return;
        // The launcher list only offers Playnite when it can be found.
        IsPlayniteAvailable = Engine.Launch.IsPlayniteAvailable();
        BuildLocalizedOptions();
        SaveQuietly();
    }

    [RelayCommand]
    private async Task PickPlaynitePathAsync()
    {
        try
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            picker.FileTypeFilter.Add(".exe");
            if (App.MainWindowInstance is { } window)
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            if (PlaynitePaths.ResolveCustom(file.Path) is null)
            {
                SetStatus(LocalizationService.Get("PlaynitePathInvalid", file.Path), InfoBarSeverity.Warning);
                return;
            }
            PlaynitePath = file.Path;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Playnite: {ex}");
            SetStatus(LocalizationService.Get("PlaynitePathPickError", ex.Message), InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private void ClearPlaynitePath() => PlaynitePath = "";
}
