using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>
/// The battery Windows keeps for each Bluetooth game controller: the number Settings → Bluetooth shows (issue #224).
/// Windows stores it on the pad's Bluetooth device node, above the HID collection the game input comes from;
/// this walks up from every gamepad collection to find it. Pads on a cable or a dongle have no such node and are
/// left to the other sources.
/// </summary>
public static class BluetoothBatteryReader
{
    /// <summary>DEVPKEY {104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2: Bluetooth battery level, one byte, 0..100.</summary>
    private static readonly Guid BatteryLevelKey = new("104EA319-6EE2-4701-BD47-8DDBF425BBE5");
    private const uint BatteryLevelPid = 2;

    /// <summary>How far up the tree to look (HID collection → Bluetooth service → Bluetooth device, with room to spare).</summary>
    private const int MaxDepth = 6;

    /// <summary>Reads every Bluetooth pad now. Blocks briefly (device queries): call it off the UI thread.</summary>
    public static IReadOnlyList<BatteryReading> Read()
    {
        var list = new List<BatteryReading>();
        // A pad can expose several HID collections; each Bluetooth device is listed once.
        var seen = new HashSet<uint>();
        foreach (var path in HidDevices.EnumeratePaths())
        {
            if (HidDevices.Query(path) is not { IsGameController: true } info) continue;
            var node = HidDevices.DevNodeOf(path);
            for (var depth = 0; node != 0 && depth < MaxDepth; depth++, node = HidDevices.ParentOf(node))
            {
                if (HidDevices.GetByte(node, BatteryLevelKey, BatteryLevelPid) is not { } level) continue;
                if (!seen.Add(node)) break;
                if (ControllerBattery.FromBluetooth(level) is not { } percent) break;
                var id = HidDevices.GetInstanceId(node) ?? path;
                list.Add(new BatteryReading($"bt:{id}", HidDevices.GetName(node) ?? "Bluetooth", percent, false, false,
                    ControllerBattery.SourceBluetooth, info.VendorId, info.ProductId));
                break;
            }
        }
        return list;
    }
}
