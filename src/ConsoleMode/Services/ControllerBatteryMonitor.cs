using System.Runtime.InteropServices;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

/// <summary>
/// Reads the battery of the connected pads (issue #70) from Windows.Gaming.Input, XInput,
/// SonyHidReader and Windows' Bluetooth stack (BluetoothBatteryReader, issue #224). Polled every half a minute by the view model, never on the input path.
/// <see cref="Snapshot"/> is the last result, for any screen that wants to list it.
/// </summary>
public static class ControllerBatteryMonitor
{
    private static readonly object Gate = new();
    private static IReadOnlyList<BatteryReading> _snapshot = [];
    private static bool _loggedError;

    /// <summary>The batteries read by the last <see cref="Refresh"/> (empty before the first, or with no battery pad).</summary>
    public static IReadOnlyList<BatteryReading> Snapshot
    {
        get { lock (Gate) return _snapshot; }
    }

    /// <summary>Reads every source now. Blocks for a moment (XInput, WinRT): call it off the UI thread.</summary>
    public static IReadOnlyList<BatteryReading> Refresh()
    {
        var wgi = new List<(BatteryReading, bool)>();
        try
        {
            foreach (var raw in RawGameController.RawGameControllers)
            {
                var report = raw.TryGetBatteryReport();
                if (report is null) continue;
                if (ControllerBattery.FromWgi(report.RemainingCapacityInMilliwattHours, report.FullChargeCapacityInMilliwattHours, (int)report.Status) is not { } b) continue;
                wgi.Add((new BatteryReading(raw.NonRoamableId, raw.DisplayName, b.Percent, b.Charging, false, ControllerBattery.SourceWgi,
                        raw.HardwareVendorId, raw.HardwareProductId),
                    raw.HardwareVendorId == ControllerMapping.SonyVendorId));
            }
        }
        catch (Exception ex)
        {
            if (!_loggedError) AppLog.Write($"Controle: bateria via Windows.Gaming.Input indisponível: {ex.Message}");
            _loggedError = true;
        }

        var xinput = new List<BatteryReading>();
        for (uint i = 0; i < 4; i++)
        {
            try
            {
                if (XInputGetBatteryInformation(i, BatteryDevTypeGamepad, out var info) != 0) continue;
                if (ControllerBattery.FromXInput(info.BatteryType, info.BatteryLevel) is not { } percent) continue;
                xinput.Add(new BatteryReading($"xinput{i}", $"XInput {i + 1}", percent, false, true, ControllerBattery.SourceXInput));
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                break;
            }
        }

        IReadOnlyList<BatteryReading> bluetooth = [];
        try
        {
            bluetooth = BluetoothBatteryReader.Read();
            foreach (var reading in bluetooth)
                if (Remember(reading.Id))
                    AppLog.Write($"Controle: bateria pelo Bluetooth do Windows: {reading.Name} ({reading.VendorId:X4}:{reading.ProductId:X4}) {reading.Percent}%");
        }
        catch (Exception ex)
        {
            if (!_loggedBluetoothError) AppLog.Write($"Controle: bateria pelo Bluetooth do Windows indisponível: {ex.Message}");
            _loggedBluetoothError = true;
        }

        var sony = SonyHidReader.ReadBatteries();
        var all = ControllerBattery.Combine(sony, wgi, xinput, bluetooth);
        lock (Gate)
        {
            _snapshot = all;
            _sources = [.. sony, .. bluetooth, .. wgi.Select(w => w.Item1), .. xinput];
        }
        return all;
    }

    private static IReadOnlyList<BatteryReading> _sources = [];

    /// <summary>
    /// For the controller test: what every source said at the last refresh, and which reading is shown, so a pad whose
    /// sources disagree (issue #224) is visible at a glance.
    /// </summary>
    public static string DescribeSources()
    {
        IReadOnlyList<BatteryReading> sources, shown;
        lock (Gate)
        {
            sources = _sources;
            shown = _snapshot;
        }
        if (sources.Count == 0) return "Bateria: nenhuma fonte";
        return string.Join(Environment.NewLine, sources.Select(r =>
        {
            var ids = r.VendorId == 0 ? "" : $" ({r.VendorId:X4}:{r.ProductId:X4})";
            var used = shown.Contains(r) ? " ← mostrada" : "";
            return $"Bateria {r.Source}: {r.Name}{ids} {ControllerBattery.Format(r)}{(r.Charging ? ", carregando" : "")}{used}";
        }));
    }

    private static readonly HashSet<string> LoggedBluetooth = [];
    private static bool _loggedBluetoothError;

    /// <summary>True the first time a Bluetooth pad is seen, so the log names it once.</summary>
    private static bool Remember(string id)
    {
        lock (LoggedBluetooth) return LoggedBluetooth.Add(id);
    }

    private const byte BatteryDevTypeGamepad = 0x00;

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputBatteryInformation
    {
        public byte BatteryType;
        public byte BatteryLevel;
    }

    [DllImport("xinput1_4.dll")]
    private static extern uint XInputGetBatteryInformation(uint userIndex, byte devType, out XInputBatteryInformation information);
}
