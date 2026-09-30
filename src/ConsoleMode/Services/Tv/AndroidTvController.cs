using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// Android TV / Google TV (TCL, Sony, Hisense, Philips…) over the network with ADB:
/// wake up, then the "HDMI n" key (or a custom command). Needs Developer options →
/// "USB debugging" / "Network debugging" on the TV; nothing to install on the PC.
/// </summary>
public sealed class AndroidTvController : ITvController
{
    public async Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        await using var adb = await ConnectAsync(config, approvalTimeout, wakeOnLan: true, ct);
        await adb.ShellAsync($"input keyevent {AdbProtocol.KeyWakeUp}", ct);
        // Right after waking, some TVs drop the input key while the launcher loads.
        await Task.Delay(1500, ct);
        var inputCommand = string.IsNullOrWhiteSpace(config.InputCommand)
            ? $"input keyevent {AdbProtocol.HdmiKey(config.HdmiInput)}"
            : config.InputCommand.Trim();
        await adb.ShellAsync(inputCommand, ct);
    }

    public async Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        await using var adb = await ConnectAsync(config, TimeSpan.FromSeconds(5), wakeOnLan: false, ct);
        await adb.ShellAsync($"input keyevent {AdbProtocol.KeySleep}", ct);
    }

    private static Task<AdbClient> ConnectAsync(TvControlConfig config, TimeSpan approvalTimeout, bool wakeOnLan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new TvControlException(LocalizationService.Get("TvHostMissing"));
        var (host, port) = AdbProtocol.ParseEndpoint(config.Host);
        return TvNetwork.ConnectAsync(config, wakeOnLan, token => AdbClient.ConnectAsync(host, port, approvalTimeout, token), ct);
    }
}
