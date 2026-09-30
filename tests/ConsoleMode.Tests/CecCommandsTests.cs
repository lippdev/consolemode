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
            CecCommands.Candidates(@"D:\libcec", @"C:\app", @"C:\PF86", @"C:\PF", @"C:\bin").ToList());
        Assert.Equal(new[] { @"D:\libcec\cec-client.exe" },
            CecCommands.Candidates("\"D:\\libcec\\cec-client.exe\"", @"C:\app", @"C:\PF86", @"C:\PF", @"C:\bin").ToList());
    }

    [Fact]
    public void Default_search_is_next_to_the_exe_then_the_libcec_install_folders_then_path()
    {
        var candidates = CecCommands.Candidates("", "EXE", "PF86", "PF", "A; ;B").ToList();
        Assert.Equal(new[]
        {
            Path.Combine("EXE", "cec-client.exe"),
            Path.Combine("PF86", "Pulse-Eight", "USB-CEC Adapter", "cec-client.exe"),
            Path.Combine("PF", "Pulse-Eight", "USB-CEC Adapter", "cec-client.exe"),
            Path.Combine("A", "cec-client.exe"),
            Path.Combine("B", "cec-client.exe")
        }, candidates);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    public void Arguments_are_fixed_flags_plus_a_clamped_port_whatever_the_input(int hdmi)
    {
        var arguments = CecCommands.Arguments(hdmi);
        Assert.Equal(new[] { "-s", "-d", "1", "-t", "p", "-p" }, arguments[..6]);
        Assert.Contains(arguments[6], new[] { "1", "2", "3", "4" });
    }

    [Fact]
    public async Task Output_capture_keeps_a_bounded_prefix_and_drains_the_reader()
    {
        using var reader = new StringReader(new string('x', 100_000));

        var output = await CecOutputCapture.ReadBoundedAsync(reader, 128, CancellationToken.None);

        Assert.Equal(128, output.Length);
        Assert.Equal(new string('x', 128), output);
        Assert.Equal(-1, reader.Peek());
    }

    [Fact]
    public async Task Output_capture_honors_cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CecOutputCapture.ReadBoundedAsync(new StringReader("output"), 10, cts.Token));
    }
}
