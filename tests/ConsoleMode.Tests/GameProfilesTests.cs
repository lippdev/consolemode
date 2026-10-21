using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class GameProfilesTests
{
    // As the app saves its config (JsonUtil.Options): camelCase, nulls left out.
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    [Theory]
    [InlineData(0, 0, GameChange.None)]
    [InlineData(0, null, GameChange.None)]
    [InlineData(570, null, GameChange.None)]
    [InlineData(570, 570, GameChange.None)]
    [InlineData(0, 570, GameChange.Started)]
    [InlineData(570, 0, GameChange.Ended)]
    [InlineData(570, 730, GameChange.Switched)]
    public void Steam_reads_turn_into_game_changes(int previous, int? current, GameChange expected) =>
        Assert.Equal(expected, GameProfiles.Change(previous, current));

    [Fact]
    public void Saving_again_replaces_the_profile_and_keeps_the_list_sorted()
    {
        var list = GameProfiles.Upsert([], new GameProfile { SteamAppId = 730, Name = "Counter-Strike 2", FpsLimit = 0 });
        list = GameProfiles.Upsert(list, new GameProfile { SteamAppId = 1091500, Name = "Cyberpunk 2077", Hdr = true });
        list = GameProfiles.Upsert(list, new GameProfile { SteamAppId = 730, Name = "Counter-Strike 2", FpsLimit = 144 });

        Assert.Equal([730, 1091500], list.Select(p => p.SteamAppId));
        Assert.Equal(144, GameProfiles.Find(list, 730)!.FpsLimit);
    }

    [Fact]
    public void Removing_and_finding_go_by_app_id()
    {
        List<GameProfile> list = [new() { SteamAppId = 1, Name = "A" }, new() { SteamAppId = 2, Name = "B" }];
        Assert.Equal([2], GameProfiles.Remove(list, 1).Select(p => p.SteamAppId));
        Assert.Null(GameProfiles.Find(list, 0));
        Assert.Null(GameProfiles.Find(null, 1));
        Assert.Equal("B", GameProfiles.Find(list, 2)!.Name);
    }

    [Fact]
    public void A_profile_with_nothing_set_changes_nothing()
    {
        Assert.True(GameProfiles.IsEmpty(new GameProfile { SteamAppId = 1 }));
        Assert.False(GameProfiles.IsEmpty(new GameProfile { SteamAppId = 1, FpsLimit = 0 }));
        Assert.False(GameProfiles.IsEmpty(new GameProfile { SteamAppId = 1, AudioDeviceId = "{0.0.0}.{x}" }));
    }

    [Fact]
    public void The_summary_lists_only_what_the_profile_changes()
    {
        var full = new GameProfile
        {
            Mode = new SavedDisplayMode { Width = 3840, Height = 2160, Frequency = 120 },
            Hdr = true, FpsLimit = 60, AudioDeviceName = "LG TV"
        };
        Assert.Equal("3840 × 2160 @ 120 Hz  ·  HDR on  ·  60 FPS  ·  LG TV", GameProfiles.Summary(full, "HDR on", "HDR off", "No limit", "Nothing"));
        Assert.Equal("HDR off  ·  No limit", GameProfiles.Summary(new GameProfile { Hdr = false, FpsLimit = 0 }, "HDR on", "HDR off", "No limit", "Nothing"));
        Assert.Equal("Nothing", GameProfiles.Summary(new GameProfile(), "HDR on", "HDR off", "No limit", "Nothing"));
    }

    [Fact]
    public void The_game_name_comes_from_its_app_manifest()
    {
        const string acf = "\"AppState\"\n{\n\t\"appid\"\t\t\"1091500\"\n\t\"name\"\t\t\"Cyberpunk 2077\"\n\t\"StateFlags\"\t\t\"4\"\n}";
        Assert.Equal("Cyberpunk 2077", GameProfiles.ParseManifestName(acf));
        Assert.Equal("Say \"hi\"", GameProfiles.ParseManifestName("\"name\"\t\t\"Say \\\"hi\\\"\""));
        Assert.Null(GameProfiles.ParseManifestName("\"appid\"\t\"1\""));
        Assert.Null(GameProfiles.ParseManifestName(null));
    }

    [Fact]
    public void Library_folders_are_read_and_unescaped()
    {
        const string vdf = "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"C:\\\\Program Files (x86)\\\\Steam\"\n\t}\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\\\SteamLibrary\"\n\t}\n}";
        Assert.Equal([@"C:\Program Files (x86)\Steam", @"D:\SteamLibrary"], GameProfiles.ParseLibraryPaths(vdf));
        Assert.Empty(GameProfiles.ParseLibraryPaths(""));
    }

    [Fact]
    public void Settings_from_older_versions_have_no_profiles() =>
        Assert.Empty(System.Text.Json.JsonSerializer.Deserialize<AppConfig>("{}")!.GameProfiles);

    [Fact]
    public void Profiles_survive_a_save_and_load()
    {
        var config = new AppConfig
        {
            GameProfiles = [new GameProfile { SteamAppId = 730, Name = "CS2", Mode = new SavedDisplayMode { Width = 1920, Height = 1080, Frequency = 240 }, FpsLimit = 0 }]
        };
        var json = System.Text.Json.JsonSerializer.Serialize(config, Options);
        var back = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json, Options)!;
        var profile = Assert.Single(back.GameProfiles);
        Assert.Equal(240, profile.Mode!.Frequency);
        Assert.Equal(0, profile.FpsLimit);
        Assert.Null(profile.Hdr);
    }
}
