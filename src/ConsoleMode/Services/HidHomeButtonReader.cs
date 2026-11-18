using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>
/// Reads the centre button (Home, Luna, Stadia…) of controllers that aren't Xbox or PlayStation, through the standard
/// HID usages (ControllerMapping.HomeUsages), so the controller shortcut to open Console Mode works with them
/// (issue #217). XInput never reports that button for them, even when it covers the rest of the pad. Only the
/// collections that declare one of those usages are opened, shared, so Windows, Steam and the game keep reading them.
/// The button may sit on the gamepad collection or on a sibling one of the same device (a "consumer control"), so
/// every collection of a game controller's vendor and product is looked at.
/// </summary>
public static class HidHomeButtonReader
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ushort> Held = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Reading = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Collections looked at already (with or without a centre button), so the scan doesn't reopen them every time.</summary>
    private static readonly HashSet<string> Checked = new(StringComparer.OrdinalIgnoreCase);
    private static CancellationTokenSource? _cts;
    private static int _users;

    /// <summary>Guide (0x0400) for each collection whose centre button is held right now, 0 for the others.</summary>
    public static IReadOnlyDictionary<string, ushort> ReadStates()
    {
        lock (Gate) return new Dictionary<string, ushort>(Held, StringComparer.OrdinalIgnoreCase);
    }

    public static void Acquire()
    {
        lock (Gate)
        {
            if (_users++ > 0) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(() => ScanLoopAsync(token));
        }
    }

    public static void Release()
    {
        lock (Gate)
        {
            if (_users == 0 || --_users > 0) return;
            _cts?.Cancel();
            _cts = null;
            Held.Clear();
            Checked.Clear();
        }
    }

    private static async Task ScanLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Scan(ct);
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: botão central (HID): {ex.Message}");
            }
            try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
        }
    }

    private static void Scan(CancellationToken ct)
    {
        var all = HidDevices.EnumeratePaths().Select(HidDevices.Query).OfType<HidInfo>().ToList();
        // The devices that are game controllers, by vendor and product: their other collections count too.
        var pads = all.Where(i => i.IsGameController && ControllerMapping.ReadsHomeFromHid(i.VendorId))
            .Select(i => (i.VendorId, i.ProductId)).ToHashSet();
        foreach (var info in all)
        {
            if (!pads.Contains((info.VendorId, info.ProductId))) continue;
            lock (Gate)
            {
                if (Reading.Contains(info.Path) || !Checked.Add(info.Path)) continue;
            }
            var declared = HidDevices.DeclaredButtons(info.Path, ControllerMapping.HomeUsages.Select(u => (u.Page, u.Id)));
            if (declared.Count == 0) continue;
            var usage = ControllerMapping.DescribeUsage(declared[0].Page, declared[0].Id);
            if (HidDevices.OpenForRead(info) is not { } reader)
            {
                AppLog.Write($"Controle: botão central em {info.VendorId:X4}:{info.ProductId:X4} ({usage}), mas o Windows não deixa ler esse dispositivo");
                continue;
            }
            AppLog.Write($"Controle: lendo o botão central pelo HID ({info.VendorId:X4}:{info.ProductId:X4}, {usage})");
            lock (Gate) Reading.Add(info.Path);
            _ = Task.Run(async () =>
            {
                await ReadLoopAsync(reader, ct);
                lock (Gate)
                {
                    Held.Remove(info.Path);
                    Reading.Remove(info.Path);
                    // Unplugged: when it comes back it is looked at again.
                    Checked.Remove(info.Path);
                }
            });
        }
    }

    private static async Task ReadLoopAsync(HidReader reader, CancellationToken ct)
    {
        var path = reader.Info.Path;
        try
        {
            await using var stream = new FileStream(reader.Handle, FileAccess.Read, 0, isAsync: true);
            var buffer = new byte[reader.Info.InputReportLength];
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                if (read <= 0) break;
                // A report of another id says nothing about the button: keep what it was.
                if (reader.Pressed(buffer, read) is not { } pressed) continue;
                var home = pressed.Any(u => ControllerMapping.IsHomeUsage(u.Page, u.Id));
                lock (Gate) Held[path] = home ? ControllerShortcuts.Guide : (ushort)0;
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
        {
            // Unplugged, or the watch stopped: the scan picks it up again.
        }
        finally
        {
            reader.Dispose();
        }
    }
}
