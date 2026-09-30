using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The controller shortcuts the user picks (Home, session menu, back to the PC). Nothing is set
// until they choose in the first-run setup or in Settings; a combo can serve one purpose only.
public partial class MainViewModel
{
    /// <summary>Raised (as a PropertyChanged) whenever any shortcut changes.</summary>
    public const string ShortcutsProperty = "Shortcuts";

    private readonly ushort[] _shortcuts = new ushort[ControllerShortcuts.Slots.Length];
    private int _shortcutCaptures;

    /// <summary>The first-run setup asks the user to pick their shortcuts (desktop interface only).</summary>
    public event Action? ShortcutOnboardingRequested;

    public ushort GetShortcut(ShortcutSlot slot) => _shortcuts[(int)slot];

    public bool HomeShortcutSet => _shortcuts[(int)ShortcutSlot.Home] != 0;
    public bool ExitShortcutSet => _shortcuts[(int)ShortcutSlot.Exit] != 0;
    public bool MenuShortcutSet => _shortcuts[(int)ShortcutSlot.Menu] != 0;

    /// <summary>True while a "press the buttons you want" capture is running: no shortcut may fire.</summary>
    public bool IsCapturingShortcut => _shortcutCaptures > 0;

    public bool NeedsShortcutOnboarding => !_loadedConfig.ShortcutsOnboardingDone;

    /// <summary>"Select + Y" / "Create + △" for the pad in use, or the "not set" text.</summary>
    public string ShortcutText(ShortcutSlot slot)
    {
        var mask = GetShortcut(slot);
        return mask == 0
            ? LocalizationService.Get("ShortcutNotSet")
            : ControllerShortcuts.Format(mask, IsPlayStationHints);
    }

    public string ShortcutName(ShortcutSlot slot) => LocalizationService.Get(slot switch
    {
        ShortcutSlot.Home => "ShortcutHome",
        ShortcutSlot.Menu => "ShortcutMenu",
        _ => "ShortcutExit"
    });

    public string HomeShortcutText => ShortcutText(ShortcutSlot.Home);
    public string MenuShortcutText => ShortcutText(ShortcutSlot.Menu);
    public string ExitShortcutText => ShortcutText(ShortcutSlot.Exit);

    /// <summary>The "or hold … to go back" line on the console screen; empty when the shortcut isn't set.</summary>
    public string ConsoleActiveHintText => ExitShortcutSet
        ? LocalizationService.Get("ConsoleActiveHint", ExitShortcutText)
        : "";

    /// <summary>
    /// Sets a shortcut. Returns null on success, or the message to show when the combo is not
    /// allowed or already belongs to another shortcut.
    /// </summary>
    public string? TrySetShortcut(ShortcutSlot slot, ushort mask)
    {
        if (!ControllerShortcuts.IsAcceptable(mask))
            return LocalizationService.Get("ShortcutNotAllowed");
        var conflict = ControllerShortcuts.FindConflict(slot, mask, _shortcuts);
        if (conflict is { } other)
            return LocalizationService.Get("ShortcutInUse", ControllerShortcuts.Format(mask, IsPlayStationHints), ShortcutName(other));

        _shortcuts[(int)slot] = mask;
        ShortcutsChanged();
        return null;
    }

    public void ClearShortcut(ShortcutSlot slot)
    {
        if (_shortcuts[(int)slot] == 0) return;
        _shortcuts[(int)slot] = 0;
        ShortcutsChanged();
    }

    /// <summary>Fills the shortcuts that are still empty with the suggested combos (they never overlap each other).</summary>
    public void UseSuggestedShortcuts()
    {
        foreach (var slot in ControllerShortcuts.Slots)
        {
            if (_shortcuts[(int)slot] != 0) continue;
            var suggested = ControllerShortcuts.Suggested(slot);
            if (ControllerShortcuts.FindConflict(slot, suggested, _shortcuts) is null)
                _shortcuts[(int)slot] = suggested;
        }
        ShortcutsChanged();
    }

    public void BeginShortcutCapture()
    {
        _shortcutCaptures++;
        OnPropertyChanged(ShortcutsProperty);
    }

    public void EndShortcutCapture()
    {
        if (_shortcutCaptures > 0) _shortcutCaptures--;
        OnPropertyChanged(ShortcutsProperty);
    }

    /// <summary>Finishing or skipping the first-run setup: it is not offered again.</summary>
    public void CompleteShortcutOnboarding()
    {
        if (_loadedConfig.ShortcutsOnboardingDone) return;
        var config = BuildConfig();
        config.ShortcutsOnboardingDone = true;
        TrySave(config);
    }

    /// <summary>Asks the window to show the shortcuts setup, if it was never done and the desktop interface is up.</summary>
    public void RequestShortcutOnboarding()
    {
        if (NeedsShortcutOnboarding && !IsConsoleUi && TourStep == 0)
            ShortcutOnboardingRequested?.Invoke();
    }

    private void LoadShortcuts(AppConfig config)
    {
        _shortcuts[(int)ShortcutSlot.Home] = ControllerShortcuts.Sanitize(config.HomeShortcut);
        _shortcuts[(int)ShortcutSlot.Menu] = ControllerShortcuts.Sanitize(config.MenuShortcut);
        _shortcuts[(int)ShortcutSlot.Exit] = ControllerShortcuts.Sanitize(config.ExitShortcut);
        // A hand-edited file could give one combo two jobs: the later one is dropped.
        foreach (var slot in ControllerShortcuts.Slots)
        {
            var mask = _shortcuts[(int)slot];
            if (mask == 0) continue;
            if (!ControllerShortcuts.IsAcceptable(mask)
                || ControllerShortcuts.FindConflict(slot, mask, _shortcuts) is { } other && other < slot)
                _shortcuts[(int)slot] = 0;
        }
        NotifyShortcutTexts();
    }

    /// <summary>Console interface: A on the row turns the shortcut off, or sets the suggested combo (capturing needs the desktop settings).</summary>
    private void ToggleSuggestedShortcut(ShortcutSlot slot)
    {
        if (GetShortcut(slot) != 0) { ClearShortcut(slot); return; }
        var error = TrySetShortcut(slot, ControllerShortcuts.Suggested(slot));
        if (error is not null) SetStatus(error, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
    }

    private void ShortcutsChanged()
    {
        NotifyShortcutTexts();
        SaveQuietly();
    }

    private void NotifyShortcutTexts()
    {
        OnPropertyChanged(nameof(HomeShortcutSet));
        OnPropertyChanged(nameof(MenuShortcutSet));
        OnPropertyChanged(nameof(ExitShortcutSet));
        OnPropertyChanged(nameof(HomeShortcutText));
        OnPropertyChanged(nameof(MenuShortcutText));
        OnPropertyChanged(nameof(ExitShortcutText));
        OnPropertyChanged(nameof(ConsoleActiveHintText));
        OnPropertyChanged(ShortcutsProperty);
    }
}
