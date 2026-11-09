using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleMode.Models;

namespace ConsoleMode.Tests;

/// <summary>Legacy reads the current app's config.json and must hand every setting it doesn't know back untouched.</summary>
public class LegacyConfigTests
{
    // The same options as JsonUtil.Options (AppPaths.cs, not linked here: it touches the file system).
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string FromCurrentApp = """
        {
          "version": 2,
          "focusMonitor": "MONITOR\\GSM0001\\{x}\\0002",
          "fullscreenMode": "customApp:a1",
          "customApps": [ { "id": "a1", "path": "C:\\Games\\Emu\\emu.exe", "arguments": "-f", "watchProcess": "" } ],
          "customAppId": "a1",
          "gameProfiles": [ { "steamAppId": 620, "name": "Portal 2", "fpsLimit": 60 } ],
          "introStyle": "x360",
          "parkPointer": true,
          "taskViewShortcut": 16416,
          "menuOutsideSession": false,
          "checkUpdates": true
        }
        """;

    [Fact]
    public void Settings_of_the_current_app_survive_a_save()
    {
        var config = JsonSerializer.Deserialize<AppConfig>(FromCurrentApp, Options)!;
        // What Legacy knows, it reads as always.
        Assert.Equal(@"MONITOR\GSM0001\{x}\0002", config.FocusMonitor);
        Assert.True(config.CheckUpdates);
        // What it changes, changes; the rest goes back as it came.
        config.HdrEnable = true;
        using var saved = JsonDocument.Parse(JsonSerializer.Serialize(config, Options));
        var root = saved.RootElement;

        Assert.True(root.GetProperty("hdrEnable").GetBoolean());
        Assert.Equal("a1", root.GetProperty("customAppId").GetString());
        Assert.Equal(@"C:\Games\Emu\emu.exe", root.GetProperty("customApps")[0].GetProperty("path").GetString());
        Assert.Equal(620, root.GetProperty("gameProfiles")[0].GetProperty("steamAppId").GetInt32());
        Assert.Equal("x360", root.GetProperty("introStyle").GetString());
        Assert.True(root.GetProperty("parkPointer").GetBoolean());
        Assert.Equal(16416, root.GetProperty("taskViewShortcut").GetInt32());
        // False is a value too, not something to drop.
        Assert.False(root.GetProperty("menuOutsideSession").GetBoolean());
    }

    [Fact]
    public void A_config_of_its_own_has_nothing_extra()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("""{ "version": 2, "uiMode": "desktop" }""", Options)!;
        Assert.Equal("desktop", config.UiMode);
        Assert.Null(config.NewerSettings);
        Assert.DoesNotContain("newerSettings", JsonSerializer.Serialize(config, Options));
    }

    [Fact]
    public void Legacy_never_checks_for_updates() => Assert.False(ConsoleMode.Services.AppEdition.ChecksForUpdates);
}
