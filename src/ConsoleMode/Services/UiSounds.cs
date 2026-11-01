using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

/// <summary>
/// Plays the console interface sounds (the ones shipped in Assets\Sounds, else <see cref="UiSoundSynth"/>) from memory through
/// winmm's PlaySound. Fire and forget: a new sound cuts the one still playing, and one that can't
/// play (no audio device, a driver error) is skipped, never an error for the user.
/// </summary>
public static class UiSounds
{
    private const uint SndAsync = 0x0001;
    private const uint SndNoDefault = 0x0002;
    private const uint SndMemory = 0x0004;

    // PlaySound reads the buffer while it plays, after the call returns: keep each one pinned.
    private static readonly Dictionary<UiSound, GCHandle> Buffers = [];
    private static bool _failed;

    /// <summary>Settings → interface sounds.</summary>
    public static bool Enabled { get; set; } = true;

    /// <param name="evenWhenOff">For the opening's sound, which has its own setting (Settings → App).</param>
    public static void Play(UiSound sound, bool evenWhenOff = false)
    {
        if (!Enabled && !evenWhenOff || _failed) return;
        try
        {
            if (!Buffers.TryGetValue(sound, out var handle))
            {
                handle = GCHandle.Alloc(Embedded(sound) ?? UiSoundSynth.Wav(sound), GCHandleType.Pinned);
                Buffers[sound] = handle;
            }
            // A new sound replaces the one playing, so quick moves keep up.
            PlaySound(handle.AddrOfPinnedObject(), 0, SndMemory | SndAsync | SndNoDefault);
        }
        catch (Exception ex)
        {
            _failed = true;
            AppLog.Write($"Sons: desativados: {ex.Message}");
        }
    }

    /// <summary>
    /// The sound shipped with the app (Assets\Sounds: Kenney's CC0 "Interface Sounds" and a tick made by the Selene
    /// theme's MIT sound script, see THIRD_PARTY_NOTICES.md), or null when it isn't there: the synthesized one plays instead.
    /// </summary>
    private static byte[]? Embedded(UiSound sound)
    {
        var name = $"ConsoleMode.Assets.Sounds.{sound.ToString().ToLowerInvariant()}.wav";
        using var stream = typeof(UiSounds).Assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
    private static extern bool PlaySound(nint sound, nint module, uint flags);
}
