using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The console interface's first-run tour (ConsoleTour has the stops): each stop switches to its tab, the view
// highlights its part of the screen and shows these texts beside it. A next, B skips, like the session menu's tour.
public partial class MainViewModel
{
    /// <summary>The stop on screen, or -1 with the tour closed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConsoleTourOpen), nameof(ConsoleTourStopNow), nameof(CanGoBackInConsoleTour),
        nameof(IsLastConsoleTourStop), nameof(ConsoleTourTitle), nameof(ConsoleTourBody), nameof(ConsoleTourProgress),
        nameof(ConsoleTourNextText), nameof(ShowSkipConsoleTour))]
    private int _consoleTourIndex = -1;

    public bool IsConsoleTourOpen => ConsoleTourIndex >= 0;
    public ConsoleTourStop ConsoleTourStopNow => ConsoleTour.Stops[Math.Max(ConsoleTourIndex, 0)];
    public bool CanGoBackInConsoleTour => ConsoleTourIndex > 0;
    public bool IsLastConsoleTourStop => ConsoleTourIndex == ConsoleTour.Stops.Count - 1;
    public bool ShowSkipConsoleTour => !IsLastConsoleTourStop;

    public string ConsoleTourTitle => IsConsoleTourOpen ? LocalizationService.Get(ConsoleTour.TitleKey(ConsoleTourStopNow)) : "";
    public string ConsoleTourBody => IsConsoleTourOpen ? LocalizationService.Get(ConsoleTour.BodyKey(ConsoleTourStopNow), ConsoleTourArgs(ConsoleTourStopNow)) : "";
    public string ConsoleTourProgress => IsConsoleTourOpen
        ? LocalizationService.Get("SessionTourProgress", ConsoleTourIndex + 1, ConsoleTour.Stops.Count) : "";
    public string ConsoleTourNextText => LocalizationService.Get(IsLastConsoleTourStop ? "ConsoleTourFinish" : "Next");

    /// <summary>The button hints of the pad in use (✕ or A…) and the names the texts point to.</summary>
    private object[] ConsoleTourArgs(ConsoleTourStop stop) => stop switch
    {
        ConsoleTourStop.Tabs => [HintLb, HintRb],
        ConsoleTourStop.Play => [HintConfirm, HintMenu],
        ConsoleTourStop.Screens => [HintConfirm],
        ConsoleTourStop.System => [HintAlt],
        ConsoleTourStop.Done => [LocalizationService.Get("AppSection"), LocalizationService.Get("TutorialCard")],
        _ => []
    };

    /// <summary>The first time the window shows (InitializeAsync, or the tray for a start that kept it hidden).</summary>
    public void OfferConsoleTour()
    {
        if (IsConsoleTourOpen || !ConsoleTour.ShouldOffer(_loadedConfig.ConsoleTourDone, IsConsoleActive)) return;
        StartTour();
    }

    /// <summary>From the start, also from System → App → Tutorial.</summary>
    [RelayCommand]
    private void StartTour()
    {
        if (IsConsoleActive) return;
        IsPickerOpen = false;
        IsRolePanelOpen = false;
        IsShortcutEditorOpen = false;
        IsStatusOpen = false;
        ConsoleTourIndex = 0;
    }

    [RelayCommand]
    private void ConsoleTourNext()
    {
        if (ConsoleTour.Step(ConsoleTourIndex, 1) is { } next) ConsoleTourIndex = next;
        else EndTour();
    }

    [RelayCommand]
    private void ConsoleTourBack()
    {
        if (ConsoleTour.Step(ConsoleTourIndex, -1) is { } previous) ConsoleTourIndex = previous;
    }

    /// <summary>Finished or skipped: not offered again, and back to Home where playing starts.</summary>
    [RelayCommand]
    private void EndTour()
    {
        if (!IsConsoleTourOpen) return;
        ConsoleTourIndex = -1;
        ConsoleTabIndex = HomeTabIndex;
        // The tour shows where the shortcuts are set: the old first-run shortcuts dialog is no longer needed.
        CompleteShortcutOnboarding();
        if (_loadedConfig.ConsoleTourDone) return;
        var config = BuildConfig();
        config.ConsoleTourDone = true;
        TrySave(config);
    }

    partial void OnConsoleTourIndexChanged(int value)
    {
        if (value < 0) return;
        var stop = ConsoleTour.Stops[value];
        if (ConsoleTour.SystemCategory(stop) is { } category) SystemCategory = category;
        ConsoleTabIndex = ConsoleTour.Tab(stop);
    }

    private void RefreshConsoleTourTexts()
    {
        OnPropertyChanged(nameof(ConsoleTourTitle));
        OnPropertyChanged(nameof(ConsoleTourBody));
        OnPropertyChanged(nameof(ConsoleTourProgress));
        OnPropertyChanged(nameof(ConsoleTourNextText));
    }
}
