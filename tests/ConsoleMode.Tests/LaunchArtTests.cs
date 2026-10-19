using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class LaunchArtTests
{
    [Theory]
    [InlineData("bigPicture")]
    [InlineData("playnite")]
    [InlineData("xboxMode")]
    [InlineData("customApp")]
    public void Every_launcher_has_its_own_tile(string mode)
    {
        var art = LaunchArt.For(mode);
        Assert.NotEqual(LaunchArt.Neutral, art);
        Assert.False(string.IsNullOrEmpty(art.Glyph));
        Assert.Equal(0xFFu, art.From >> 24); // opaque, so the art behind doesn't show through the tile
        Assert.Equal(0xFFu, art.To >> 24);
        Assert.Equal(0xFFu, art.Glow >> 24);
    }

    [Theory]
    [InlineData("bigPicture")]
    [InlineData("playnite")]
    [InlineData("xboxMode")]
    [InlineData("customApp")]
    public void The_glow_is_brighter_than_the_tile(string mode)
    {
        static int Luma(uint c) => (int)(((c >> 16) & 0xFF) * 299 + ((c >> 8) & 0xFF) * 587 + (c & 0xFF) * 114) / 1000;
        var art = LaunchArt.For(mode);
        Assert.True(Luma(art.Glow) > Luma(art.From) + 40, $"{mode}: glow {Luma(art.Glow)} vs tile {Luma(art.From)}");
    }

    [Fact]
    public void Launchers_look_different_from_each_other()
    {
        var tiles = new[] { "bigPicture", "playnite", "xboxMode", "customApp" }.Select(LaunchArt.For).ToList();
        Assert.Equal(tiles.Count, tiles.Select(t => t.From).Distinct().Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("somethingNew")]
    public void Unknown_modes_get_the_neutral_tile(string? mode) => Assert.Equal(LaunchArt.Neutral, LaunchArt.For(mode));
}
