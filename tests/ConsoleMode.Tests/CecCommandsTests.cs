using ConsoleMode.Services.Tv;

namespace ConsoleMode.Tests;

public sealed class CecCommandsTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(3, "3")]
    [InlineData(0, "1")]
    [InlineData(8, "4")]
    public void Arguments_run_one_quiet_command_as_a_playback_device_on_the_pc_port(int hdmi, string port)
    {
        Assert.Equal(new[] { "-s", "-d", "1", "-t", "p", "-p", port }, CecCommands.Arguments(hdmi));
    }

    [Fact]
    public void Commands_target_the_tv()
    {
        Assert.Equal("on 0", CecCommands.PowerOn);
        Assert.Equal("as", CecCommands.ActiveSource);
        Assert.Equal("standby 0", CecCommands.Standby);
    }

    [Theory]
    [InlineData("autodetect FAILED", true)]
    [InlineData("ERROR:   could not open a connection (try 2)", true)]
    [InlineData("opening a connection to the CEC adapter...\nwaiting for input", false)]
    [InlineData("", false)]
    public void Missing_adapter_is_recognised_in_the_output(string output, bool missing)
    {
        Assert.Equal(missing, CecCommands.NoAdapter(output));
    }

    [Fact]
    public void Custom_path_wins_and_accepts_a_folder_or_the_exe()
    {
        Assert.Equal(new[] { Path.Combine(@"D:\libcec", "cec-client.exe") },
            CecCommands.Candidates(@"D:\libcec", @"C:\PF86", @"C:\PF", @"C:\bin").ToList());
        Assert.Equal(new[] { @"D:\libcec\cec-client.exe" },
            CecCommands.Candidates("\"D:\\libcec\\cec-client.exe\"", @"C:\PF86", @"C:\PF", @"C:\bin").ToList());
    }

    [Fact]
    public void Default_search_is_the_libcec_install_folders_then_path()
    {
        var candidates = CecCommands.Candidates("", "PF86", "PF", "A; ;B").ToList();
        Assert.Equal(new[]
        {
            Path.Combine("PF86", "Pulse-Eight", "USB-CEC Adapter", "cec-client.exe"),
            Path.Combine("PF", "Pulse-Eight", "USB-CEC Adapter", "cec-client.exe"),
            Path.Combine("A", "cec-client.exe"),
            Path.Combine("B", "cec-client.exe")
        }, candidates);
    }
}
