using ConsoleMode.Models;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

public sealed class ConsoleEngine
{
    public ConsoleRuntimeState State { get; } = new();
    public MonitorService Monitors { get; } = new();
    public AudioService Audio { get; } = new();
    public RtssService Rtss { get; } = new();
    public LaunchService Launch { get; } = new();
    public VideoFeaturesService Video { get; } = new();

    public const string AudioOnConnectId = "__on_connect__";

    /// <summary>
    /// Runs an action on the UI thread and waits for it. The black curtain windows and the
    /// Big Picture SetWinEventHook both need a thread that pumps messages; Start/Tick don't.
    /// </summary>
    public Action<Action>? UiInvoker { get; set; }

    private void OnUi(Action action)
    {
        if (UiInvoker is null) action();
        else UiInvoker(action);
    }

    /// <param name="confirmScreen">
    /// Optional safety net, called once the screens are switched and before anything is launched.
    /// Receives the game screen bounds and returns false when nobody confirmed they can see it;
    /// Start then throws <see cref="OperationCanceledException"/> so the caller restores the setup.
    /// </param>
    public void Start(AppConfig config, MonitorInfo? focusInfo, Func<ScreenRect?, bool>? confirmScreen = null)
    {
        if (State.IsActive) throw new InvalidOperationException(LocalizationService.Get("AlreadyConsoleActive"));
        config = ResolveMonitorNames(config);
        AppLog.Write($"Start: foco={config.FocusMonitor}, esconder={string.Join('+', config.HideMonitors)}, estratégia={config.HideStrategy}, modo={config.FullscreenMode}");

        State.FocusMonitor = config.FocusMonitor;
        State.FocusMonitorRect = null;
        State.HideMonitors = [.. config.HideMonitors];
        State.HideStrategy = config.HideStrategy;
        State.FullscreenMode = config.FullscreenMode;
        State.AudioDeviceId = config.AudioAutoSwitch ? null : config.AudioDeviceId;
        State.AudioAutoSwitch = config.AudioAutoSwitch;
        State.AudioDeviceHint = config.AudioDeviceName;
        State.LastAudioSwitchName = null;
        State.AudioPendingTarget = false;
        State.ShouldExit = false;
        State.SteamMoved = false;
        State.MoveCount = 0;
        State.HasAppeared = false;
        State.ModeLaunched = false;
        State.LaunchTime = null;
        State.AbsenceCount = 0;
        State.CachedBigPictureHandle = 0;
        State.PlayniteWindowMissingSince = null;
        State.CachedXboxHandle = 0;
        State.AudioWatchComplete = false;
        State.BigPictureWatchActive = false;
        State.FpsLimit = config.FpsLimit;
        State.RtssBackup = null;
        State.RtssLimitApplied = false;
        OnUi(NativeWindows.StopBigPictureExitWatch);

        // Read the screens fresh: the row the window shows can be stale (the TV was switched off,
        // or the last restore disconnected it), and a stale "active" would skip turning it on.
        focusInfo = Monitors.GetMonitors(true).FirstOrDefault(m => m.Name == config.FocusMonitor) ?? focusInfo;
        State.FocusWasInactive = focusInfo is null || !focusInfo.IsActive;

        Monitors.SaveBackup();
        State.OriginalPrimary = Monitors.GetPrimaryName();
        Monitors.SaveBackupMeta(new MonitorBackupMeta
        {
            OriginalPrimary = State.OriginalPrimary,
            FocusWasInactive = State.FocusWasInactive,
            FocusMonitor = config.FocusMonitor,
            HideMonitors = [.. config.HideMonitors],
            HideStrategy = config.HideStrategy
        });

        State.IsActive = true;

        if (AppPaths.HasSvv)
        {
            State.BackupAudioId = Audio.GetDefaultId();
            if (!string.IsNullOrWhiteSpace(State.BackupAudioId))
                File.WriteAllText(AppPaths.BackupAudioFile, State.BackupAudioId);
        }

        if (focusInfo is null || !focusInfo.IsActive)
        {
            Monitors.EnableMonitors([config.FocusMonitor], windowsEnable: true);
            // Never hide the other screens while the game screen is still dark.
            if (!Monitors.EnsureActive(config.FocusMonitor))
            {
                AppLog.Write($"Start: {config.FocusMonitor} não ativou; restaurando");
                throw new InvalidOperationException(LocalizationService.Get("GameScreenDidNotTurnOn"));
            }
            focusInfo = Monitors.GetMonitors(true).FirstOrDefault(m => m.Name == config.FocusMonitor);
            Monitors.GetDisplayModes(config.FocusMonitor, focusInfo, forceRefresh: true);
        }

        Monitors.SetPrimary(config.FocusMonitor);
        Thread.Sleep(400);

        if (config.HideMonitors.Count > 0)
        {
            switch (config.HideStrategy)
            {
                case "disconnect":
                    Monitors.DisableWindows(config.HideMonitors);
                    Thread.Sleep(500);
                    break;
                case "blackCurtain":
                    ShowCurtains(config.HideMonitors);
                    break;
                case "turnOff":
                    Monitors.DisableDdc(config.HideMonitors);
                    Thread.Sleep(400);
                    break;
            }
        }

        Monitors.SetPrimary(config.FocusMonitor);
        Thread.Sleep(500);

        if (config.MonitorModes.TryGetValue(config.FocusMonitor, out var mode))
            Monitors.ApplyFocusMode(config.FocusMonitor, mode);

        if (config.HdrEnable) Video.EnableHdr(config.FocusMonitor, State);
        if (config.VrrEnable) Video.EnableVrr(State);

        Monitors.ClearCache();
        Audio.ClearCache();
        Monitors.UpdateFocusRect(config.FocusMonitor, State, allowMmtFallback: true);

        if (!string.IsNullOrWhiteSpace(config.AudioDeviceId) && !config.AudioAutoSwitch && AppPaths.HasSvv)
        {
            var devices = Audio.GetDevices(true);
            var target = devices.FirstOrDefault(d => d.FriendlyId == config.AudioDeviceId);
            if (target is { IsActive: true })
            {
                Audio.SetOutput(config.AudioDeviceId);
                CompleteAudioWatch();
            }
            else
            {
                State.AudioPendingTarget = true;
            }
        }

        if (AppPaths.HasSvv)
        {
            InitializeAudioWatch();
            if (!config.AudioAutoSwitch && !State.AudioPendingTarget && string.IsNullOrWhiteSpace(config.AudioDeviceId))
                CompleteAudioWatch();
        }

        if (confirmScreen is not null)
        {
            Monitors.UpdateFocusRect(config.FocusMonitor, State, allowMmtFallback: true);
            if (!confirmScreen(State.FocusMonitorRect))
            {
                AppLog.Write("Start: tela de jogo não confirmada; restaurando");
                throw new OperationCanceledException(LocalizationService.Get("GameScreenNotConfirmed"));
            }
        }

        if (config.FpsLimit > 0 && Rtss.IsReady)
        {
            var rtss = Rtss.Enable(config.FpsLimit, State);
            if (!rtss.Success && !string.IsNullOrWhiteSpace(rtss.Message))
                AppLog.Write(rtss.Message);
        }

        switch (config.FullscreenMode)
        {
            case "bigPicture":
                Thread.Sleep(400);
                Monitors.UpdateFocusRect(config.FocusMonitor, State, true);
                Launch.StartBigPicture();
                break;
            case "xboxMode":
                Launch.StartXbox();
                break;
            case "playnite":
                Thread.Sleep(400);
                Monitors.UpdateFocusRect(config.FocusMonitor, State, true);
                Launch.StartPlaynite();
                break;
        }

        State.ModeLaunched = true;
        State.LaunchTime = DateTime.Now;
        Monitors.ClearCache();
    }

