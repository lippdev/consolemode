using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

// The console background: by default a collage of the covers of the user's installed Steam games
// under a solid-colour gradient; or just the gradient, or a picture of their own.
public partial class MainViewModel
{
    /// <summary>Covers in the collage: enough to fill a 4K screen of 220 px posters.</summary>
    private const int BackgroundCoverCount = 48;

    public ObservableCollection<ImageSource> BackgroundCovers { get; } = [];

    [ObservableProperty] private string _consoleBackgroundMode = ConsoleBackgroundService.ModeAuto;
    [ObservableProperty] private string _consoleBackgroundImage = "";
    [ObservableProperty] private ImageSource? _backgroundCustomImage;

    /// <summary>The window asks for a file picker (the mode is "image" and no picture is set yet, or the row was pressed).</summary>
    public event Action? PickBackgroundImageRequested;

    public bool IsBackgroundImageMode => ConsoleBackgroundMode == ConsoleBackgroundService.ModeImage;
    public bool ShowBackgroundCovers => ConsoleBackgroundMode == ConsoleBackgroundService.ModeAuto && BackgroundCovers.Count > 0;
    public bool ShowBackgroundCustom => ConsoleBackgroundMode == ConsoleBackgroundService.ModeImage && BackgroundCustomImage is not null;

    public string BackgroundModeText => LocalizationService.Get(ConsoleBackgroundMode switch
    {
        ConsoleBackgroundService.ModeGradient => "BackgroundGradient",
        ConsoleBackgroundService.ModeImage => "BackgroundImage",
        _ => "BackgroundAuto"
    });

    partial void OnConsoleBackgroundModeChanged(string value)
    {
        NotifyBackground();
        if (_applying || IsLoading) return;
        SaveQuietly();
        _ = RefreshBackgroundAsync();
        if (value == ConsoleBackgroundService.ModeImage && !SteamArt.IsSupportedImage(ConsoleBackgroundImage))
            PickBackgroundImageRequested?.Invoke();
    }

    partial void OnConsoleBackgroundImageChanged(string value)
    {
        if (_applying || IsLoading) return;
        SaveQuietly();
        _ = RefreshBackgroundAsync();
    }

    partial void OnBackgroundCustomImageChanged(ImageSource? value) => NotifyBackground();

    private void NotifyBackground()
    {
        OnPropertyChanged(nameof(ShowBackgroundCovers));
        OnPropertyChanged(nameof(IsBackgroundImageMode));
        OnPropertyChanged(nameof(ShowBackgroundCustom));
        OnPropertyChanged(nameof(BackgroundModeText));
    }

    /// <summary>Auto → gradient → image → auto (the console rows cycle with A).</summary>
    public void CycleBackgroundMode()
    {
        var modes = ConsoleBackgroundService.Modes;
        var next = (Array.IndexOf(modes, ConsoleBackgroundMode) + 1) % modes.Length;
        ConsoleBackgroundMode = modes[next];
    }

    public void PickBackgroundImage() => PickBackgroundImageRequested?.Invoke();

    /// <summary>Sets the picture and switches to it.</summary>
    public void SetBackgroundImage(string path)
    {
        if (!SteamArt.IsSupportedImage(path)) return;
        ConsoleBackgroundImage = path;
        ConsoleBackgroundMode = ConsoleBackgroundService.ModeImage;
    }

    /// <summary>Reads the covers off disk (not on the UI thread) and shows them, or loads the user's picture.</summary>
    public async Task RefreshBackgroundAsync()
    {
        try
        {
            var mode = ConsoleBackgroundMode;
            var custom = ConsoleBackgroundImage;
            var covers = mode == ConsoleBackgroundService.ModeAuto
                ? await Task.Run(() => ConsoleBackgroundService.FindCovers(BackgroundCoverCount))
                : [];
            if (mode != ConsoleBackgroundMode) return;   // changed while reading

            BackgroundCovers.Clear();
            foreach (var path in covers)
                BackgroundCovers.Add(new BitmapImage(new Uri(path)) { DecodePixelWidth = 240 });

            BackgroundCustomImage = mode == ConsoleBackgroundService.ModeImage && SteamArt.IsSupportedImage(custom) && File.Exists(custom)
                ? new BitmapImage(new Uri(custom)) { DecodePixelWidth = 1920 }
                : null;
            NotifyBackground();
            AppLog.Write($"Fundo: modo={mode}, capas={BackgroundCovers.Count}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Fundo: {ex.Message}");
        }
    }
}
