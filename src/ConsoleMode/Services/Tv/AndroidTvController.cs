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

    public async Task RestoreAsync(TvControlConfig config, CancellationToken ct)
    {
        var commands = AdbProtocol.RestoreCommands(config.RestoreCommand, config.TurnOffOnRestore);
        if (commands.Count == 0) return;
        await using var adb = await ConnectAsync(config, TimeSpan.FromSeconds(5), wakeOnLan: false, ct);
        for (var i = 0; i < commands.Count; i++)
        {
            // A moment between the user's command and standby, or the TV may drop the second while it switches.
            if (i > 0) await Task.Delay(1000, ct);
            await adb.ShellAsync(commands[i], ct);
        }
    }

    private static Task<AdbClient> ConnectAsync(TvControlConfig config, TimeSpan approvalTimeout, bool wakeOnLan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new TvControlException(LocalizationService.Get("TvHostMissing"));
        var (host, port) = AdbProtocol.ParseEndpoint(config.Host);
        return TvNetwork.ConnectAsync(config, wakeOnLan, token => AdbClient.ConnectAsync(host, port, approvalTimeout, token), ct);
    }
}
