using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class AltTabRulesTests
{
    private static WindowFacts Normal(string title = "Discord") =>
        new(title, "Chrome_WidgetWin_1", Visible: true, Cloaked: false, HasOwner: false, IsToolWindow: false, IsAppWindow: false, IsOwnProcess: false);

    [Fact]
    public void An_ordinary_visible_titled_window_is_listed() =>
        Assert.True(AltTabRules.ShouldList(Normal()));

    [Fact]
    public void Invisible_cloaked_untitled_and_our_own_windows_are_not_listed()
    {
        Assert.False(AltTabRules.ShouldList(Normal() with { Visible = false }));
        Assert.False(AltTabRules.ShouldList(Normal() with { Cloaked = true }));      // a suspended UWP app
        Assert.False(AltTabRules.ShouldList(Normal(title: "")));
        Assert.False(AltTabRules.ShouldList(Normal(title: "   ")));
        Assert.False(AltTabRules.ShouldList(Normal() with { IsOwnProcess = true }));  // the menu itself
    }

    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Shell_SecondaryTrayWnd")]
    public void The_desktop_and_the_taskbar_are_not_listed(string className) =>
        Assert.False(AltTabRules.ShouldList(Normal("Program Manager") with { ClassName = className }));

    [Fact]
    public void Tool_windows_and_owned_dialogs_are_skipped_unless_they_say_they_are_app_windows()
    {
        Assert.False(AltTabRules.ShouldList(Normal() with { IsToolWindow = true }));
        Assert.False(AltTabRules.ShouldList(Normal() with { HasOwner = true }));
        Assert.True(AltTabRules.ShouldList(Normal() with { IsToolWindow = true, IsAppWindow = true }));
        Assert.True(AltTabRules.ShouldList(Normal() with { HasOwner = true, IsAppWindow = true }));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(9, 1)]
    public void The_switcher_opens_on_the_second_card_like_alt_tab(int count, int expected) =>
        Assert.Equal(expected, AltTabRules.InitialIndex(count));

    [Theory]
    [InlineData("chrome.exe", "Chrome")]
    [InlineData("Discord.EXE", "Discord")]
    [InlineData("steamwebhelper", "Steamwebhelper")]
    [InlineData("a.exe", "A")]
    [InlineData("", "")]
    [InlineData(".exe", "")]
    [InlineData(null, "")]
    public void Process_names_are_shown_without_the_extension(string? input, string expected) =>
        Assert.Equal(expected, AltTabRules.ProcessDisplayName(input));
}
