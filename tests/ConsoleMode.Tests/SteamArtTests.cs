using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class SteamArtTests
{
    private const string Vdf = """
        "libraryfolders"
        {
            "0"
            {
                "path"      "C:\Program Files (x86)\Steam"
                "label"     ""
                "totalsize"     "0"
                "apps"
                {
                    "228980"        "192842418"
                    "1245620"       "58231457312"
                }
            }
            "1"
            {
                "path"      "D:\SteamLibrary"
                "apps"
                {
                    "1245620"       "58231457312"
                    "570"           "40000000000"
                }
            }
        }
        """;

    [Fact]
    public void Installed_games_come_from_every_library_folder_without_repeats()
    {
        Assert.Equal([228980, 1245620, 570], SteamArt.ParseInstalledAppIds(Vdf));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a vdf at all")]
    [InlineData("\"libraryfolders\" { \"0\" { \"path\" \"C:\\Steam\" } }")]
    public void A_file_without_games_gives_an_empty_list(string vdf) =>
        Assert.Empty(SteamArt.ParseInstalledAppIds(vdf));

    [Fact]
    public void The_cover_lives_in_steams_library_cache()
    {
        var path = SteamArt.CoverPath(Path.Combine("C:", "Steam"), 1245620);
        Assert.EndsWith(Path.Combine("appcache", "librarycache", "1245620", "library_600x900.jpg"), path);
    }

    [Fact]
    public void Pick_keeps_only_games_with_a_cover_and_respects_the_count()
    {
        var picked = SteamArt.Pick([1, 2, 3, 4, 5, 6], id => id % 2 == 0, 2, new Random(7));
        Assert.Equal(2, picked.Count);
        Assert.All(picked, id => Assert.True(id % 2 == 0));
        Assert.Equal(picked.Count, picked.Distinct().Count());
    }

    [Fact]
    public void Pick_returns_what_exists_when_there_are_fewer_than_asked()
    {
        var picked = SteamArt.Pick([1, 2, 3], _ => true, 40, new Random(1));
        Assert.Equal([1, 2, 3], picked.Order().ToList());
    }

    [Fact]
    public void Pick_shuffles_so_the_collage_changes_between_launches()
    {
        var ids = Enumerable.Range(1, 60).ToList();
        var a = SteamArt.Pick(ids, _ => true, 20, new Random(1));
        var b = SteamArt.Pick(ids, _ => true, 20, new Random(2));
        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Pick_with_no_room_returns_nothing(int count) =>
        Assert.Empty(SteamArt.Pick([1, 2, 3], _ => true, count, new Random(1)));

    [Theory]
    [InlineData("C:/pics/wall.JPG", true)]
    [InlineData("wall.png", true)]
    [InlineData("wall.webp", true)]
    [InlineData("wall.gif", false)]
    [InlineData("wall.exe", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_common_image_types_are_accepted_for_your_own_image(string? path, bool expected) =>
        Assert.Equal(expected, SteamArt.IsSupportedImage(path));
}
