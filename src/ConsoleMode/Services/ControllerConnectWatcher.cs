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

    private void OnAdded(object? sender, RawGameController controller)
    {
        var now = DateTime.UtcNow;
        var can = CanTrigger?.Invoke() ?? false;
        AppLog.Write($"Controle conectado: {controller.DisplayName}; auto-start {(ControllerConnectPolicy.ShouldTrigger(now, _startedAt, _quietUntil, can) ? "sim" : "não")}; " + ControllerInput.DescribeDevices().ReplaceLineEndings(" | "));
        _dispatcher.TryEnqueue(() => PresenceChanged?.Invoke());
        if (!ControllerConnectPolicy.ShouldTrigger(now, _startedAt, _quietUntil, can)) return;
        _dispatcher.TryEnqueue(() => Connected?.Invoke());
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
