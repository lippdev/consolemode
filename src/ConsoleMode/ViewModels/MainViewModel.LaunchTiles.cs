using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Native;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ConsoleMode.ViewModels;

/// <summary>One launcher in the console Home row (Steam, Playnite, Xbox, custom app).</summary>
public sealed partial class LaunchTile : ObservableObject
{
    public required ComboOption Option { get; init; }
    public string Text => Option.Text;
    public string Glyph { get; init; } = "";
    public Brush Background { get; init; } = null!;
    /// <summary>The launcher's own logo (Steam's, Playnite's, the Xbox app's, the custom app's); until then, or without it, the glyph.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasLogo), nameof(ShowGlyph))] private ImageSource? _logo;
    public bool HasLogo => Logo is not null;
    /// <summary>The app whose logo this tile is waiting for (exe path or Store app id).</summary>
    public string? LogoKey { get; set; }
    public bool ShowGlyph => Logo is null;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Opacity))] private bool _isSelected;
    /// <summary>The other launchers fade back, so the one that opens stands out.</summary>
    public double Opacity => IsSelected ? 1 : 0.6;
}

// The console Home is a row of launchers over the game art, like a console's game row:
// moving the focus onto a tile picks what opens (see ConsoleHomeView), and A on it plays it.
public partial class MainViewModel
{
    public ObservableCollection<LaunchTile> LaunchTiles { get; } = [];

    /// <summary>The line under the launcher name: audio, FPS, HDR, VRR (the launcher itself is the title).</summary>
    [ObservableProperty] private string _homeMetaText = "";

    private void RebuildLaunchTiles()
    {
        LaunchTiles.Clear();
        foreach (var option in LaunchOptions)
        {
            var art = LaunchArt.For(option.Value);
            LaunchTiles.Add(new LaunchTile
            {
                Option = option,
                Glyph = art.Glyph,
                Background = TileGradient(art.From, art.To),
                IsSelected = ReferenceEquals(option, SelectedLaunch)
            });
        }
        foreach (var tile in LaunchTiles) _ = LoadLaunchLogoAsync(tile);
    }

    /// <summary>Asked in pixels: the tile draws it at 64, so this stays sharp on a 4K TV.</summary>
    private const int LaunchLogoSize = 256;

    // The logos are read once per app: the tiles are rebuilt on every language switch.
    // A Store app's logo is a file of its package; a program's, the icon pixels of its exe.
    private readonly Dictionary<string, (string? File, IconBitmap? Pixels)> _launchLogos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A launcher's path changed: its tile shows the new app's logo (or the glyph).</summary>
    private void RefreshLaunchLogo(string mode)
    {
        foreach (var tile in LaunchTiles.Where(t => t.Option.Value == mode))
        {
            tile.Logo = null;
            _ = LoadLaunchLogoAsync(tile);
        }
    }

    private async Task LoadLaunchLogoAsync(LaunchTile tile)
    {
        var (exe, appId) = tile.Option.Value switch
        {
            "bigPicture" => (SteamSession.FindSteamExe(), (string?)null),
            "playnite" => (Engine.Launch.GetPlaynitePath(), null),
            "xboxMode" => (null, LaunchArt.XboxAppId),
            JoyChromium.Mode => (LaunchService.GetJoyChromiumPath(), null),
            CustomApp.Mode => (CustomApp.CleanPath(CustomAppPath), null),
            _ => (null, null)
        };
        var key = appId ?? exe;
        // The app this tile now waits for: a slower load for a previous path must not land after this one.
        tile.LogoKey = key;
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!_launchLogos.TryGetValue(key, out var logo))
        {
            logo = await Task.Run(() =>
            {
                var file = appId is null ? null : PackageLogos.Find(appId);
                return (file, file is null ? WindowIcons.ForApp(exe, appId, LaunchLogoSize) : null);
            });
            _launchLogos[key] = logo;
        }
        if (!string.Equals(tile.LogoKey, key, StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            if (logo.File is not null)
            {
                tile.Logo = new BitmapImage(new Uri(logo.File)) { DecodePixelWidth = LaunchLogoSize };
            }
            else if (logo.Pixels is { } bitmap)
            {
                var image = new WriteableBitmap(bitmap.Width, bitmap.Height);
                using (var stream = image.PixelBuffer.AsStream()) stream.Write(bitmap.Pixels);
                tile.Logo = image;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: ícone de \"{key}\": {ex.Message}");
        }
    }

    private void SyncLaunchTiles()
    {
        foreach (var tile in LaunchTiles) tile.IsSelected = ReferenceEquals(tile.Option, SelectedLaunch);
    }

    [RelayCommand]
    private void SelectLaunchTile(LaunchTile? tile)
    {
        if (tile is not null && !ReferenceEquals(SelectedLaunch, tile.Option)) SelectedLaunch = tile.Option;
    }

    /// <summary>A (or a click) on a tile: that launcher, and play it if the setup allows.</summary>
    [RelayCommand]
    private async Task PlayLaunchTile(LaunchTile? tile)
    {
        SelectLaunchTile(tile);
        if (CanStart) await StartAsync();
    }

    private void UpdateHomeMeta()
    {
        // SummaryItems starts with the launcher, which is already the title. The resolution comes
        // before this text in the view, so every item here is led by its separator.
        HomeMetaText = string.Concat(SummaryItems.Skip(1).Select(item => "  ·  " + item));
    }

    private static LinearGradientBrush TileGradient(uint from, uint to) => new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Argb(from), Offset = 0 },
            new GradientStop { Color = Argb(to), Offset = 1 }
        }
    };

    private static Windows.UI.Color Argb(uint v) =>
        Microsoft.UI.ColorHelper.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
}
