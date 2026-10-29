using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class JoyChromiumTests
{
    [Fact]
    public void The_default_path_is_the_per_user_install_folder() =>
        Assert.Equal(Path.Combine(@"C:\Users\Ana\AppData\Local", "Programs", "joychromium", "JoyChromium.exe"),
            JoyChromium.DefaultPath(@"C:\Users\Ana\AppData\Local"));

    [Theory]
    [InlineData(@"C:\Users\Ana\AppData\Local\Programs\joychromium\JoyChromium.exe,0", @"C:\Users\Ana\AppData\Local\Programs\joychromium\JoyChromium.exe")]
    [InlineData("\"D:\\Apps\\JoyChromium\\JoyChromium.exe\",0", @"D:\Apps\JoyChromium\JoyChromium.exe")]
    [InlineData(@"D:\Apps\JoyChromium\JoyChromium.exe", @"D:\Apps\JoyChromium\JoyChromium.exe")]
    [InlineData(@"D:\Apps\JoyChromium\JoyChromium.exe,-101", @"D:\Apps\JoyChromium\JoyChromium.exe")]
    [InlineData(@"D:\Apps\JoyChromium\icon.ico,0", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void The_exe_comes_from_the_uninstall_icon_without_the_index(string? displayIcon, string? expected) =>
        Assert.Equal(expected, JoyChromium.ExeFromDisplayIcon(displayIcon));

    [Fact]
    public void The_default_folder_wins_then_the_uninstall_key()
    {
        const string standard = @"C:\L\Programs\joychromium\JoyChromium.exe";
        const string moved = @"D:\Apps\JoyChromium\JoyChromium.exe";

        Assert.Equal(standard, JoyChromium.Resolve(standard, moved + ",0", _ => true));
        Assert.Equal(moved, JoyChromium.Resolve(standard, moved + ",0", p => p == moved));
        Assert.Null(JoyChromium.Resolve(standard, moved + ",0", _ => false));
        Assert.Null(JoyChromium.Resolve(null, null, _ => true));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]   // it was closed: nothing to take out of full screen
    [InlineData(false, true, false)]   // open on the desk on its own, not from the menu: left alone
    public void Going_back_to_the_pc_only_unfullscreens_the_one_opened_from_the_menu(bool openedFromMenu, bool running, bool expected) =>
        Assert.Equal(expected, JoyChromium.ShouldReturnToWindow(openedFromMenu, running));

    private const string Releases = """
        [
          { "tag_name": "v0.1.4-alpha", "draft": true, "prerelease": true, "assets": [] },
          { "tag_name": "v0.1.3-alpha", "draft": false, "prerelease": true, "assets": [
            { "name": "JoyChromium-0.1.3-alpha-win-arm64.exe", "size": 116896155,
              "browser_download_url": "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win-arm64.exe",
              "digest": "sha256:8f6c97c71438f6aaefeadbcbf62070da968cac1f2370934b88e5e42f153884f1" },
            { "name": "JoyChromium-0.1.3-alpha-win-x64.exe", "size": 122413381,
              "browser_download_url": "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win-x64.exe",
              "digest": "sha256:c29c5663fa8fbf75528dfe6fa1a43e6fd602c872762f5026b749f3dcd139a148" },
            { "name": "JoyChromium-0.1.3-alpha-win.exe", "size": 238365764,
              "browser_download_url": "https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win.exe",
              "digest": "sha256:8ad9395bbcf7dffeaa6bc365525d4b500f0e7a77885383e68ef8838bcf32018f" }
          ] }
        ]
        """;

    [Fact]
    public void The_newest_published_release_gives_the_installer_for_this_processor()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(Releases);
        var x64 = JoyChromium.PickSetup(doc.RootElement, arm64: false)!;
        Assert.Equal("0.1.3-alpha", x64.Version);   // the draft above it is skipped, the pre-release counts
        Assert.Equal("JoyChromium-0.1.3-alpha-win-x64.exe", x64.Name);
        Assert.Equal("sha256:c29c5663fa8fbf75528dfe6fa1a43e6fd602c872762f5026b749f3dcd139a148", x64.Digest);
        Assert.Equal("JoyChromium-0.1.3-alpha-win-arm64.exe", JoyChromium.PickSetup(doc.RootElement, arm64: true)!.Name);
    }

    [Theory]
    [InlineData("https://example.com/JoyChromium-0.1.3-alpha-win-x64.exe", "sha256:c29c5663fa8fbf75528dfe6fa1a43e6fd602c872762f5026b749f3dcd139a148")]
    [InlineData("https://github.com/someone/fork/releases/download/v1/JoyChromium-0.1.3-alpha-win-x64.exe", "sha256:c29c5663fa8fbf75528dfe6fa1a43e6fd602c872762f5026b749f3dcd139a148")]
    [InlineData("https://github.com/lippdev/joychromium/releases/download/v0.1.3-alpha/JoyChromium-0.1.3-alpha-win-x64.exe", "")]
    public void Nothing_is_installed_from_elsewhere_or_without_a_digest(string url, string digest)
    {
        var json = $$"""[{ "tag_name": "v0.1.3-alpha", "assets": [{ "name": "JoyChromium-0.1.3-alpha-win-x64.exe", "browser_download_url": "{{url}}", "digest": "{{digest}}" }] }]""";
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Null(JoyChromium.PickSetup(doc.RootElement, arm64: false));
    }

    [Fact]
    public void The_watched_process_is_what_the_launcher_guide_names() =>
        Assert.Equal("JoyChromium", CustomApp.NormalizeProcessName(JoyChromium.DefaultPath(@"C:\L")));
}
