using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The opening (BootIntro): the logo while the app loads, with a sound, in the style picked in System → App (the app's
// own, or in the mood of the Xbox 360's or the PS4's); the opening and its sound can each be turned off there. Any
// press, click or key skips it.
public partial class MainViewModel
{
    [ObservableProperty] private bool _isIntroShowing;
    [ObservableProperty] private bool _introAnimation = true;
    [ObservableProperty] private bool _introSound = true;
    [ObservableProperty] private ComboOption? _selectedIntroStyle;

    /// <summary>The style on screen right now (the view picks the matching layer when the opening starts).</summary>
    public string IntroStyleNow { get; private set; } = BootIntro.ConsoleStyle;

    public ObservableCollection<ComboOption> IntroStyleOptions { get; } = [];

    private readonly Stopwatch _introClock = new();
    private int _introRun;

    /// <summary>
    /// Called by App before the window first shows (the config isn't loaded yet, so the opening's settings are read here).
    /// </summary>
    public void BeginIntro()
    {
        var config = ConfigService.Load();
        if (!BootIntro.ShouldShow(config.IntroAnimation, windowShown: true)) return;
        StartIntro(BootIntro.PickForOpening(config.IntroStyle, Random.Shared.NextDouble()), config.IntroSound, BootIntro.LongestShown);
    }

    /// <summary>Loading is done (or failed): keep the logo up for what is left of its minimum, then fade it out.</summary>
    public async Task FinishIntroAsync()
    {
        if (!IsIntroShowing) return;
        var run = _introRun;
        var still = BootIntro.StillToShow(_introClock.Elapsed, IntroStyleNow);
        if (still > TimeSpan.Zero) await Task.Delay(still);
        if (run == _introRun) IsIntroShowing = false;
    }

    /// <summary>A press, a click or a key on the opening.</summary>
    public void SkipIntro() => IsIntroShowing = false;

    /// <summary>System → App → See the opening: plays the chosen style now, as if the app had just opened.</summary>
    [RelayCommand]
    private void PreviewIntro()
    {
        if (IsConsoleActive) return;
        StartIntro(SelectedIntroStyle?.Value, IntroSound, BootIntro.MinimumShown(SelectedIntroStyle?.Value) + TimeSpan.FromMilliseconds(400));
    }

    private void StartIntro(string? style, bool sound, TimeSpan showFor)
    {
        IsIntroShowing = false;
        IntroStyleNow = BootIntro.Normalize(style);
        var run = ++_introRun;
        _introClock.Restart();
        IsIntroShowing = true;
        if (BootIntro.ShouldPlaySound(shown: true, sound)) UiSounds.Play(BootIntro.Sound(IntroStyleNow), evenWhenOff: true);
        _ = EndIntroAfterAsync(showFor, run);
    }

    private async Task EndIntroAfterAsync(TimeSpan delay, int run)
    {
        if (delay > TimeSpan.Zero) await Task.Delay(delay);
        if (run == _introRun) IsIntroShowing = false;
    }

    /// <summary>A style was picked in System → App (any of them): no more surprises, it is always that one.</summary>
    private bool _introStyleChosen;

    private void BuildIntroStyleOptions()
    {
        var current = SelectedIntroStyle?.Value ?? _loadedConfig.IntroStyle;
        IntroStyleOptions.Clear();
        IntroStyleOptions.Add(new ComboOption { Text = LocalizationService.Get("IntroStyleConsole"), Value = BootIntro.ConsoleStyle });
        IntroStyleOptions.Add(new ComboOption { Text = LocalizationService.Get("IntroStyleX360"), Value = BootIntro.Xbox360Style });
        IntroStyleOptions.Add(new ComboOption { Text = LocalizationService.Get("IntroStylePs4"), Value = BootIntro.Ps4Style });
        SelectedIntroStyle = IntroStyleOptions.FirstOrDefault(o => o.Value == BootIntro.Normalize(current)) ?? IntroStyleOptions[0];
    }

    partial void OnSelectedIntroStyleChanged(ComboOption? value)
    {
        if (_applying || value is null) return;
        _introStyleChosen = true;
        SaveQuietly();
    }

    /// <summary>
    /// Picked in System → App, even the one already shown (Console Mode, while unset): that counts as a choice and ends
    /// the surprises.
    /// </summary>
    private void ChooseIntroStyle(string value)
    {
        _introStyleChosen = true;
        var option = IntroStyleOptions.FirstOrDefault(o => o.Value == value) ?? SelectedIntroStyle;
        if (ReferenceEquals(option, SelectedIntroStyle)) SaveQuietly();
        else SelectedIntroStyle = option;
    }

    /// <summary>What is saved: the picked style, or nothing while none was picked (keeps the surprises).</summary>
    private string? IntroStyleToSave => _introStyleChosen ? SelectedIntroStyle?.Value : null;

    partial void OnIntroAnimationChanged(bool value)
    {
        OnPropertyChanged(nameof(IntroSoundRowEnabled));
        SaveQuietly();
    }

    partial void OnIntroSoundChanged(bool value)
    {
        SaveQuietly();
        // Turning it on lets you hear it.
        if (value && !_applying) UiSounds.Play(BootIntro.Sound(SelectedIntroStyle?.Value), evenWhenOff: true);
    }

    /// <summary>The sound and the style only matter with the opening on.</summary>
    public bool IntroSoundRowEnabled => IntroAnimation;
}
