using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

/// <summary>One card of the window switcher in the session menu.</summary>
public sealed partial class SwitchWindowItem : ObservableObject
{
    public SwitchWindowItem(SwitchWindow window) => Window = window;

    public SwitchWindow Window { get; }
    public string Title => Window.Title;
    public string ProcessName => Window.ProcessName;

    /// <summary>The program's icon, loaded after the card shows; the card draws a generic one until then.</summary>
    [ObservableProperty] private ImageSource? _icon;
}

// The "Alt + Tab" inside the session menu: the open windows, always on screen, picked with the pad.
public partial class MainViewModel
{
    public ObservableCollection<SwitchWindowItem> SwitcherWindows { get; } = [];

    [ObservableProperty] private string _sessionWindowsText = "";

    public bool SwitcherIsEmpty => SwitcherWindows.Count == 0;

    /// <summary>Lists the windows off the UI thread, then fills the grid and starts loading the icons.</summary>
    public async Task RefreshSwitcherAsync()
    {
        var windows = await Task.Run(WindowSwitcher.List);
        SwitcherWindows.Clear();
        foreach (var window in windows) SwitcherWindows.Add(new SwitchWindowItem(window));
        SessionWindowsText = LocalizationService.Get("SessionWindowsCount", windows.Count);
        OnPropertyChanged(nameof(SwitcherIsEmpty));
        foreach (var item in SwitcherWindows.ToList()) _ = LoadIconAsync(item);
    }

    private static async Task LoadIconAsync(SwitchWindowItem item)
    {
        if (item.Window.ExePath is not { } path) return;
        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
            using var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 64);
            if (thumbnail is null) return;
            var image = new BitmapImage();
            await image.SetSourceAsync(thumbnail);
            item.Icon = image;
        }
        catch
        {
            // Protected folders (packaged apps) refuse: the generic icon stays.
        }
    }

    /// <summary>A on a card: bring that window to the front and leave the menu.</summary>
    [RelayCommand]
    private void SwitchToWindow(SwitchWindowItem? item)
    {
        if (item is null) return;
        var ok = WindowSwitcher.Activate(item.Window.Handle);
        AppLog.Write($"Janelas: trocar para \"{item.Title}\" ({item.ProcessName}) => {(ok ? "ok" : "não conseguiu")}");
        if (ok) CloseSessionMenu();
    }

    /// <summary>X on a card: ask the window to close, then drop its card once it is gone.</summary>
    public async Task CloseSwitcherWindowAsync(SwitchWindowItem item)
    {
        WindowSwitcher.RequestClose(item.Window.Handle);
        AppLog.Write($"Janelas: fechar \"{item.Title}\" ({item.ProcessName})");
        // Give it a moment: a window that asks to save stays, and so does its card.
        await Task.Delay(450);
        if (!WindowSwitcher.Exists(item.Window.Handle) || !WindowSwitcher.List().Any(w => w.Handle == item.Window.Handle))
        {
            SwitcherWindows.Remove(item);
            SessionWindowsText = LocalizationService.Get("SessionWindowsCount", SwitcherWindows.Count);
            OnPropertyChanged(nameof(SwitcherIsEmpty));
        }
    }
}
