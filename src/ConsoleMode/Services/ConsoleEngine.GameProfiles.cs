using ConsoleMode.Models;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

// Per-game profiles (issue #69): while a session runs, Steam's running game is read on every Tick.
// When a game with a profile starts, its resolution, HDR, FPS limit and audio output are applied;
// when it closes (or another game starts), the screen goes back to how it was when it started.
public sealed partial class ConsoleEngine
{
    private void ResetGameProfileState()
    {
        State.RunningGameId = 0;
        State.GameBaseline = null;
        State.GameProfileApplied = false;
    }

    private void TickGameProfile()
    {
        if (string.IsNullOrWhiteSpace(State.FocusMonitor) || State.RestoreInProgress) return;
        var previous = State.RunningGameId;
        var current = SteamSession.RunningAppId();
        var change = GameProfiles.Change(previous, current);
        if (change == GameChange.None) return;

        if (change is GameChange.Ended or GameChange.Switched) EndGame(previous);
        State.RunningGameId = current ?? 0;
        if (change is GameChange.Started or GameChange.Switched) BeginGame(State.RunningGameId);
    }

    private void BeginGame(int appId)
    {
        State.GameBaseline = ReadGameBaseline();
        State.GameProfileApplied = false;
        if (GameProfiles.Find(State.GameProfiles, appId) is not { } profile || GameProfiles.IsEmpty(profile))
        {
            AppLog.Write($"Perfil por jogo: {appId} começou, sem perfil");
            return;
        }
        AppLog.Write($"Perfil por jogo: aplicando o perfil de {profile.Name} ({appId})");
        Apply(profile.Mode, profile.Hdr, profile.FpsLimit, profile.AudioDeviceId);
        State.GameProfileApplied = true;
    }

    private void EndGame(int appId)
    {
        if (State.GameProfileApplied && State.GameBaseline is { } baseline)
        {
            AppLog.Write($"Perfil por jogo: {appId} fechou; voltando a tela de antes");
            Apply(baseline.Mode, baseline.Hdr, baseline.FpsLimit, baseline.AudioDeviceId);
        }
        State.GameBaseline = null;
        State.GameProfileApplied = false;
    }

    /// <summary>
    /// The session menu saved a profile for the game running now: closing it goes back to how the
    /// screen was when it started, as if the profile had been applied then.
    /// </summary>
    public void OnGameProfileSaved(List<GameProfile> profiles)
    {
        State.GameProfiles = [.. profiles];
        if (State.RunningGameId > 0 && GameProfiles.Find(profiles, State.RunningGameId) is not null)
            State.GameProfileApplied = true;
    }

    /// <summary>The settings in effect now on the game screen.</summary>
    public GameBaseline ReadGameBaseline()
    {
        var focus = State.FocusMonitor ?? "";
        var mode = NativeWindows.GetCurrentDisplayMode(focus);
        return new GameBaseline
        {
            Mode = mode is null ? null : new SavedDisplayMode { Width = mode.Width, Height = mode.Height, Frequency = mode.Frequency },
            Hdr = Video.IsHdrOn(focus),
            FpsLimit = State.FpsLimit,
            AudioDeviceId = string.IsNullOrWhiteSpace(State.AudioDeviceId) ? Audio.GetDefaultId() : State.AudioDeviceId
        };
    }

    /// <summary>Each part on its own: one that fails is logged and the others still apply.</summary>
    private void Apply(SavedDisplayMode? mode, bool? hdr, int? fpsLimit, string? audioId)
    {
        var focus = State.FocusMonitor!;
        if (mode is { Width: > 0, Height: > 0 })
        {
            var current = NativeWindows.GetCurrentDisplayMode(focus);
            if (current is null || current.Width != mode.Width || current.Height != mode.Height || current.Frequency != mode.Frequency)
                Try("resolução", () =>
                {
                    Monitors.ApplyFocusMode(focus, mode);
                    Monitors.UpdateFocusRect(focus, State, true);
                });
        }
        if (hdr is { } on && Video.IsHdrOn(focus) != on)
            Try("HDR", () => { if (!Video.SetHdrFromMenu(focus, on, State)) throw new InvalidOperationException(focus); });
        if (fpsLimit is { } fps && fps != State.FpsLimit && Rtss.IsReady)
            Try("FPS", () =>
            {
                var result = Rtss.SetLimit(fps, State);
                if (!result.Success) throw new InvalidOperationException(result.Message);
            });
        if (!string.IsNullOrWhiteSpace(audioId) && audioId != (State.AudioDeviceId ?? Audio.GetDefaultId()))
            Try("áudio", () =>
            {
                if (Audio.GetDevices(true).FirstOrDefault(d => d.FriendlyId == audioId) is not { IsActive: true })
                    throw new InvalidOperationException($"saída {audioId} não está ativa");
                Audio.SetOutput(audioId);
                State.AudioDeviceId = audioId;
                State.AudioWatchComplete = true;   // the auto-switch must not undo it
            });
    }

    private static void Try(string what, Action action)
    {
        try { action(); }
        catch (Exception ex) { AppLog.Write($"Perfil por jogo: {what}: {ex.Message}"); }
    }
}