    public string Tick()
    {
        if (!State.IsActive) return "inactive";
        if (State.ShouldExit) return "exit";

        var mode = State.FullscreenMode;
        if (mode == "xboxMode")
        {
            TickAudio();
            return "running";
        }

        if (IsExitSignaled())
        {
            AppLog.Write("Loop: saída do modo tela cheia detectada");
            return "exit";
        }

        if (!State.HasAppeared && !string.IsNullOrWhiteSpace(State.FocusMonitor))
        {
            var handles = Launch.GetFullscreenHandles(mode);
            if (handles.Length > 0)
            {
                Monitors.UpdateFocusRect(State.FocusMonitor, State, true);
                var focusRect = Monitors.GetMonitorRect(State.FocusMonitor, State);
                var bpHandle = handles[0];
                var bpArea = NativeWindows.GetWindowArea(bpHandle);
                var onFocus = focusRect is not null &&
                              NativeWindows.IsWindowCenterOnRect(bpHandle, focusRect.X, focusRect.Y, focusRect.Width, focusRect.Height);

                if (!onFocus && State.MoveCount < 8)
                {
                    MoveToFocus(State.FocusMonitor, handles, focusRect);
                    State.MoveCount++;
                    State.SteamMoved = true;
                    if (focusRect is not null)
                        onFocus = NativeWindows.IsWindowCenterOnRect(bpHandle, focusRect.X, focusRect.Y, focusRect.Width, focusRect.Height);
                }

                // Out of move attempts: watch the window where it is, or closing it would never restore.
                if (!onFocus && State.MoveCount >= 8)
                {
                    if (State.MoveCount == 8)
                    {
                        AppLog.Write("Loop: janela do modo tela cheia não chegou na tela de jogo; acompanhando mesmo assim");
                        State.MoveCount++;
                    }
                    onFocus = true;
                }

                if (bpArea > 200000 && onFocus)
                {
                    foreach (var h in handles)
                    {
                        if (NativeWindows.GetWindowArea(h) <= 200000) continue;
                        State.CachedBigPictureHandle = h;
                        State.HasAppeared = true;
                        OnUi(() => State.BigPictureWatchActive = NativeWindows.StartBigPictureExitWatch(h));
                        break;
                    }
                }
            }
        }

        TickAudio();
        return "running";
    }

