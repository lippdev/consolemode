using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
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
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasNoIcon))] private ImageSource? _icon;

    /// <summary>Still on the generic icon (loading, or the program has none).</summary>
    public bool HasNoIcon => Icon is null;
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

    /// <summary>Asked from Windows in pixels: sharp on a 4K TV, where the card draws it at more than twice its 48.</summary>
    private const int IconSize = 128;

    private static async Task LoadIconAsync(SwitchWindowItem item)
    {
        var bitmap = await Task.Run(() => WindowIcons.Load(item.Window, IconSize));
        if (bitmap is null) return;
        try
        {
            var image = new WriteableBitmap(bitmap.Width, bitmap.Height);
            using (var stream = image.PixelBuffer.AsStream()) stream.Write(bitmap.Pixels);
            item.Icon = image;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Janelas: ícone de \"{item.ProcessName}\": {ex.Message}");
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
