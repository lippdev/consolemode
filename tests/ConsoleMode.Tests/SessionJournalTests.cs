using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class SessionJournalTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cm-journal-" + Guid.NewGuid().ToString("N"));

    public SessionJournalTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_session_journal_comes_back_as_it_was_written()
    {
        var path = Path.Combine(_dir, "session.json");
        var written = new SessionJournal
        {
            StartedAt = new DateTime(2026, 10, 29, 21, 30, 0),
            FullscreenMode = "joyChromium",
            BackupAudioId = "{0.0.0.00000000}.{abc}",
            DesktopIcons = [new DesktopIcon("Lixeira", 10, 20), new DesktopIcon("Steam", 10, 110)],
            PointerX = -400,
            PointerY = 300
        };
        SessionJournal.Write(path, written);

        var read = SessionJournal.Read(path)!;
        Assert.Equal(written.StartedAt, read.StartedAt);
        Assert.Equal("joyChromium", read.FullscreenMode);
        Assert.Equal(written.BackupAudioId, read.BackupAudioId);
        Assert.Equal(written.DesktopIcons, read.DesktopIcons);
        Assert.Equal((-400, 300), read.PointerBefore);
    }

    [Fact]
    public void Missing_parts_stay_empty()
    {
        var read = SessionJournal.Parse("""{"startedAt":"2026-10-29T21:30:00","fullscreenMode":"bigPicture"}""")!;
        Assert.Null(read.DesktopIcons);
        Assert.Null(read.PointerBefore);
        Assert.Null(read.BackupAudioId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{\"startedAt\":\"2026-10")]   // cut short when the PC lost power
    [InlineData("not json")]
    public void A_broken_journal_reads_as_none(string? json) => Assert.Null(SessionJournal.Parse(json));

    [Fact]
    public void No_file_means_no_interrupted_session_and_deleting_twice_is_fine()
    {
        var path = Path.Combine(_dir, "session.json");
        Assert.Null(SessionJournal.Read(path));
        SessionJournal.Write(path, new SessionJournal { FullscreenMode = "playnite" });
        SessionJournal.Delete(path);
        SessionJournal.Delete(path);
        Assert.False(File.Exists(path));
    }
}
