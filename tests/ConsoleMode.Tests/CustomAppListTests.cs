using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class CustomAppListTests
{
    [Fact]
    public void Each_custom_app_has_its_own_launcher_value()
    {
        Assert.Equal("customApp:3", CustomApp.EntryMode("3"));
        Assert.True(CustomApp.IsEntryMode("customApp:3"));
        Assert.Equal("3", CustomApp.EntryId("customApp:3"));
        Assert.False(CustomApp.IsEntryMode(CustomApp.Mode));
        Assert.Null(CustomApp.EntryId("bigPicture"));
        Assert.Null(CustomApp.EntryId(null));
    }

    [Theory]
    [InlineData(new string[0], "1")]
    [InlineData(new[] { "1", "2" }, "3")]
    [InlineData(new[] { "2", "3" }, "1")]
    [InlineData(new[] { "1", "3" }, "2")]
    public void A_new_app_takes_the_smallest_free_id(string[] ids, string expected) =>
        Assert.Equal(expected, CustomApp.NextId(ids));

    [Fact]
    public void A_config_from_before_the_list_keeps_its_one_custom_app()
    {
        var config = new AppConfig { CustomAppPath = "\"C:\\Emu\\retroarch.exe\"", CustomAppArguments = "-f", CustomAppWatchProcess = "retroarch" };
        var app = Assert.Single(CustomApp.Entries(config));
        Assert.Equal("1", app.Id);
        Assert.Equal("C:\\Emu\\retroarch.exe", app.Path);
        Assert.Equal("-f", app.Arguments);
        Assert.Equal("retroarch", app.WatchProcess);
    }

    [Fact]
    public void The_list_wins_and_apps_without_an_exe_are_dropped()
    {
        var config = new AppConfig
        {
            CustomAppPath = "C:\\old.exe",
            CustomApps = [new() { Id = "2", Path = "C:\\a.exe" }, new() { Id = "4", Path = " " }, new() { Id = "5", Path = "C:\\b.exe" }]
        };
        Assert.Equal(["2", "5"], CustomApp.Entries(config).Select(a => a.Id));
        Assert.Empty(CustomApp.Entries(new AppConfig()));
    }

    [Fact]
    public void The_selected_app_is_the_one_named_else_the_first()
    {
        List<CustomAppEntry> apps = [new() { Id = "1" }, new() { Id = "2" }];
        Assert.Equal("2", CustomApp.Selected(apps, "2")!.Id);
        Assert.Equal("1", CustomApp.Selected(apps, "9")!.Id);
        Assert.Null(CustomApp.Selected([], "1"));
    }

    [Fact]
    public void A_launcher_that_quits_hands_off_to_the_program_it_started()
    {
        (int, int, string)[] processes = [(4, 0, "System"), (100, 50, "explorer.exe"), (300, 200, "Game.exe"), (301, 200, "conhost.exe")];
        // 200 (the launcher) is gone; its child is still there, with 200 as its parent.
        Assert.Equal("Game", CustomApp.HandOffTarget(processes, 200));
    }

    [Fact]
    public void Console_helpers_are_passed_through_to_what_they_started()
    {
        (int, int, string)[] processes = [(300, 200, "cmd.exe"), (301, 300, "conhost.exe"), (302, 300, "emulator.exe")];
        Assert.Equal("emulator", CustomApp.HandOffTarget(processes, 200));
    }

    [Fact]
    public void Nothing_left_running_means_no_hand_off()
    {
        (int, int, string)[] processes = [(300, 200, "conhost.exe"), (100, 50, "explorer.exe")];
        Assert.Null(CustomApp.HandOffTarget(processes, 200));
        Assert.Null(CustomApp.HandOffTarget([], 200));
    }

    [Fact]
    public void The_add_tile_and_every_custom_app_have_their_own_look()
    {
        Assert.NotEqual(LaunchArt.Neutral, LaunchArt.For(LaunchArt.AddCustomApp));
        Assert.NotEqual(LaunchArt.Neutral, LaunchArt.For(CustomApp.EntryMode("7")));
        Assert.Equal(LaunchArt.For(CustomApp.EntryMode("1")), LaunchArt.For(CustomApp.EntryMode("2")));
    }
}
