using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// Single display commands through Windows' own APIs: CCD, ChangeDisplaySettingsEx and DDC/CI
/// (issue #91). <see cref="MonitorService"/> keeps the orchestration (retries, waits, fallbacks).
/// </summary>
internal sealed class DisplayBackend
{
    public List<MonitorInfo> ListMonitors()
    {
        var result = new List<MonitorInfo>();
        foreach (var m in NativeDisplays.ListMonitors())
        {
            DisplayIdentity.TryParseDevicePath(m.DevicePath, out var hardwareId, out var instanceId);
            var (driverKey, edid) = ReadRegistry(instanceId);

            var current = m.IsActive ? NativeWindows.GetCurrentDisplayMode(m.GdiName) : null;
            int maxW = edid.PreferredWidth, maxH = edid.PreferredHeight;
            if (m.IsActive)
            {
                foreach (var mode in NativeWindows.EnumerateDisplayModes(m.GdiName))
                {
                    if ((long)mode.Width * mode.Height <= (long)maxW * maxH) continue;
                    maxW = mode.Width;
                    maxH = mode.Height;
                }
            }
            var width = current?.Width ?? maxW;
            var height = current?.Height ?? maxH;
            var maxText = maxW > 0 ? $"{maxW} X {maxH}" : "";

            result.Add(new MonitorInfo
            {
                Name = m.GdiName,
                Resolution = current is not null ? $"{current.Width} X {current.Height}" : maxText.Length > 0 ? maxText : "N/A",
                MaximumResolution = maxText,
                MaxWidth = maxW,
                MaxHeight = maxH,
                Width = width,
                Height = height,
                Frequency = current?.Frequency > 0 ? current.Frequency.ToString() : "",
                Colors = current?.BitsPerPel > 0 ? current.BitsPerPel.ToString() : "",
                IsPrimary = m.IsActive && NativeDisplays.IsPrimary(m.GdiName),
                IsActive = m.IsActive,
                IsDisconnected = !m.IsActive,
                MonitorName = !string.IsNullOrWhiteSpace(m.FriendlyName) ? m.FriendlyName : edid.Name,
                ShortId = hardwareId,
                MonitorId = DisplayIdentity.MonitorId(hardwareId, driverKey),
                SerialNumber = edid.Serial,
                LeftTop = m.IsActive ? $"{m.PositionX}, {m.PositionY}" : ""
            });
        }
        return result;
    }

    private static (string? DriverKey, EdidInfo Edid) ReadRegistry(string instanceId)
    {
        var empty = new EdidInfo("", "", 0, 0);
        if (string.IsNullOrWhiteSpace(instanceId)) return (null, empty);
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{instanceId}");
            if (key is null) return (null, empty);
            using var parameters = key.OpenSubKey("Device Parameters");
            return (key.GetValue("Driver") as string, DisplayIdentity.ParseEdid(parameters?.GetValue("EDID") as byte[]));
        }
        catch (Exception ex)
        {
            AppLog.Write($"Telas: registro de {instanceId}: {ex.Message}");
            return (null, empty);
        }
    }

    public void SaveLayout(string path)
    {
        var entries = ListMonitors().Select(m =>
        {
            var current = m.IsActive ? NativeWindows.GetCurrentDisplayMode(m.Name) : null;
            MonitorService.ParseLeftTop(m.LeftTop, out var x, out var y);
            return new LayoutEntry(m.Name, m.MonitorId, m.SerialNumber, current?.BitsPerPel ?? 0,
                current?.Width ?? 0, current?.Height ?? 0, current?.Frequency ?? 0, x ?? 0, y ?? 0);
        });
        File.WriteAllText(path, DisplayIdentity.FormatLayout(entries));
    }

    public void LoadLayout(string path)
    {
        var specs = ResolveLayout(DisplayIdentity.ParseLayout(File.ReadAllLines(path)));
        foreach (var (name, spec) in specs)
        {
            int.TryParse(spec.GetValueOrDefault("Width"), out var w);
            int.TryParse(spec.GetValueOrDefault("Height"), out var h);
            if (w <= 0 || h <= 0) continue;
            int.TryParse(spec.GetValueOrDefault("DisplayFrequency"), out var f);
            int.TryParse(spec.GetValueOrDefault("BitsPerPixel"), out var bits);
            int? x = int.TryParse(spec.GetValueOrDefault("PositionX"), out var px) ? px : null;
            int? y = int.TryParse(spec.GetValueOrDefault("PositionY"), out var py) ? py : null;
            NativeDisplays.QueueMode(name, w, h, f, bits, x, y);
        }
        var code = NativeDisplays.ApplyPending();
        AppLog.Write($"Telas: layout restaurado => {code}");
    }

    public Dictionary<string, Dictionary<string, string>> ResolveLayout(Dictionary<string, Dictionary<string, string>> specs) =>
        DisplayIdentity.RemapLayoutNames(specs, ListMonitors().Select(m => (m.MonitorId, m.Name)));

    public void Enable(IReadOnlyList<string> names) => NativeDisplays.Enable(names);
    public void Disable(IReadOnlyList<string> names) => NativeDisplays.Disable(names);
    public void SetPrimary(string name) => CcdHelper.SetPrimary(name);
    public void PowerOn(string name) => NativeDisplays.SetPower(name, on: true);
    public void PowerOff(string name) => NativeDisplays.SetPower(name, on: false);

    public void SetMode(string name, int width, int height, int frequency, string? colors, int? x, int? y, bool primary)
    {
        int.TryParse(colors, out var bits);
        NativeDisplays.QueueMode(name, width, height, frequency, bits, x, y);
        NativeDisplays.ApplyPending();
        if (primary) CcdHelper.SetPrimary(name);
    }

    public void MoveProcessWindows(string monitorName, string processName, ScreenRect rect) =>
        NativeDisplays.MoveProcessWindows(processName, rect);
}
