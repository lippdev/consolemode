using System.Runtime.InteropServices;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

/// <summary>
/// Reads the battery of the connected pads (issue #70) from Windows.Gaming.Input, XInput and
/// SonyHidReader. Polled every half a minute by the view model, never on the input path.
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
                wgi.Add((new BatteryReading(raw.NonRoamableId, raw.DisplayName, b.Percent, b.Charging, false, ControllerBattery.SourceWgi),
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

        var all = ControllerBattery.Combine(SonyHidReader.ReadBatteries(), wgi, xinput);
        lock (Gate) _snapshot = all;
        return all;
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
