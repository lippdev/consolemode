using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Media;

namespace ConsoleMode.ViewModels;

/// <summary>One launcher in the console Home row (Steam, Playnite, Xbox, custom app).</summary>
public sealed partial class LaunchTile : ObservableObject
{
    public required ComboOption Option { get; init; }
    public string Text => Option.Text;
    public string Glyph { get; init; } = "";
    public Brush Background { get; init; } = null!;
    /// <summary>Soft light in the launcher's colour behind the tile, lit when it is the one that opens.</summary>
    public Brush Glow { get; init; } = null!;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Opacity), nameof(GlowOpacity))] private bool _isSelected;
    /// <summary>The other launchers fade back, so the one that opens stands out.</summary>
    public double Opacity => IsSelected ? 1 : 0.5;
    public double GlowOpacity => IsSelected ? 1 : 0;
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
                Glow = TileGlow(art.Glow),
                IsSelected = ReferenceEquals(option, SelectedLaunch)
            });
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

    // Strongest in the middle, gone at the edge: reads as the tile lighting up the background.
    private static RadialGradientBrush TileGlow(uint color) => new()
    {
        GradientStops =
        {
            new GradientStop { Color = Argb(0x8C000000 | (color & 0xFFFFFF)), Offset = 0 },
            new GradientStop { Color = Argb(0x33000000 | (color & 0xFFFFFF)), Offset = 0.55 },
            new GradientStop { Color = Argb(color & 0xFFFFFF), Offset = 1 }
        }
    };

    private static Windows.UI.Color Argb(uint v) =>
        Microsoft.UI.ColorHelper.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
}
