using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace ConsoleMode.Services;

/// <summary>Explicit, portable localization independent of the Windows display language.</summary>
public static class LocalizationService
{
    public const string PortugueseBrazil = "pt-BR";
    public const string EnglishUnitedStates = "en-US";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [PortugueseBrazil] = Load(PortugueseBrazil),
            [EnglishUnitedStates] = Load(EnglishUnitedStates)
        };

    private static string _language = PortugueseBrazil;

    public static string Language => _language;
    public static LocalizedStrings Texts { get; } = new();
    public static event EventHandler? LanguageChanged;

    public static bool SetLanguage(string? language)
    {
        var next = string.Equals(language, EnglishUnitedStates, StringComparison.OrdinalIgnoreCase)
            ? EnglishUnitedStates
            : PortugueseBrazil;
        if (string.Equals(_language, next, StringComparison.Ordinal)) return false;

        _language = next;
        Texts.NotifyAll();
        LanguageChanged?.Invoke(null, EventArgs.Empty);
        return true;
    }

    public static string Get(string key, params object?[] arguments)
    {
        var value = Catalogs[_language].GetValueOrDefault(key)
                    ?? Catalogs[PortugueseBrazil].GetValueOrDefault(key)
                    ?? $"[{key}]";
        return arguments.Length == 0
            ? value
            : string.Format(CultureInfo.GetCultureInfo(_language), value, arguments);
    }

    public static IReadOnlyCollection<string> GetKeys(string language) =>
        Catalogs[Normalize(language)].Keys.ToArray();

    /// <summary>
    /// Language for this run: the saved choice wins, then the installer's language dialog,
    /// then the Windows display language (Portuguese → pt-BR, anything else → en-US).
    /// </summary>
    public static string ResolveInitial(string? saved, string? installerChoice, string? windowsCulture)
    {
        if (!string.IsNullOrWhiteSpace(saved)) return Normalize(saved);
        if (!string.IsNullOrWhiteSpace(installerChoice)) return Normalize(installerChoice);
        return windowsCulture?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true
            ? PortugueseBrazil
            : EnglishUnitedStates;
    }

    private static string Normalize(string? language) =>
        string.Equals(language, EnglishUnitedStates, StringComparison.OrdinalIgnoreCase)
            ? EnglishUnitedStates
            : PortugueseBrazil;

    private static IReadOnlyDictionary<string, string> Load(string language)
    {
        var name = $"ConsoleMode.Localization.Strings.{language}.json";
        using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream(name)
                          ?? throw new InvalidOperationException($"Localization resource '{name}' was not embedded.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
               ?? throw new InvalidOperationException($"Localization resource '{name}' is empty or invalid.");
    }
}

