namespace ConsoleMode.Services;

/// <summary>The interface sounds of the console mode.</summary>
public enum UiSound
{
    /// <summary>Focus moved to another item.</summary>
    Move,
    /// <summary>Something was picked, opened or switched.</summary>
    Confirm,
    /// <summary>Went back or closed a panel.</summary>
    Back
}

/// <summary>
/// Builds the interface sounds in code, so the app ships no audio files: short, soft blips
/// (a sine plus a quiet octave, a fast attack and a decay) at a low level, in the spirit of a
/// console menu. Returns a 16-bit mono PCM WAV that <c>SoundPlayer</c> can play from memory.
/// </summary>
public static class UiSoundSynth
{
    public const int SampleRate = 44100;

    /// <summary>One note: glides from <c>From</c> to <c>To</c> Hz over <c>Ms</c>.</summary>
    private readonly record struct Note(double From, double To, int Ms, double Gain);

    private static Note[] Notes(UiSound sound) => sound switch
    {
        UiSound.Move => [new(1150, 1000, 45, 0.30)],
        UiSound.Confirm => [new(740, 740, 55, 0.32), new(1110, 1110, 95, 0.34)],
        _ => [new(820, 820, 50, 0.30), new(560, 540, 85, 0.30)]
    };

    /// <summary>Samples in -1..1.</summary>
    public static float[] Samples(UiSound sound)
    {
        var samples = new List<float>();
        double phase = 0;
        foreach (var note in Notes(sound))
        {
            var count = SampleRate * note.Ms / 1000;
            var attack = SampleRate * 3 / 1000;
            var release = SampleRate * 6 / 1000;
            for (var i = 0; i < count; i++)
            {
                var t = (double)i / count;
                var frequency = note.From + (note.To - note.From) * t;
                phase += 2 * Math.PI * frequency / SampleRate;
                var tone = Math.Sin(phase) + 0.25 * Math.Sin(2 * phase);
                var envelope = Math.Exp(-3.2 * t)
                               * Math.Min(1.0, (double)i / attack)
                               * Math.Min(1.0, (double)(count - i) / release);
                samples.Add((float)(tone / 1.25 * note.Gain * envelope));
            }
        }
        return [.. samples];
    }

    public static byte[] Wav(UiSound sound)
    {
        var samples = Samples(sound);
        var dataLength = samples.Length * 2;
        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);                 // PCM header size
        writer.Write((short)1);           // PCM
        writer.Write((short)1);           // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);     // byte rate
        writer.Write((short)2);           // block align
        writer.Write((short)16);          // bits per sample
        writer.Write("data"u8);
        writer.Write(dataLength);
        foreach (var sample in samples)
            writer.Write((short)Math.Round(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
        writer.Flush();
        return stream.ToArray();
    }
}
