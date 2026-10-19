using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class DiagnosticsReportTests
{
    [Fact]
    public void Full_report_lists_every_section_with_its_marks()
    {
        var report = DiagnosticsReport.Format(new DiagnosticsData
        {
            System = new DiagnosticsSystem("1.7.0", "stable", "Windows 11 23H2 (build 22631.4317)", ".NET 8.0.10"),
            Displays =
            [
                new DiagnosticsDisplay("LG TV", "3840 × 2160 @ 120 Hz", "on", IsActive: true, IsPrimary: false, IsFocus: true, IsHidden: false),
                new DiagnosticsDisplay("Dell (DEL4321)", "", "", IsActive: false, IsPrimary: true, IsFocus: false, IsHidden: true)
            ],
            Audio = [new DiagnosticsAudio("Speakers", IsDefault: true, IsConfigured: false), new DiagnosticsAudio("LG TV", false, true)],
            Controllers = [new DiagnosticsController("Xbox Wireless Controller", "Xbox"), new DiagnosticsController("DualSense", "PlayStation", "battery 80%")],
            Apps = [new DiagnosticsApp("Steam", @"D:\Steam\steam.exe", "9.0"), new DiagnosticsApp("RTSS", null)],
            FullscreenMode = "bigPicture",
            Log = ["line one", "line two"]
        });

        Assert.Contains("== Console Mode ==", report);
        Assert.Contains("Version: 1.7.0 (stable)", report);
        Assert.Contains(".NET: .NET 8.0.10", report);
        Assert.Contains("- LG TV: 3840 × 2160 @ 120 Hz, HDR on [game screen]", report);
        Assert.Contains("- Dell (DEL4321): mode unknown [hidden in console mode, primary, off]", report);
        Assert.Contains("- Speakers [Windows default]", report);
        Assert.Contains("- LG TV [chosen in Console Mode]", report);
        Assert.Contains("- Xbox Wireless Controller: Xbox" + Environment.NewLine, report);
        Assert.Contains("- DualSense: PlayStation, battery 80%", report);
        Assert.Contains("Launches: bigPicture", report);
        Assert.Contains(@"- Steam: D:\Steam\steam.exe (9.0)", report);
        Assert.Contains("- RTSS: not found", report);
        Assert.Contains("line one" + Environment.NewLine + "line two", report);
        Assert.DoesNotContain(DiagnosticsReport.Unavailable, report);
    }

    [Fact]
    public void Failed_sections_read_unavailable_and_empty_ones_none()
    {
        var report = DiagnosticsReport.Format(new DiagnosticsData { Controllers = [], Log = [] });

        // System, displays, audio and programs failed; controllers and log are just empty.
        Assert.Equal(4, report.Split(DiagnosticsReport.Unavailable).Length - 1);
        Assert.Equal(2, report.Split("(none)").Length - 1);
        Assert.Contains("== Controllers ==" + Environment.NewLine + "(none)", report);
        Assert.Contains("== Displays ==" + Environment.NewLine + DiagnosticsReport.Unavailable, report);
    }

    [Fact]
    public void Log_keeps_only_the_last_lines()
    {
        var log = Enumerable.Range(1, 80).Select(i => $"entry {i:D3}").ToList();
        var report = DiagnosticsReport.Format(new DiagnosticsData { Log = log });

        Assert.DoesNotContain("entry 030", report);
        Assert.Contains("entry 031", report);
        Assert.Contains("entry 080", report);
    }

    [Theory]
    [InlineData(@"C:\Users\Filipe\AppData\Local\Playnite\Playnite.FullscreenApp.exe", @"C:\Users\…\AppData\Local\Playnite\Playnite.FullscreenApp.exe")]
    [InlineData(@"c:\users\joão silva\Desktop", @"c:\users\…\Desktop")]
    [InlineData("C:/Users/bob/Documents", "C:/Users/…/Documents")]
    [InlineData(@"perfil em C:\Users\maria", @"perfil em C:\Users\…")]
    [InlineData(@"D:\Games\Steam\steam.exe", @"D:\Games\Steam\steam.exe")]
    public void User_name_is_removed_from_paths(string input, string expected) =>
        Assert.Equal(expected, DiagnosticsReport.Redact(input));

    [Theory]
    [InlineData("chave: dpapi:AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA==", "chave: dpapi:[redacted]")]
    [InlineData("token=abc123XYZ fim", "token=[redacted] fim")]
    [InlineData("\"pairingKey\": \"s3cr3t\"", "\"pairingKey\": \"[redacted]\"")]
    [InlineData("Authorization: Bearer abc.def", "Authorization: [redacted]")]
    [InlineData("password=hunter2;", "password=[redacted];")]
    public void Secrets_are_redacted(string input, string expected) =>
        Assert.Equal(expected, DiagnosticsReport.Redact(input));

    [Fact]
    public void Private_keys_are_redacted_in_the_report()
    {
        var report = DiagnosticsReport.Format(new DiagnosticsData
        {
            Log = ["-----BEGIN PRIVATE KEY-----", "MIIEvQIBADANBgkqhkiG9w0BAQEFAASC", "-----END PRIVATE KEY-----", @"lido de C:\Users\ana\x"]
        });

        Assert.DoesNotContain("MIIEvQ", report);
        Assert.Contains("[redacted key]", report);
        Assert.DoesNotContain(@"\ana\", report);
    }
}
