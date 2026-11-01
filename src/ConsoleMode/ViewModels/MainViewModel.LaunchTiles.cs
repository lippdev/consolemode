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
    /// <summary>The "+" tile after the launchers: A on it adds a custom app; the focus on it picks nothing.</summary>
    public bool IsAdd { get; init; }
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
        var add = LaunchArt.For(LaunchArt.AddCustomApp);
        LaunchTiles.Add(new LaunchTile
        {
            Option = new ComboOption { Text = LocalizationService.Get("AddCustomAppTile"), Value = LaunchArt.AddCustomApp },
            Glyph = add.Glyph,
            Background = TileGradient(add.From, add.To),
            IsAdd = true
        });
        foreach (var tile in LaunchTiles.Where(t => !t.IsAdd)) _ = LoadLaunchLogoAsync(tile);
    }

    /// <summary>Read at this size (pixels), enough for a 4K TV; each tile then gets it shrunk to what it draws.</summary>
    private const int LaunchLogoSize = 256;

    /// <summary>The logo's size on a tile (ConsoleHomeView.xaml), grown by the focused tile's 1.14 (ConsoleHomeView.ScaleTile).</summary>
    private const double LaunchLogoShown = 64 * 1.14;

    /// <summary>The screen's pixels per view pixel (1 at 100 %, 1.5 at 150 %), from the view.</summary>
    private double _logoScale = 1;

    // The logos are read once per app: the tiles are rebuilt on every language switch.
    // A Store app's logo is a file of its package; a program's, the icon pixels of its exe. Both end up as pixels.
    private readonly Dictionary<string, IconBitmap?> _launchLogos = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The window moved to a screen with another scale: the logos are shrunk again for it.</summary>
    public void SetLogoScale(double scale)
    {
        if (scale <= 0 || Math.Abs(scale - _logoScale) < 0.01) return;
        _logoScale = scale;
        foreach (var tile in LaunchTiles)
            if (tile.LogoKey is { } key && _launchLogos.TryGetValue(key, out var logo) && logo is not null) ShowLaunchLogo(tile, logo);
    }

    /// <summary>
    /// The logo shrunk here, averaging every pixel, to the pixels the tile shows: left to the screen, a 256-pixel
    /// logo drawn at 64 comes out with jagged edges.
    /// </summary>
    private void ShowLaunchLogo(LaunchTile tile, IconBitmap logo)
    {
        var (width, height) = IconPixels.FitInside(logo.Width, logo.Height, (int)Math.Ceiling(LaunchLogoShown * _logoScale));
        var pixels = IconPixels.Shrink(logo.Pixels, logo.Width, logo.Height, width, height);
        var image = new WriteableBitmap(width, height);
        using (var stream = image.PixelBuffer.AsStream()) stream.Write(pixels);
        tile.Logo = image;
    }

    /// <summary>A package's logo file as premultiplied BGRA, no larger than <see cref="LaunchLogoSize"/>.</summary>
    private static async Task<IconBitmap> ReadLogoFileAsync(string file)
    {
        var storage = await Windows.Storage.StorageFile.GetFileFromPathAsync(file);
        using var stream = await storage.OpenReadAsync();
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var (width, height) = IconPixels.FitInside((int)decoder.PixelWidth, (int)decoder.PixelHeight, LaunchLogoSize);
        var transform = new Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = (uint)width,
            ScaledHeight = (uint)height,
            InterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant
        };
        var data = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, transform,
            Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        return new IconBitmap(width, height, data.DetachPixelData());
    }

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
        var custom = CustomApp.EntryId(tile.Option.Value) is { } id ? _customApps.FirstOrDefault(a => a.Id == id) : null;
        var (exe, appId) = tile.Option.Value switch
        {
            "bigPicture" => (SteamSession.FindSteamExe(), (string?)null),
            "playnite" => (Engine.Launch.GetPlaynitePath(), null),
            "xboxMode" => (null, LaunchArt.XboxAppId),
            _ when custom is not null => (CustomApp.CleanPath(custom.Path), null),
            _ => (null, null)
        };
        var key = appId ?? exe;
        // The app this tile now waits for: a slower load for a previous path must not land after this one.
        tile.LogoKey = key;
        if (string.IsNullOrWhiteSpace(key)) return;
        try
        {
            if (!_launchLogos.TryGetValue(key, out var logo))
            {
                var file = appId is null ? null : await Task.Run(() => PackageLogos.Find(appId));
                logo = file is not null
                    ? await Task.Run(() => ReadLogoFileAsync(file))
                    : await Task.Run(() => WindowIcons.ForApp(exe, appId, LaunchLogoSize));
                _launchLogos[key] = logo;
            }
            if (!string.Equals(tile.LogoKey, key, StringComparison.OrdinalIgnoreCase) || logo is null) return;
            ShowLaunchLogo(tile, logo);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: ícone de \"{key}\": {ex.Message}");
        }
    }

    private void SyncLaunchTiles()
    {
        foreach (var tile in LaunchTiles) tile.IsSelected = !tile.IsAdd && ReferenceEquals(tile.Option, SelectedLaunch);
    }

    [RelayCommand]
    private void SelectLaunchTile(LaunchTile? tile)
    {
        if (tile is not null && !tile.IsAdd && !ReferenceEquals(SelectedLaunch, tile.Option)) SelectedLaunch = tile.Option;
    }

    /// <summary>A (or a click) on a tile: that launcher, and play it if the setup allows; on "+", add a custom app.</summary>
    [RelayCommand]
    private async Task PlayLaunchTile(LaunchTile? tile)
    {
        if (tile?.IsAdd == true) { await AddCustomAppAsync(); return; }
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
