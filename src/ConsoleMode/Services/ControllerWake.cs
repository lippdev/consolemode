using System.Text;
using System.Text.Json;

namespace ConsoleMode.Services;

public enum WakeDeviceKind { None, XboxAdapter, Bluetooth, Controller }

/// <summary>A device Windows can let wake the PC (WMI MSPower_DeviceWakeEnable) and whether it may now.</summary>
public sealed record WakeDevice(string InstanceName, string Name, string PnpClass, bool Armed)
{
    public WakeDeviceKind Kind => ControllerWake.Classify(InstanceName, PnpClass);
}

/// <summary>
/// Waking the PC from sleep with the controller (issue #155): what wakes it is the device the
/// controller talks through (the Xbox Wireless Adapter, the Bluetooth radio, or the controller
/// itself on a cable), and Windows only lets it once "Allow this device to wake the computer" is on.
/// Devices are told apart by their instance id, which, unlike the name, isn't translated.
/// </summary>
public static class ControllerWake
{
    // Xbox Wireless Adapter for Windows: the original, the slim one, and the newer revisions.
    private static readonly string[] XboxAdapterIds = ["PID_02E6", "PID_02FE", "PID_02F9", "PID_091E"];

    // Sony, Nintendo, 8BitDo, Valve, Logitech gamepads, PowerA, PDP, Hori, Nacon. Xbox pads are
    // found by their class instead: Microsoft also makes the keyboards and mice.
    private static readonly string[] GamepadVendors =
        ["VID_054C", "VID_057E", "VID_2DC8", "VID_28DE", "VID_046D&PID_C2", "VID_20D6", "VID_0E6F", "VID_0F0D", "VID_146B"];

    public static WakeDeviceKind Classify(string? instanceName, string? pnpClass)
    {
        var id = (instanceName ?? "").ToUpperInvariant();
        var cls = pnpClass ?? "";
        if (id.Length == 0) return WakeDeviceKind.None;

        if (id.StartsWith(@"USB\VID_045E&", StringComparison.Ordinal) && XboxAdapterIds.Any(id.Contains))
            return WakeDeviceKind.XboxAdapter;

        // The radio itself, not the devices paired through it (BTHENUM\…, BTHLE\…).
        if (cls.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase) && (id.StartsWith(@"USB\", StringComparison.Ordinal) || id.StartsWith(@"PCI\", StringComparison.Ordinal)))
            return WakeDeviceKind.Bluetooth;

        // XInput pads show up as HID collections marked IG_; Xbox pads on a cable as their own class.
        if (id.Contains("&IG_", StringComparison.Ordinal) || cls.Equals("XnaComposite", StringComparison.OrdinalIgnoreCase)
            || cls.Equals("XboxComposite", StringComparison.OrdinalIgnoreCase))
            return WakeDeviceKind.Controller;
        if ((id.StartsWith(@"USB\", StringComparison.Ordinal) || id.StartsWith(@"HID\", StringComparison.Ordinal))
            && GamepadVendors.Any(id.Contains))
            return WakeDeviceKind.Controller;

        return WakeDeviceKind.None;
    }

    /// <summary>The controller-related devices, the adapter first, then Bluetooth, then controllers.</summary>
    public static List<WakeDevice> Relevant(IEnumerable<WakeDevice> devices) =>
        [.. devices.Where(d => d.Kind != WakeDeviceKind.None).OrderBy(d => d.Kind).ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>Reads the JSON the query script prints (one object or an array).</summary>
    public static List<WakeDevice> Parse(string? json)
    {
        var list = new List<WakeDevice>();
        if (string.IsNullOrWhiteSpace(json)) return list;
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToList() : [doc.RootElement];
        foreach (var item in items)
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var instance = Text(item, "InstanceName");
            if (string.IsNullOrWhiteSpace(instance)) continue;
            var armed = item.TryGetProperty("Enable", out var enable) && enable.ValueKind == JsonValueKind.True;
            var name = Text(item, "Name");
            list.Add(new WakeDevice(instance, string.IsNullOrWhiteSpace(name) ? DeviceId(instance) : name, Text(item, "PnpClass"), armed));
        }
        return list;
    }

    /// <summary>WMI names wake entries after the device with an "_N" suffix.</summary>
    public static string DeviceId(string instanceName)
    {
        var cut = instanceName.LastIndexOf('_');
        return cut > 0 && cut < instanceName.Length - 1 && instanceName[(cut + 1)..].All(char.IsAsciiDigit)
            ? instanceName[..cut]
            : instanceName;
    }

    /// <summary>Lists every device that can wake the PC, with its name and class, as JSON.</summary>
    public const string QueryScript = """
        [Console]::OutputEncoding = [Text.Encoding]::UTF8
        $ErrorActionPreference = 'Stop'
        $pnp = @{}
        Get-CimInstance Win32_PnPEntity -Property DeviceID,Name,PNPClass | ForEach-Object { $pnp[$_.DeviceID] = $_ }
        $rows = @(Get-CimInstance -Namespace root\wmi -ClassName MSPower_DeviceWakeEnable | ForEach-Object {
            $d = $pnp[($_.InstanceName -replace '_\d+$', '')]
            [pscustomobject]@{ InstanceName = $_.InstanceName; Name = $d.Name; PnpClass = $d.PNPClass; Enable = [bool]$_.Enable }
        })
        ConvertTo-Json -InputObject $rows -Compress
        """;

    /// <summary>Turns on "may wake the computer" for the given WMI entries (needs an elevated PowerShell).</summary>
    public static string EnableScript(IEnumerable<string> instanceNames)
    {
        var quoted = string.Join(",", instanceNames.Select(n => "'" + n.Replace("'", "''") + "'"));
        return "$ErrorActionPreference = 'Stop'\n"
             + $"$ids = @({quoted})\n"
             + "Get-CimInstance -Namespace root\\wmi -ClassName MSPower_DeviceWakeEnable | Where-Object { $ids -contains $_.InstanceName } | Set-CimInstance -Property @{ Enable = $true }\n";
    }

    /// <summary>The argument for powershell -EncodedCommand (UTF-16LE in Base64).</summary>
    public static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    private static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
