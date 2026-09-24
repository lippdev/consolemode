using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class PlaynitePathTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cm-playnite-" + Guid.NewGuid().ToString("N"));

    public PlaynitePathTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Folder_resolves_to_the_fullscreen_exe_inside_it()
    {
        var exe = Path.Combine(_dir, PlaynitePaths.FullscreenExe);
        File.WriteAllText(exe, "");

        Assert.Equal(exe, PlaynitePaths.ResolveCustom(_dir));
        Assert.Equal(exe, PlaynitePaths.ResolveCustom($"\"{_dir}\""));
    }

    [Fact]
    public void Desktop_exe_is_swapped_for_its_fullscreen_sibling()
    {
        var desktop = Path.Combine(_dir, "Playnite.DesktopApp.exe");
        var fullscreen = Path.Combine(_dir, PlaynitePaths.FullscreenExe);
        File.WriteAllText(desktop, "");
        File.WriteAllText(fullscreen, "");

        Assert.Equal(fullscreen, PlaynitePaths.ResolveCustom(desktop));
    }

    [Fact]
    public void Missing_or_empty_paths_resolve_to_null()
    {
        Assert.Null(PlaynitePaths.ResolveCustom(""));
        Assert.Null(PlaynitePaths.ResolveCustom(null));
        Assert.Null(PlaynitePaths.ResolveCustom(_dir));
        Assert.Null(PlaynitePaths.ResolveCustom(Path.Combine(_dir, "notes.txt")));
    }
}