    public int PollDelayMs()
    {
        if (State.FullscreenMode == "xboxMode") return 5000;
        if (!State.HasAppeared) return 1500;
        if (AudioWatchNeeded()) return 4000;
        return 2000;
    }

    public void Stop()
    {
        if (State.RestoreInProgress) return;
        if (!State.IsActive)
        {
            OnUi(BlackCurtain.Close);
            Rtss.Restore(State);
            return;
        }

        State.RestoreInProgress = true;
        AppLog.Write("Stop-ConsoleMode: iniciando restauração");
        try
        {
            OnUi(BlackCurtain.Close);
            Video.RestoreHdr(State);
            Video.RestoreVrr(State);
            Thread.Sleep(800);
            var restore = Monitors.RestoreBackup(State);
            if (!restore.Success)
            {
                foreach (var issue in restore.Issues)
                    AppLog.Write($"Restauração do setup: {issue}");
            }
            Audio.Restore(State.BackupAudioId);
            Rtss.Restore(State);
            Monitors.ClearCache();
            Audio.ClearCache();
            AppLog.Write("Stop-ConsoleMode: restauração concluída");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Stop-ConsoleMode: ERRO: {ex.Message}");
            throw;
        }
        finally
        {
            State.RestoreInProgress = false;
            State.IsActive = false;
            State.ShouldExit = false;
            State.SteamMoved = false;
            State.MoveCount = 0;
            State.HasAppeared = false;
            State.ModeLaunched = false;
            State.LaunchTime = null;
            State.AbsenceCount = 0;
            State.FocusMonitorRect = null;
            State.OriginalPrimary = null;
            State.FocusWasInactive = false;
            State.AudioDeviceId = null;
            State.AudioAutoSwitch = false;
            State.AudioDeviceHint = null;
            State.AudioBaselineActiveIds.Clear();
            State.LastAudioPoll = null;
            State.LastAudioSwitchName = null;
            State.AudioPendingTarget = false;
            State.CachedBigPictureHandle = 0;
            State.CachedXboxHandle = 0;
            State.AudioWatchComplete = false;
            State.BigPictureWatchActive = false;
            State.FpsLimit = 0;
            State.RtssBackup = null;
            State.RtssLimitApplied = false;
            State.HdrApplied = false;
            State.HdrMonitor = null;
            State.VrrApplied = false;
            OnUi(NativeWindows.StopBigPictureExitWatch);
        }
    }

    public void RequestExit() => State.ShouldExit = true;

    private void ShowCurtains(IReadOnlyList<string> names)
    {
        var rects = new List<ScreenRect>();
        foreach (var name in names)
        {
            var bounds = DisplayScreens.GetBounds(name);
            if (bounds is not null) rects.Add(bounds);
        }
        OnUi(() =>
        {
            BlackCurtain.Close();
            if (rects.Count > 0) BlackCurtain.Show(rects, RequestExit);
        });
    }

    /// <summary>
    /// Config stores stable monitor ids; everything below Start works with the current GDI names.
    /// </summary>
    private AppConfig ResolveMonitorNames(AppConfig config)
    {
        var focus = Monitors.ResolveName(config.FocusMonitor);
        var hide = config.HideMonitors
            .Select(Monitors.ResolveName)
            .Where(n => !string.Equals(n, focus, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var modes = new Dictionary<string, SavedDisplayMode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, mode) in config.MonitorModes)
            modes[Monitors.ResolveName(key)] = mode;

        return new AppConfig
        {
            Version = config.Version,
            FocusMonitor = focus,
            HideMonitors = hide,
            HideStrategy = config.HideStrategy,
            FullscreenMode = config.FullscreenMode,
            AudioDeviceId = config.AudioDeviceId,
            AudioDeviceName = config.AudioDeviceName,
            AudioAutoSwitch = config.AudioAutoSwitch,
            FpsLimit = config.FpsLimit,
            MonitorModes = modes,
            HdrEnable = config.HdrEnable,
            VrrEnable = config.VrrEnable,
            TourDone = config.TourDone,
            ConfirmedSetup = config.ConfirmedSetup,
            CheckUpdates = config.CheckUpdates,
            SkippedUpdateVersion = config.SkippedUpdateVersion
        };
    }

