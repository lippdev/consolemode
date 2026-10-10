using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The session menu's guided tour (SessionMenuTour): offered once, the first time the menu opens, and
// then on Y in the menu or from Settings. The window draws it: the menu dims except for the rows of
// the current stop, and a bubble beside them says what they do.
public partial class MainViewModel
{
    private IReadOnlyList<SessionTourStop> _sessionTourStops = [];
    /// <summary>Settings asked for the tour: it starts as soon as the menu it opened is on screen.</summary>
    private bool _sessionTourOnOpen;

    /// <summary>Position in the tour's stops; -1 while there is no tour.</summary>
    [ObservableProperty] private int _sessionTourIndex = -1;

    public bool IsSessionTourOpen => SessionTourIndex >= 0 && SessionTourIndex < _sessionTourStops.Count;
    public SessionTourStop SessionTourStop => IsSessionTourOpen ? _sessionTourStops[SessionTourIndex] : SessionTourStop.Invite;
    public bool IsSessionTourInvite => IsSessionTourOpen && SessionTourStop == SessionTourStop.Invite;
    private bool IsSessionTourLastStop => SessionTourIndex == _sessionTourStops.Count - 1;

    public string SessionTourTitle => IsSessionTourOpen ? LocalizationService.Get(SessionMenuTour.TitleKey(SessionTourStop, IsSessionMenuPreview)) : "";

    /// <summary>The explanation, with the buttons written for the pad in use.</summary>
    public string SessionTourBody
    {
        get
        {
            if (!IsSessionTourOpen) return "";
            var key = SessionMenuTour.BodyKey(SessionTourStop, IsSessionMenuPreview);
            return SessionTourStop switch
            {
                SessionTourStop.Invite => LocalizationService.Get(key, HintAlt),
                SessionTourStop.BackToGame => LocalizationService.Get(key, HintBack),
                SessionTourStop.Volume => LocalizationService.Get(key, HintConfirm, HintOption, HintBack),
                SessionTourStop.Windows => LocalizationService.Get(key, HintConfirm, HintOption),
                _ => LocalizationService.Get(key)
            };
        }
    }

    public string SessionTourProgress => SessionMenuTour.Progress(_sessionTourStops, SessionTourIndex) is { } progress
        ? LocalizationService.Get("SessionTourProgress", progress.Number, progress.Total)
        : "";

    public string SessionTourPrimaryText => LocalizationService.Get(IsSessionTourInvite ? "SessionTourStart" : IsSessionTourLastStop ? "GotIt" : "Next");
    public string SessionTourSecondaryText => LocalizationService.Get(IsSessionTourInvite ? "SessionTourNotNow" : "SkipTour");

    partial void OnSessionTourIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSessionTourOpen));
        OnPropertyChanged(nameof(SessionTourStop));
        OnPropertyChanged(nameof(IsSessionTourInvite));
        OnPropertyChanged(nameof(SessionTourTitle));
        OnPropertyChanged(nameof(SessionTourBody));
        OnPropertyChanged(nameof(SessionTourProgress));
        OnPropertyChanged(nameof(SessionTourPrimaryText));
        OnPropertyChanged(nameof(SessionTourSecondaryText));
    }

    /// <summary>The menu's entrance is over: start the tour Settings asked for, or offer it if it was never taken.</summary>
    public void OnSessionMenuShown()
    {
        if (_sessionTourOnOpen)
        {
            _sessionTourOnOpen = false;
            StartSessionTour(invite: false);
        }
        else if (!_loadedConfig.SessionMenuTourDone) StartSessionTour(invite: true);
    }

    /// <summary>Y in the menu, or Settings with the menu already open: the tour straight away, no invite.</summary>
    public void StartSessionTour(bool invite)
    {
        if (!IsSessionMenuOpen || IsSessionTourOpen || IsSessionMenuBusy) return;
        IsSessionPickerOpen = false;
        _sessionTourStops = SessionMenuTour.Stops(invite, IsFpsMenuAvailable || IsFpsOverlayAvailable);
        SessionTourIndex = 0;
        AppLog.Write(invite ? "Menu da sessão: tour oferecido" : "Menu da sessão: tour iniciado");
    }

    /// <summary>A: the invite accepted, the next stop, or the end after the last one.</summary>
    [RelayCommand]
    private void SessionTourNext()
    {
        if (!IsSessionTourOpen) return;
        if (IsSessionTourLastStop)
        {
            EndSessionTour();
            return;
        }
        if (IsSessionTourInvite) AppLog.Write("Menu da sessão: tour aceito");
        SessionTourIndex++;
    }

    /// <summary>Left: one stop back, never back to the invite. False when there is none.</summary>
    public bool SessionTourPrevious()
    {
        if (!IsSessionTourOpen || SessionTourIndex == 0 || _sessionTourStops[SessionTourIndex - 1] == SessionTourStop.Invite) return false;
        SessionTourIndex--;
        return true;
    }

    /// <summary>B, "Skip" or "Not now", or the last stop done: the tour is not offered again (Y and Settings still show it).</summary>
    [RelayCommand]
    private void EndSessionTour()
    {
        if (!IsSessionTourOpen) return;
        AppLog.Write(IsSessionTourInvite ? "Menu da sessão: tour recusado"
            : IsSessionTourLastStop ? "Menu da sessão: tour concluído"
            : $"Menu da sessão: tour encerrado em {SessionTourStop}");
        SessionTourIndex = -1;
        if (_loadedConfig.SessionMenuTourDone) return;
        var config = BuildConfig();
        config.SessionMenuTourDone = true;
        TrySave(config);
    }

    /// <summary>The menu closed under the tour (its shortcut, the session ending): put away, still offered next time.</summary>
    private void DropSessionTour()
    {
        _sessionTourOnOpen = false;
        SessionTourIndex = -1;
    }

    /// <summary>Settings → "Show the tour": opens the menu (a preview outside a session) with the tour running.</summary>
    [RelayCommand]
    private void ShowSessionMenuTour()
    {
        if (IsSessionMenuOpen)
        {
            StartSessionTour(invite: false);
            return;
        }
        _sessionTourOnOpen = true;
        ToggleSessionMenu();
        if (!IsSessionMenuOpen) _sessionTourOnOpen = false;
    }
}
