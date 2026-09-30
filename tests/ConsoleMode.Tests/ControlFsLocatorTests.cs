using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControlFsLocatorTests
{
    [Fact]
    public void The_registry_folder_comes_first_then_the_usual_ones_without_repeats()
    {
        var folders = ControlFsLocator.Folders(@"C:\Users\ana\AppData\Local\Programs\ControlFS\", @"C:\Users\ana\AppData\Local", @"C:\Program Files");
        Assert.Equal(
            [@"C:\Users\ana\AppData\Local\Programs\ControlFS", Path.Combine(@"C:\Program Files", "ControlFS")],
            folders);
    }

    [Fact]
    public void Blank_and_quoted_inputs_are_cleaned_or_dropped()
    {
        var folders = ControlFsLocator.Folders("  ", null, null);
        Assert.Empty(folders);
        Assert.Equal([@"D:\Apps\ControlFS"], ControlFsLocator.Folders("\"D:\\Apps\\ControlFS\\\"", null, null));
    }

    [Fact]
    public void Full_screen_is_the_exact_screen_not_a_maximized_window()
    {
        Assert.True(ControlFsLocator.IsFullScreen(0, 0, 1920, 1080, 0, 0, 1920, 1080));
        Assert.True(ControlFsLocator.IsFullScreen(1920, 0, 3840, 2160, 1920, 0, 3840, 2160));      // a second screen
        Assert.False(ControlFsLocator.IsFullScreen(-8, -8, 1936, 1048, 0, 0, 1920, 1080));          // maximized: the frame overhangs
        Assert.False(ControlFsLocator.IsFullScreen(0, 0, 1920, 1080, 0, 0, 3840, 2160));            // on the wrong screen
        Assert.False(ControlFsLocator.IsFullScreen(0, 0, 100, 100, 0, 0, 0, 0));                    // no screen known
    }

    [Fact]
    public void Resolve_returns_the_first_folder_that_has_the_exe_and_null_when_none_does()
    {
        string[] folders = [@"C:\a", @"C:\b", @"C:\c"];
        var hit = ControlFsLocator.Resolve(folders, p => p == Path.Combine(@"C:\b", "ControlFS.exe") || p == Path.Combine(@"C:\c", "ControlFS.exe"));
        Assert.Equal(Path.Combine(@"C:\b", "ControlFS.exe"), hit);
        Assert.Null(ControlFsLocator.Resolve(folders, _ => false));
    }
}