public sealed class LocalizedStrings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyAll()
    {
        foreach (var property in typeof(LocalizedStrings).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
    }

    public string Brand => LocalizationService.Get(nameof(Brand));
    public string Back => LocalizationService.Get(nameof(Back));
    public string HomeHeading => LocalizationService.Get(nameof(HomeHeading));
    public string HomeDescription => LocalizationService.Get(nameof(HomeDescription));
    public string ReleaseNews => LocalizationService.Get(nameof(ReleaseNews));
    public string IgnoreUpdate => LocalizationService.Get(nameof(IgnoreUpdate));
    public string NoDisplays => LocalizationService.Get(nameof(NoDisplays));
    public string TryAgain => LocalizationService.Get(nameof(TryAgain));
    public string RefreshDisplaysTooltip => LocalizationService.Get(nameof(RefreshDisplaysTooltip));
    public string RefreshDisplaysName => LocalizationService.Get(nameof(RefreshDisplaysName));
    public string RoleQuestion => LocalizationService.Get(nameof(RoleQuestion));
    public string RolePlayHere => LocalizationService.Get(nameof(RolePlayHere));
    public string RoleTurnOff => LocalizationService.Get(nameof(RoleTurnOff));
    public string RoleKeepOn => LocalizationService.Get(nameof(RoleKeepOn));
    public string Settings => LocalizationService.Get(nameof(Settings));
    public string TourMapTitle => LocalizationService.Get(nameof(TourMapTitle));
    public string TourRoleTitle => LocalizationService.Get(nameof(TourRoleTitle));
    public string TourPlayTitle => LocalizationService.Get(nameof(TourPlayTitle));
    public string TourRoleDescription => LocalizationService.Get(nameof(TourRoleDescription));
    public string TourPlayDescription => LocalizationService.Get(nameof(TourPlayDescription));
    public string Next => LocalizationService.Get(nameof(Next));
    public string SkipTour => LocalizationService.Get(nameof(SkipTour));
    public string GotIt => LocalizationService.Get(nameof(GotIt));
    public string CreateDesktopShortcut => LocalizationService.Get(nameof(CreateDesktopShortcut));
    public string SendFeedback => LocalizationService.Get(nameof(SendFeedback));
    public string ActiveTitle => LocalizationService.Get(nameof(ActiveTitle));
    public string RestoreNow => LocalizationService.Get(nameof(RestoreNow));
    public string RunningInTray => LocalizationService.Get(nameof(RunningInTray));
    public string SettingsTitle => LocalizationService.Get(nameof(SettingsTitle));
    public string SettingsIntro => LocalizationService.Get(nameof(SettingsIntro));
    public string GameScreenSection => LocalizationService.Get(nameof(GameScreenSection));
    public string ResolutionRefreshCard => LocalizationService.Get(nameof(ResolutionRefreshCard));
    public string HideOtherScreensCard => LocalizationService.Get(nameof(HideOtherScreensCard));
    public string HideOtherScreensDescription => LocalizationService.Get(nameof(HideOtherScreensDescription));
    public string EnteringConsoleSection => LocalizationService.Get(nameof(EnteringConsoleSection));
    public string LaunchCard => LocalizationService.Get(nameof(LaunchCard));
    public string PlaynitePathCard => LocalizationService.Get(nameof(PlaynitePathCard));
    public string PlaynitePathChoose => LocalizationService.Get(nameof(PlaynitePathChoose));
    public string PlaynitePathClear => LocalizationService.Get(nameof(PlaynitePathClear));
    public string AudioOutputCard => LocalizationService.Get(nameof(AudioOutputCard));
    public string OptionalExtrasSection => LocalizationService.Get(nameof(OptionalExtrasSection));
    public string HdrCard => LocalizationService.Get(nameof(HdrCard));
    public string HdrDescription => LocalizationService.Get(nameof(HdrDescription));
    public string VrrCard => LocalizationService.Get(nameof(VrrCard));
    public string VrrDescription => LocalizationService.Get(nameof(VrrDescription));
    public string FpsCard => LocalizationService.Get(nameof(FpsCard));
    public string FpsPlaceholder => LocalizationService.Get(nameof(FpsPlaceholder));
    public string AppSection => LocalizationService.Get(nameof(AppSection));
    public string StartupCard => LocalizationService.Get(nameof(StartupCard));
    public string StartupDescription => LocalizationService.Get(nameof(StartupDescription));
    public string UpdatesCard => LocalizationService.Get(nameof(UpdatesCard));
    public string CheckNow => LocalizationService.Get(nameof(CheckNow));
    public string UiModeCard => LocalizationService.Get(nameof(UiModeCard));
    public string UiModeDescription => LocalizationService.Get(nameof(UiModeDescription));
    public string SwitchToDesktop => LocalizationService.Get(nameof(SwitchToDesktop));
    public string SwitchToConsole => LocalizationService.Get(nameof(SwitchToConsole));
    public string ConsoleScreensHeading => LocalizationService.Get(nameof(ConsoleScreensHeading));
    public string ConsoleQuickHeading => LocalizationService.Get(nameof(ConsoleQuickHeading));
    public string FullSettings => LocalizationService.Get(nameof(FullSettings));
    public string HintSelect => LocalizationService.Get(nameof(HintSelect));
    public string HintChange => LocalizationService.Get(nameof(HintChange));
    public string ConsoleSettingsHint => LocalizationService.Get(nameof(ConsoleSettingsHint));
    public string BackToPc => LocalizationService.Get(nameof(BackToPc));
    public string ConsoleActiveHint => LocalizationService.Get(nameof(ConsoleActiveHint));
    public string HomeButtonCard => LocalizationService.Get(nameof(HomeButtonCard));
    public string HomeButtonDescription => LocalizationService.Get(nameof(HomeButtonDescription));
    public string HomeButtonShortPressCard => LocalizationService.Get(nameof(HomeButtonShortPressCard));
    public string AutoStartControllerCard => LocalizationService.Get(nameof(AutoStartControllerCard));
    public string AutoStartControllerDescription => LocalizationService.Get(nameof(AutoStartControllerDescription));
    public string NotifyUpdatesCard => LocalizationService.Get(nameof(NotifyUpdatesCard));
    public string NotifyUpdatesDescription => LocalizationService.Get(nameof(NotifyUpdatesDescription));
    public string HelpDataSection => LocalizationService.Get(nameof(HelpDataSection));
    public string TvConfirmationCard => LocalizationService.Get(nameof(TvConfirmationCard));
    public string TvConfirmationDescription => LocalizationService.Get(nameof(TvConfirmationDescription));
    public string TestNow => LocalizationService.Get(nameof(TestNow));
    public string TutorialCard => LocalizationService.Get(nameof(TutorialCard));
    public string TutorialDescription => LocalizationService.Get(nameof(TutorialDescription));
    public string SeeAgain => LocalizationService.Get(nameof(SeeAgain));
    public string OneClickCard => LocalizationService.Get(nameof(OneClickCard));
    public string OneClickDescription => LocalizationService.Get(nameof(OneClickDescription));
    public string CreateShortcut => LocalizationService.Get(nameof(CreateShortcut));
    public string DataFolderCard => LocalizationService.Get(nameof(DataFolderCard));
    public string DataFolderDescription => LocalizationService.Get(nameof(DataFolderDescription));
    public string LanguageCard => LocalizationService.Get(nameof(LanguageCard));
    public string LanguageDescription => LocalizationService.Get(nameof(LanguageDescription));
    public string LanguagePortuguese => LocalizationService.Get(nameof(LanguagePortuguese));
    public string LanguageEnglish => LocalizationService.Get(nameof(LanguageEnglish));
    public string ConfirmTitle => LocalizationService.Get(nameof(ConfirmTitle));
    public string ConfirmDescription => LocalizationService.Get(nameof(ConfirmDescription));
    public string Revert => LocalizationService.Get(nameof(Revert));
    public string Continue => LocalizationService.Get(nameof(Continue));
    public string Countdown => LocalizationService.Get(nameof(Countdown));
    // ToggleSwitch defaults follow the Windows display language, not the app language.
    public string StartNow => LocalizationService.Get(nameof(StartNow));
    public string ToggleOn => LocalizationService.Get(nameof(ToggleOn));
    public string ToggleOff => LocalizationService.Get(nameof(ToggleOff));
}
