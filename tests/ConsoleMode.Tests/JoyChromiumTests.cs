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
    [InlineData("joyChromium", true, true)]
    [InlineData("joyChromium", false, false)]  // it closed itself: that's what ended the session
    [InlineData("bigPicture", true, false)]    // open on its own, outside our session: left alone
    [InlineData(null, true, false)]
    public void Going_back_to_the_pc_only_unfullscreens_our_open_session(string? mode, bool running, bool expected) =>
        Assert.Equal(expected, JoyChromium.ShouldReturnToWindow(mode, running));

    [Fact]
    public void The_watched_process_is_what_the_launcher_guide_names() =>
        Assert.Equal("JoyChromium", CustomApp.NormalizeProcessName(JoyChromium.DefaultPath(@"C:\L")));
}
