using System.Text;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class UiSoundSynthTests
{
    public static IEnumerable<object[]> AllSounds() => Enum.GetValues<UiSound>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(AllSounds))]
    public void Every_sound_is_a_valid_16_bit_mono_wav(UiSound sound)
    {
        var wav = UiSoundSynth.Wav(sound);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
        Assert.Equal((short)1, BitConverter.ToInt16(wav, 20));    // PCM
        Assert.Equal((short)1, BitConverter.ToInt16(wav, 22));    // mono
        Assert.Equal(UiSoundSynth.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal((short)16, BitConverter.ToInt16(wav, 34));
        Assert.Equal(wav.Length - 44, BitConverter.ToInt32(wav, 40));
        Assert.Equal(wav.Length - 8, BitConverter.ToInt32(wav, 4));
    }

    [Theory]
    [MemberData(nameof(AllSounds))]
    public void Sounds_are_short_quiet_and_not_silent(UiSound sound)
    {
        var samples = UiSoundSynth.Samples(sound);
        var seconds = (double)samples.Length / UiSoundSynth.SampleRate;
        Assert.InRange(seconds, 0.03, 0.25);
        var peak = samples.Max(Math.Abs);
        Assert.InRange(peak, 0.05, 0.5);   // audible, but never loud over a TV
    }

    [Theory]
    [MemberData(nameof(AllSounds))]
    public void Sounds_start_and_end_at_zero_so_they_do_not_click(UiSound sound)
    {
        var samples = UiSoundSynth.Samples(sound);
        Assert.InRange(Math.Abs(samples[0]), 0f, 0.01f);
        Assert.InRange(Math.Abs(samples[^1]), 0f, 0.01f);
    }

    [Fact]
    public void The_three_sounds_are_different()
    {
        var move = UiSoundSynth.Wav(UiSound.Move);
        var confirm = UiSoundSynth.Wav(UiSound.Confirm);
        var back = UiSoundSynth.Wav(UiSound.Back);
        Assert.NotEqual(move, confirm);
        Assert.NotEqual(confirm, back);
        Assert.NotEqual(move, back);
    }
}
