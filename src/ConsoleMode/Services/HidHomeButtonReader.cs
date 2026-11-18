using System.Text;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>
/// Reads the centre button (Home, Luna, Stadia…) of controllers that aren't Xbox or PlayStation, through the standard
/// HID usages (ControllerMapping.HomeUsages), so the controller shortcut to open Console Mode works with them
/// (issue #217). XInput never reports that button for them, even when it covers the rest of the pad. Only the
/// collections that declare one of those usages are opened, shared, so Windows, Steam and the game keep reading them.
/// The button may sit on the gamepad collection or on a sibling one of the same device (a "consumer control"), so
/// every collection of a game controller's vendor and product is looked at.
/// While the controller test is open (<see cref="AcquireDiagnostics"/>), the other collections of those devices are
/// read too, only to show which usages each button sends.
/// </summary>
public static class HidHomeButtonReader
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ushort> Held = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Collection> Open = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Collections looked at already, so the scan doesn't query their buttons every time.</summary>
    private static readonly Dictionary<string, Collection> Seen = new(StringComparer.OrdinalIgnoreCase);
    private static CancellationTokenSource? _cts;
    private static int _users;
    private static int _diagnosticUsers;

    /// <summary>One collection of a controller: what it declares, whether it's being read, and (for the test) what is pressed.</summary>
    private sealed class Collection(HidInfo info, string? homeUsage)
    {
        public HidInfo Info { get; } = info;
        public string? HomeUsage { get; } = homeUsage;
        public bool Unreadable { get; set; }
        public CancellationTokenSource? Stop { get; set; }
        public IReadOnlyList<(ushort Page, ushort Id)> Pressed { get; set; } = [];
    }

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
            _diagnosticUsers = 0;
            Held.Clear();
            Seen.Clear();
        }
    }

    /// <summary>The controller test: read every collection of the controllers, to show what each button sends.</summary>
    public static void AcquireDiagnostics()
    {
        lock (Gate) _diagnosticUsers++;
        Acquire();
    }

    public static void ReleaseDiagnostics()
    {
        lock (Gate)
        {
            if (_diagnosticUsers > 0 && --_diagnosticUsers == 0)
                // The collections opened only for the test close; the centre buttons stay read.
                foreach (var c in Open.Values.Where(c => c.HomeUsage is null)) c.Stop?.Cancel();
        }
        Release();
    }

    /// <summary>
    /// For the controller test: each collection of the controllers (not Xbox or PlayStation) with its centre-button
    /// usage, whether it's read, and the usages pressed right now.
    /// </summary>
    public static string Describe()
    {
        List<Collection> all;
        lock (Gate) all = [.. Seen.Values];
        if (all.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var c in all.OrderBy(c => c.Info.VendorId).ThenBy(c => c.Info.ProductId).ThenBy(c => c.Info.UsagePage))
        {
            var i = c.Info;
            bool reading;
            IReadOnlyList<(ushort Page, ushort Id)> pressed;
            lock (Gate)
            {
                reading = Open.ContainsKey(i.Path);
                pressed = c.Pressed;
            }
            var home = c.HomeUsage is null ? "sem botão central padrão" : $"botão central = {c.HomeUsage}";
            var state = c.Unreadable ? "o Windows não deixa ler" : reading ? "lido" : "não lido";
            var now = pressed.Count == 0 ? "" : " · apertado: " + string.Join(", ", pressed.Select(u => ControllerMapping.DescribeUsage(u.Page, u.Id)));
            sb.AppendLine($"HID {i.VendorId:X4}:{i.ProductId:X4} {ControllerMapping.DescribeUsage(i.UsagePage, i.Usage)}: {home}; {state}{now}");
        }
        return sb.ToString().TrimEnd();
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
        bool diagnostics;
        lock (Gate)
        {
            diagnostics = _diagnosticUsers > 0;
            // A collection opened for the test by a scan that raced its closing: close it now.
            if (!diagnostics)
                foreach (var c in Open.Values.Where(c => c.HomeUsage is null)) c.Stop?.Cancel();
        }
        foreach (var info in all)
        {
            if (!pads.Contains((info.VendorId, info.ProductId))) continue;
            Collection? collection;
            lock (Gate)
            {
                if (Open.ContainsKey(info.Path)) continue;
                Seen.TryGetValue(info.Path, out collection);
            }
            if (collection is null)
            {
                var declared = HidDevices.DeclaredButtons(info.Path, ControllerMapping.HomeUsages.Select(u => (u.Page, u.Id)));
                collection = new Collection(info, declared.Count == 0 ? null : ControllerMapping.DescribeUsage(declared[0].Page, declared[0].Id));
                lock (Gate) Seen[info.Path] = collection;
            }
            // Without a centre button, a collection is only read for the controller test.
            if (collection.Unreadable || (collection.HomeUsage is null && !diagnostics)) continue;
            if (HidDevices.OpenForRead(info) is not { } reader)
            {
                collection.Unreadable = true;
                if (collection.HomeUsage is not null)
                    AppLog.Write($"Controle: botão central em {info.VendorId:X4}:{info.ProductId:X4} ({collection.HomeUsage}), mas o Windows não deixa ler esse dispositivo");
                continue;
            }
            var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
            lock (Gate)
            {
                // The test may have closed since this scan started: then a collection without a centre button isn't wanted.
                if (collection.HomeUsage is null && _diagnosticUsers == 0)
                {
                    stop.Dispose();
                    reader.Dispose();
                    continue;
                }
                collection.Stop = stop;
                Open[info.Path] = collection;
            }
            if (collection.HomeUsage is not null)
                AppLog.Write($"Controle: lendo o botão central pelo HID ({info.VendorId:X4}:{info.ProductId:X4}, {collection.HomeUsage})");
            _ = Task.Run(async () =>
            {
                await ReadLoopAsync(reader, collection, stop.Token);
                stop.Dispose();
                lock (Gate)
                {
                    Held.Remove(info.Path);
                    Open.Remove(info.Path);
                    collection.Pressed = [];
                    // Unplugged: when it comes back it is looked at again.
                    if (!ct.IsCancellationRequested && collection.HomeUsage is not null) Seen.Remove(info.Path);
                }
            });
        }
    }

    private static async Task ReadLoopAsync(HidReader reader, Collection collection, CancellationToken ct)
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
                // A report of another id says nothing about the buttons: keep what they were.
                if (reader.Pressed(buffer, read) is not { } pressed) continue;
                lock (Gate)
                {
                    collection.Pressed = pressed;
                    if (collection.HomeUsage is not null)
                        Held[path] = pressed.Any(u => ControllerMapping.IsHomeUsage(u.Page, u.Id)) ? ControllerShortcuts.Guide : (ushort)0;
                }
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
