using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>One way of reaching the TV (network API, CEC adapter…).</summary>
public interface ITvController
{
    /// <summary>Turns the TV on and switches it to the PC's HDMI input.</summary>
    Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct);

    /// <summary>Puts the TV in standby.</summary>
    Task TurnOffAsync(TvControlConfig config, CancellationToken ct);
}

/// <summary>A failure worth showing to the user as is (already localized).</summary>
public sealed class TvControlException(string message) : Exception(message);

/// <summary>
/// Turns the TV on / to the PC's input when console mode starts and, optionally, puts it in
/// standby after the restore. A TV that doesn't answer never blocks or breaks console mode:
/// the engine only logs, and its own "wait for the game screen" check does the rest.
/// </summary>
public sealed class TvControlService
{
    /// <summary>Enough for a TV waking from deep standby (Wake-on-LAN) to answer.</summary>
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Settings → Test: time to press "Allow" on the TV the first time.</summary>
    public static readonly TimeSpan PairingTimeout = TimeSpan.FromSeconds(60);

    public static ITvController? Create(string provider) => provider switch
    {
        TvControlConfig.AndroidTv => new AndroidTvController(),
        _ => null
    };

    /// <summary>Settings → Test. Throws <see cref="TvControlException"/> with a message for the user.</summary>
    public async Task TestAsync(TvControlConfig config, CancellationToken ct)
    {
        var controller = Create(config.Provider) ?? throw new TvControlException(LocalizationService.Get("TvNotConfigured"));
        AppLog.Write($"TV: teste ({config.Provider})");
        await controller.TurnOnAsync(config, PairingTimeout, ct);
    }

    /// <summary>Called by the engine on its worker thread; never throws.</summary>
    public void TurnOn(TvControlConfig config) =>
        Run(config, "ligar", StartTimeout, (c, tv, ct) => c.TurnOnAsync(tv, TimeSpan.FromSeconds(10), ct));

    /// <summary>Called by the engine after the restore; never throws.</summary>
    public void TurnOff(TvControlConfig config) =>
        Run(config, "desligar", StopTimeout, (c, tv, ct) => c.TurnOffAsync(tv, ct));

    private static void Run(TvControlConfig config, string action, TimeSpan timeout,
        Func<ITvController, TvControlConfig, CancellationToken, Task> work)
    {
        var controller = Create(config.Provider);
        if (controller is null) return;
        var started = DateTime.UtcNow;
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            Task.Run(() => work(controller, config, cts.Token), cts.Token).GetAwaiter().GetResult();
            AppLog.Write($"TV: {action} ({config.Provider}) ok em {(DateTime.UtcNow - started).TotalSeconds:0.0}s");
        }
        catch (OperationCanceledException)
        {
            AppLog.Write($"TV: {action} ({config.Provider}) sem resposta em {timeout.TotalSeconds:0}s");
        }
        catch (Exception ex)
        {
            AppLog.Write($"TV: {action} ({config.Provider}) falhou: {ex.Message}");
        }
    }
}
