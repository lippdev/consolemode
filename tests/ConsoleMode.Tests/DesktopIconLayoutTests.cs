using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class DesktopIconLayoutTests
{
    private static readonly DesktopIcon Bin = new("::{645FF040-5081-101B-9F08-00AA002F954E}", 0, 0);
    private static readonly DesktopIcon Game = new(@"C:\Users\Me\Desktop\Game.lnk", 0, 96);

    [Fact]
    public void Icons_already_in_place_are_not_moved() =>
        Assert.Empty(DesktopIconLayout.Moves([Bin, Game], [Bin, Game]));

    [Fact]
    public void Icons_somewhere_else_go_back_to_where_they_were()
    {
        var moves = DesktopIconLayout.Moves([Bin, Game], [Bin, Game with { X = 1800, Y = 0 }]);
        Assert.Equal([Game], moves);
    }

    [Fact]
    public void Icons_removed_during_the_session_are_skipped() =>
        Assert.Empty(DesktopIconLayout.Moves([Bin, Game], [Bin with { X = 200 }]).Where(m => m.Name == Game.Name));

    [Fact]
    public void Icons_added_during_the_session_are_left_alone()
    {
        var added = new DesktopIcon(@"C:\Users\Me\Desktop\New.txt", 500, 500);
        Assert.Empty(DesktopIconLayout.Moves([Bin], [Bin, added]));
    }

    [Fact]
    public void Names_match_regardless_of_case() =>
        Assert.Single(DesktopIconLayout.Moves([Game], [Game with { Name = Game.Name.ToUpperInvariant(), X = 64 }]));

    [Fact]
    public void Blank_and_repeated_names_are_ignored()
    {
        var blank = new DesktopIcon("", 0, 0);
        var moves = DesktopIconLayout.Moves([blank, Game, Game with { X = 999 }], [blank with { X = 5 }, Game with { X = 10 }]);
        Assert.Equal([Game], moves);
    }

    [Fact]
    public void Settings_from_older_versions_restore_the_icons() =>
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<ConsoleMode.Models.AppConfig>("{}")!.RestoreDesktopIcons);
}
