using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ConsoleMode.ViewModels;

// The console interface is three tabs, switched with LB / RB (L1 / R1): Home (the launchers and the
// play button), Session (screens and quick settings) and System (every setting, by category).
public partial class MainViewModel
{
    public const int HomeTabIndex = 0;
    public const int SessionTabIndex = 1;
    public const int SystemTabIndex = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeTab), nameof(IsSessionTab), nameof(IsSystemTab))]
    private int _consoleTabIndex;

    public bool IsHomeTab => ConsoleTabIndex == HomeTabIndex;
    public bool IsSessionTab => ConsoleTabIndex == SessionTabIndex;
    public bool IsSystemTab => ConsoleTabIndex == SystemTabIndex;

    public string HintLb => IsPlayStationHints ? "L1" : "LB";
    public string HintRb => IsPlayStationHints ? "R1" : "RB";

    /// <summary>The System tab's category on the left; only its options show on the right.</summary>
    [ObservableProperty] private int _systemCategory;

    /// <summary>x:Bind helper: the options of one System category show only while it is the chosen one.</summary>
    public Microsoft.UI.Xaml.Visibility CategoryVisibility(int current, int category) =>
        current == category ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>x:Bind helper: the accent bar shows only next to the chosen System category (it fades, so it's an opacity).</summary>
    public double IndicatorOpacity(int current, int category) => current == category ? 1.0 : 0.0;

    /// <summary>x:Bind helper: the active tab is fully lit, the others dimmed.</summary>
    public double TabOpacity(int current, int tab) => current == tab ? 1.0 : 0.55;

    // The System tab is the old "all settings" list: the flag stays in step with it.
    partial void OnConsoleTabIndexChanged(int value) => IsConsoleSettingsOpen = value == SystemTabIndex;

    [RelayCommand]
    private void SelectConsoleTab(string index)
    {
        if (int.TryParse(index, out var value)) ConsoleTabIndex = Math.Clamp(value, HomeTabIndex, SystemTabIndex);
    }

    /// <summary>LB (-1) / RB (+1). The tabs don't wrap, like the Xbox ones. Returns whether it moved.</summary>
    public bool StepConsoleTab(int step)
    {
        var next = Math.Clamp(ConsoleTabIndex + step, HomeTabIndex, SystemTabIndex);
        if (next == ConsoleTabIndex) return false;
        ConsoleTabIndex = next;
        return true;
    }
}
