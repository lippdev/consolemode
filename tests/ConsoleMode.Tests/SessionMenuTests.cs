using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class SessionMenuTests
{
    [Theory]
    [InlineData(50, 1, 55)]
    [InlineData(50, -1, 45)]
    [InlineData(98, 1, 100)]
    [InlineData(2, -1, 0)]
    [InlineData(0, -1, 0)]
    public void Volume_steps_by_five_within_bounds(int current, int direction, int expected) =>
        Assert.Equal(expected, SessionMenuMath.StepVolume(current, direction));

    [Theory]
    [InlineData(500, 50)]
    [InlineData(1000, 100)]
    [InlineData(0, 0)]
    [InlineData(-1, null)]
    [InlineData(1001, null)]
    public void GetPercent_exit_code_is_the_level_times_ten(int exitCode, int? expected) =>
        Assert.Equal(expected, SessionMenuMath.ParseVolumeExitCode(exitCode));

    [Theory]
    [InlineData(0, 12, "12 min")]
    [InlineData(1, 5, "1 h 05 min")]
    [InlineData(2, 0, "2 h 00 min")]
    public void Elapsed_reads_like_a_console(int hours, int minutes, string expected) =>
        Assert.Equal(expected, SessionMenuMath.FormatElapsed(new TimeSpan(hours, minutes, 30)));

    [Fact]
    public void Rtss_backup_happens_only_the_first_time()
    {
        Assert.True(SessionMenuMath.NeedsRtssBackup(limitAlreadyApplied: false));
        Assert.False(SessionMenuMath.NeedsRtssBackup(limitAlreadyApplied: true));
    }
}