    private void MoveToFocus(string monitorName, nint[] handles, ScreenRect? rect)
    {
        if (rect is not null)
            Monitors.MoveWindowViaMmt(monitorName, rect, State.FullscreenMode == "playnite" ? "Playnite.FullscreenApp" : "steamwebhelper");

        if (rect is null) return;
        foreach (var handle in handles)
        {
            if (handle == 0) continue;
            NativeWindows.MoveWindowToRect(handle, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private bool IsExitSignaled()
    {
        var watchedGone = NativeWindows.ConsumeBigPictureExitRequest() ||
                          (State.CachedBigPictureHandle != 0 && !NativeWindows.IsWindowStillVisible(State.CachedBigPictureHandle));
        if (State.FullscreenMode == "playnite" && State.HasAppeared) return IsPlayniteExitSignaled(watchedGone);
        if (watchedGone) return true;
        if (!State.HasAppeared) return false;
        if (Launch.IsFullscreenActive(State.FullscreenMode, State))
        {
            State.AbsenceCount = 0;
            return false;
        }
        State.AbsenceCount++;
        return State.AbsenceCount >= 2;
    }

    /// <summary>
    /// Playnite may replace the window being watched (loading screen → main window). Follow the new
    /// window while the process runs, and end the session only when it exits or stays windowless.
    /// </summary>
    private bool IsPlayniteExitSignaled(bool watchedGone)
    {
        if (watchedGone) State.CachedBigPictureHandle = 0;
        var showing = Launch.IsPlayniteActive(State);
        if (showing && watchedGone)
        {
            var handle = State.CachedBigPictureHandle;
            AppLog.Write("Loop: Playnite trocou de janela; acompanhando a nova");
            OnUi(() => State.BigPictureWatchActive = NativeWindows.StartBigPictureExitWatch(handle));
        }

        if (showing) State.PlayniteWindowMissingSince = null;
        else State.PlayniteWindowMissingSince ??= DateTime.Now;
        var exit = PlayniteExitPolicy.IsExit(Launch.IsPlayniteRunning(), showing, State.PlayniteWindowMissingSince, DateTime.Now);
        if (exit) AppLog.Write("Loop: Playnite fechou");
        return exit;
    }

    private bool AudioWatchNeeded()
    {
        if (!AppPaths.HasSvv || State.AudioWatchComplete) return false;
        return State.AudioAutoSwitch || State.AudioPendingTarget;
    }

    private void CompleteAudioWatch()
    {
        State.AudioWatchComplete = true;
        State.AudioPendingTarget = false;
    }

    private void InitializeAudioWatch()
    {
        var devices = Audio.GetDevices(true);
        State.AudioBaselineActiveIds = devices.Where(d => d.IsActive).Select(d => d.FriendlyId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        State.LastAudioPoll = DateTime.Now;
    }

    private void TickAudio()
    {
        if (!AudioWatchNeeded()) return;
        if (State.AudioAutoSwitch) UpdateAutoSwitch();
        else UpdatePendingTarget();
    }

    private void UpdateAutoSwitch()
    {
        var now = DateTime.Now;
        if (State.LastAudioPoll is not null)
        {
            var min = State.HasAppeared ? 8 : 3;
            if ((now - State.LastAudioPoll.Value).TotalSeconds < min) return;
        }
        State.LastAudioPoll = now;

        var devices = Audio.GetDevices(true);
        var newly = devices.Where(d => d.IsActive && !State.AudioBaselineActiveIds.Contains(d.FriendlyId)).ToList();
        if (newly.Count == 0) return;

        MonitorInfo? focus = null;
        if (!string.IsNullOrWhiteSpace(State.FocusMonitor))
            focus = Monitors.GetMonitors().FirstOrDefault(m => m.Name == State.FocusMonitor)
                    ?? Monitors.GetMonitors(true).FirstOrDefault(m => m.Name == State.FocusMonitor);

        var pick = Audio.PickNewDevice(newly, State.AudioDeviceHint, focus);
        if (pick is null) return;
        Audio.SetOutput(pick.FriendlyId);
        State.AudioDeviceId = pick.FriendlyId;
        State.LastAudioSwitchName = pick.Name;
        CompleteAudioWatch();
    }

    private void UpdatePendingTarget()
    {
        if (!State.AudioPendingTarget || string.IsNullOrWhiteSpace(State.AudioDeviceId)) return;
        var devices = Audio.GetDevices(true);
        var target = devices.FirstOrDefault(d => d.FriendlyId == State.AudioDeviceId);
        if (target is not { IsActive: true }) return;
        Audio.SetOutput(target.FriendlyId);
        State.LastAudioSwitchName = target.Name;
        CompleteAudioWatch();
    }
}
