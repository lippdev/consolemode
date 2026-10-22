using System.Text.Json;
using ConsoleMode.Models;

namespace ConsoleMode.Tests;

public class AutomationConfigTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void The_new_settings_start_off_and_empty_so_nothing_changes_for_existing_users()
    {
        var config = new AppConfig();
        Assert.Equal("", config.StartScript);
        Assert.Equal("", config.StopScript);
        Assert.False(config.NtscRefresh);
        Assert.False(config.ParkPointer);

        // A config saved by an older version has none of them: it loads with the defaults.
        var old = JsonSerializer.Deserialize<AppConfig>("{\"fullscreenMode\":\"bigPicture\"}", Options)!;
        Assert.Equal("", old.StartScript);
        Assert.Equal("", old.StopScript);
        Assert.False(old.NtscRefresh);
        Assert.False(old.ParkPointer);
    }

    [Fact]
    public void The_new_settings_roundtrip_through_json()
    {
        var config = new AppConfig
        {
            StartScript = "\"C:\\Game Modes\\bg3-mods.bat\" off",
            StopScript = "C:\\Game Modes\\bg3-mods.bat on",
            NtscRefresh = true,
            ParkPointer = true
        };

        var json = JsonSerializer.Serialize(config, Options);
        Assert.Contains("\"ntscRefresh\":true", json);
        Assert.Contains("\"parkPointer\":true", json);

        var loaded = JsonSerializer.Deserialize<AppConfig>(json, Options)!;
        Assert.Equal(config.StartScript, loaded.StartScript);
        Assert.Equal(config.StopScript, loaded.StopScript);
        Assert.True(loaded.NtscRefresh);
        Assert.True(loaded.ParkPointer);
    }

    [Fact]
    public void A_session_starts_with_no_stop_command_and_no_parked_pointer()
    {
        var state = new ConsoleRuntimeState();
        Assert.Equal("", state.StopScript);
        Assert.False(state.ParkPointer);
        Assert.Equal(0, state.PointerParks);
        Assert.Null(state.LastPointerPark);
    }
}
