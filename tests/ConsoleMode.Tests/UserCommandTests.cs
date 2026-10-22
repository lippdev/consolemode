using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class UserCommandTests
{
    [Theory]
    [InlineData("  C:\\Scripts\\mods.bat off  ", "C:\\Scripts\\mods.bat off")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Clean_trims_and_never_returns_null(string? input, string expected) =>
        Assert.Equal(expected, UserCommand.Clean(input));

    [Theory]
    [InlineData("mods.bat", true)]
    [InlineData("  powershell -File x.ps1 ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void A_command_is_set_only_when_it_has_text(string? input, bool expected) =>
        Assert.Equal(expected, UserCommand.IsSet(input));

    [Fact]
    public void Cmd_arguments_keep_a_quoted_path_with_arguments_intact()
    {
        // /s drops only the outer pair of quotes, so the quoted path (with a space) and its argument survive.
        Assert.Equal("/d /s /c \"\"C:\\My Scripts\\mods.bat\" off\"",
            UserCommand.CmdArguments("\"C:\\My Scripts\\mods.bat\" off"));
        Assert.Equal("/d /s /c \"mods.bat on\"", UserCommand.CmdArguments("  mods.bat on  "));
    }

    [Fact]
    public void The_log_shows_one_short_line()
    {
        Assert.Equal("a b", UserCommand.ForLog("a\r\nb"));
        Assert.Equal("", UserCommand.ForLog(null));

        var longCommand = new string('x', 300);
        var shown = UserCommand.ForLog(longCommand);
        Assert.Equal(120, shown.Length);
        Assert.EndsWith("...", shown);
        Assert.Equal(new string('y', 120), UserCommand.ForLog(new string('y', 120)));
    }

    [Fact]
    public void A_command_may_run_for_a_minute_before_the_session_moves_on() =>
        Assert.Equal(TimeSpan.FromSeconds(60), UserCommand.Timeout);
}
