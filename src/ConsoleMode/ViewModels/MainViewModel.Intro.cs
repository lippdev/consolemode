using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The opening (BootIntro): the logo and a loading bar while the app loads, with a sound; each can be turned off
// in System → App. Any press, click or key skips it.
public partial class MainViewModel
{
    [ObservableProperty] private bool _isIntroShowing;
    [ObservableProperty] private bool _introAnimation = true;
    [ObservableProperty] private bool _introSound = true;

    private readonly Stopwatch _introClock = new();
    private bool _introFinishing;

    /// <summary>
    /// Called by App before the window first shows (the config isn't loaded yet, so its two settings are read here).
    /// </summary>
    public void BeginIntro()
    {
        var config = ConfigService.Load();
        if (!BootIntro.ShouldShow(config.IntroAnimation, windowShown: true)) return;
        IsIntroShowing = true;
        _introClock.Restart();
        if (BootIntro.ShouldPlaySound(shown: true, config.IntroSound)) UiSounds.Play(UiSound.Boot, evenWhenOff: true);
        _ = EndIntroAfterAsync(BootIntro.LongestShown);
    }

    /// <summary>Loading is done (or failed): keep the logo up for what is left of its minimum, then fade it out.</summary>
    public async Task FinishIntroAsync()
    {
        if (!IsIntroShowing || _introFinishing) return;
        _introFinishing = true;
        await EndIntroAfterAsync(BootIntro.StillToShow(_introClock.Elapsed));
    }

    /// <summary>A press, a click or a key on the opening.</summary>
    public void SkipIntro() => IsIntroShowing = false;

    private async Task EndIntroAfterAsync(TimeSpan delay)
    {
        if (delay > TimeSpan.Zero) await Task.Delay(delay);
        IsIntroShowing = false;
    }

    partial void OnIntroAnimationChanged(bool value)
    {
        OnPropertyChanged(nameof(IntroSoundRowEnabled));
        SaveQuietly();
    }

    partial void OnIntroSoundChanged(bool value)
    {
        SaveQuietly();
        // Turning it on lets you hear it.
        if (value && !_applying) UiSounds.Play(UiSound.Boot, evenWhenOff: true);
    }

    /// <summary>The sound only plays with the opening.</summary>
    public bool IntroSoundRowEnabled => IntroAnimation;
}
