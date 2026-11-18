using Microsoft.UI.Dispatching;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

/// <summary>
/// "Enter console mode when a controller connects" (issue #29). Fires only while the app sits
/// in the tray: a pad plugged in with the window open is someone using the PC, not the couch.
/// Grace periods keep it quiet right after startup (pads already connected report themselves)
/// and right after a session ends (wireless pads reconnect on their own).
/// </summary>
public sealed class ControllerConnectWatcher : IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly DateTime _startedAt = DateTime.UtcNow;
    private DateTime _quietUntil = DateTime.MinValue;

    /// <summary>Should we act right now? (setting on, window hidden, no session.)</summary>
    public Func<bool>? CanTrigger { get; set; }

    public event Action? Connected;

    /// <summary>Any pad added or removed, no conditions; for UI that shows what's plugged in.</summary>
    public event Action? PresenceChanged;

    public ControllerConnectWatcher(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        try
        {
            RawGameController.RawGameControllerAdded += OnAdded;
            RawGameController.RawGameControllerRemoved += OnRemoved;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controles: sem aviso de conexão: {ex.Message}");
        }
    }

    /// <summary>Call when a session ends, so the pad waking up doesn't start another one.</summary>
    public void QuietFor(TimeSpan span) => _quietUntil = DateTime.UtcNow + span;

    /// <summary>
    /// Raised on a Windows.Gaming.Input thread. CanTrigger reads the window (AppWindow), which only the UI thread may
    /// touch (it threw RPC_E_WRONG_THREAD, issue #217), so the decision is made there.
    /// </summary>
    private void OnAdded(object? sender, RawGameController controller)
    {
        var now = DateTime.UtcNow;
        var name = controller.DisplayName;
        var devices = ControllerInput.DescribeDevices().ReplaceLineEndings(" | ");
        _dispatcher.TryEnqueue(() =>
        {
            bool can;
            try { can = CanTrigger?.Invoke() ?? false; }
            catch (Exception ex)
            {
                AppLog.Write($"Controles: não deu para ver se a sessão deve começar: {ex.Message}");
                can = false;
            }
            var trigger = ControllerConnectPolicy.ShouldTrigger(now, _startedAt, _quietUntil, can);
            AppLog.Write($"Controle conectado: {name}; auto-start {(trigger ? "sim" : "não")}; {devices}");
            PresenceChanged?.Invoke();
            if (trigger) Connected?.Invoke();
        });
    }

    private void OnRemoved(object? sender, RawGameController controller)
    {
        AppLog.Write($"Controle desconectado: {controller.DisplayName}");
        _dispatcher.TryEnqueue(() => PresenceChanged?.Invoke());
    }

    public void Dispose()
    {
        try
        {
            RawGameController.RawGameControllerAdded -= OnAdded;
            RawGameController.RawGameControllerRemoved -= OnRemoved;
        }
        catch { /* ignore */ }
    }
}
