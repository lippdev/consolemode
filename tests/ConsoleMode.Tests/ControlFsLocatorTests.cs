using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControlFsLocatorTests
{
    [Fact]
    public void The_registry_folder_comes_first_then_the_usual_ones_without_repeats()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), "ana", "AppData", "Local");
        var registryLocation = Path.Combine(localAppData, "Programs", "ControlFS");
        var programFiles = Path.Combine(Path.GetTempPath(), "Program Files");
        var folders = ControlFsLocator.Folders(registryLocation + Path.DirectorySeparatorChar, localAppData, programFiles);
        Assert.Equal(
            [registryLocation, Path.Combine(programFiles, "ControlFS")],
            folders);
    }

    [Fact]
    public void Blank_and_quoted_inputs_are_cleaned_or_dropped()
    {
        var folders = ControlFsLocator.Folders("  ", null, null);
        Assert.Empty(folders);
        var quotedPath = Path.Combine(Path.GetTempPath(), "Apps", "ControlFS");
        var quoted = $"\"{quotedPath}{Path.DirectorySeparatorChar}\"";
        Assert.Equal([quotedPath], ControlFsLocator.Folders(quoted, null, null));
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
        string[] folders = [Path.Combine(Path.GetTempPath(), "a"), Path.Combine(Path.GetTempPath(), "b"), Path.Combine(Path.GetTempPath(), "c")];
        var secondExe = Path.Combine(folders[1], "ControlFS.exe");
        var thirdExe = Path.Combine(folders[2], "ControlFS.exe");
        var hit = ControlFsLocator.Resolve(folders, p => p == secondExe || p == thirdExe);
        Assert.Equal(secondExe, hit);
        Assert.Null(ControlFsLocator.Resolve(folders, _ => false));
    }
}
