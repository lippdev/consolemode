using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class CustomAppTests : IDisposable
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cm-customapp-" + Guid.NewGuid().ToString("N"));

    public CustomAppTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Theory]
    [InlineData(@"C:\Games\RetroArch\retroarch.exe", "retroarch")]
    [InlineData("\"C:\\Program Files\\Emu\\Emu.EXE\"", "Emu")]
    [InlineData("  retroarch.exe ", "retroarch")]
    [InlineData("retroarch", "retroarch")]
    [InlineData("/opt/x/app.exe", "app")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Process_name_drops_the_folder_and_the_extension(string? input, string expected) =>
        Assert.Equal(expected, CustomApp.NormalizeProcessName(input));

    [Fact]
    public void Watches_the_launched_exe_unless_another_process_is_named()
    {
        Assert.Equal("Launcher", CustomApp.WatchedProcess(@"C:\Games\Launcher.exe", ""));
        Assert.Equal("Launcher", CustomApp.WatchedProcess(@"C:\Games\Launcher.exe", "   "));
        Assert.Equal("Game-Win64-Shipping", CustomApp.WatchedProcess(@"C:\Games\Launcher.exe", "Game-Win64-Shipping.exe"));
    }

    [Fact]
    public void Display_name_prefers_the_file_description()
    {
        Assert.Equal("RetroArch", CustomApp.DisplayName(@"C:\RetroArch\retroarch.exe", " RetroArch "));
        Assert.Equal("retroarch", CustomApp.DisplayName(@"C:\RetroArch\retroarch.exe", ""));
        Assert.Equal("retroarch", CustomApp.DisplayName(@"C:\RetroArch\retroarch.exe", null));
    }

    [Fact]
    public void Validation_flags_missing_wrong_and_absent_files()
    {
        var exe = Path.Combine(_dir, "emu.exe");
        var txt = Path.Combine(_dir, "notes.txt");
        File.WriteAllText(exe, "");
        File.WriteAllText(txt, "");

        Assert.Equal(CustomApp.Problem.None, CustomApp.Validate(exe));
        Assert.Equal(CustomApp.Problem.None, CustomApp.Validate($"\"{exe}\""));
        Assert.Equal(CustomApp.Problem.Missing, CustomApp.Validate(""));
        Assert.Equal(CustomApp.Problem.Missing, CustomApp.Validate(null));
        Assert.Equal(CustomApp.Problem.NotExe, CustomApp.Validate(txt));
        Assert.Equal(CustomApp.Problem.NotFound, CustomApp.Validate(Path.Combine(_dir, "gone.exe")));
        Assert.Null(CustomApp.ProblemText(exe));
        Assert.NotNull(CustomApp.ProblemText(""));
    }

    [Fact]
    public void Never_exits_before_the_process_appeared() =>
        Assert.False(CustomApp.IsExit(hasAppeared: false, running: false, goneSince: T0, T0 + TimeSpan.FromHours(1)));

    [Fact]
    public void Stays_while_the_process_runs() =>
        Assert.False(CustomApp.IsExit(hasAppeared: true, running: true, goneSince: null, T0));

    [Fact]
    public void Waits_the_grace_period_after_the_process_is_gone()
    {
        Assert.False(CustomApp.IsExit(true, false, T0, T0 + CustomApp.ExitGrace - TimeSpan.FromSeconds(1)));
        Assert.True(CustomApp.IsExit(true, false, T0, T0 + CustomApp.ExitGrace));
    }

    [Fact]
    public void Times_out_only_when_the_process_never_showed_up()
    {
        Assert.False(CustomApp.HasTimedOut(false, T0, T0 + CustomApp.AppearTimeout - TimeSpan.FromSeconds(1)));
        Assert.True(CustomApp.HasTimedOut(false, T0, T0 + CustomApp.AppearTimeout));
        Assert.False(CustomApp.HasTimedOut(true, T0, T0 + CustomApp.AppearTimeout));
        Assert.False(CustomApp.HasTimedOut(false, null, T0));
    }
}
