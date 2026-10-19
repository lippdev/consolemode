using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace ConsoleMode.ViewModels;

// "Custom app" launch mode: any .exe as the console launcher (issue #111).
public partial class MainViewModel
{
    [ObservableProperty] private string _customAppPath = "";
    [ObservableProperty] private string _customAppArguments = "";
    [ObservableProperty] private string _customAppWatchProcess = "";

    public bool IsCustomApp => SelectedLaunch?.Value == CustomApp.Mode;

    /// <summary>The exe's description or file name, shown in summaries.</summary>
    public string CustomAppName => CustomApp.Validate(CustomAppPath) == CustomApp.Problem.None
        ? LaunchService.GetCustomAppName(CustomAppPath)
        : LocalizationService.Get("LaunchCustomApp");

    public string CustomAppPathText => CustomApp.ProblemText(CustomAppPath) ?? CustomApp.CleanPath(CustomAppPath);

    /// <summary>The selected launcher as the summaries show it: a custom app by its own name.</summary>
    private string LaunchSummaryText => IsCustomApp
        ? CustomAppName
        : SelectedLaunch?.Text ?? LocalizationService.Get("LaunchSteam");

    partial void OnCustomAppPathChanged(string value)
    {
        OnPropertyChanged(nameof(CustomAppName));
        OnPropertyChanged(nameof(CustomAppPathText));
        SettingChanged();
    }

    partial void OnCustomAppArgumentsChanged(string value) => SaveQuietly();
    partial void OnCustomAppWatchProcessChanged(string value) => SaveQuietly();

    [RelayCommand]
    private async Task PickCustomAppPathAsync()
    {
        try
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            picker.FileTypeFilter.Add(".exe");
            if (App.MainWindowInstance is { } window)
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            CustomAppPath = file.Path;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aplicativo personalizado: {ex}");
            SetStatus(LocalizationService.Get("PlaynitePathPickError", ex.Message), InfoBarSeverity.Error);
        }
    }
}
