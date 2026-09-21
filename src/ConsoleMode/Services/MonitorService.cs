using System.Text.Json;
using System.Text.RegularExpressions;
using ConsoleMode.Models;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

public sealed class MonitorService
{
    private List<MonitorInfo>? _cache;
    private readonly Dictionary<string, List<DisplayModeOption>> _modesCache = new(StringComparer.OrdinalIgnoreCase);

    public void ClearCache()
    {
        _cache = null;
        _modesCache.Clear();
    }

    public int InvokeMmt(params string[] args)
    {
        if (!AppPaths.HasMmt) throw new InvalidOperationException($"MultiMonitorTool.exe não encontrado em {AppPaths.MmtPath}");
        return ProcessRunner.Run(AppPaths.MmtPath, args);
    }

    public IReadOnlyList<MonitorInfo> GetMonitors(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache is not null) return _cache;
        if (!AppPaths.HasMmt)
        {
            _cache = [];
            return _cache;
        }

        var csvPath = Path.Combine(Path.GetTempPath(), "consolemode_monitors.csv");
        try
        {
            InvokeMmt("/HideInactiveMonitors", "0", "/scomma", csvPath);
            if (!File.Exists(csvPath))
            {
                _cache = [];
                return _cache;
            }

            var rows = CsvReader.Read(csvPath);
            var winMap = BuildDisplayNumberMap(rows);
            var monitors = new List<MonitorInfo>();
            foreach (var row in rows)
            {
                var name = row.Get("Name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var isActive = string.Equals(row.Get("Active"), "Yes", StringComparison.OrdinalIgnoreCase);
                var maxResolution = row.Get("Maximum Resolution");
                var resolution = row.Get("Resolution");
                if (string.IsNullOrWhiteSpace(resolution))
                    resolution = string.IsNullOrWhiteSpace(maxResolution) ? "N/A" : maxResolution;

                ParseRes(resolution, out var width, out var height);
                ParseRes(maxResolution, out var maxW, out var maxH);
                if (maxW <= 0 && width > 0) { maxW = width; maxH = height; }

                monitors.Add(new MonitorInfo
                {
                    Name = name,
                    WindowsDisplayNumber = winMap.GetValueOrDefault(name),
                    Resolution = resolution,
                    MaximumResolution = maxResolution,
                    MaxWidth = maxW,
                    MaxHeight = maxH,
                    Width = width,
                    Height = height,
                    Frequency = row.Get("Frequency"),
                    Colors = row.Get("Colors"),
                    IsPrimary = string.Equals(row.Get("Primary"), "Yes", StringComparison.OrdinalIgnoreCase),
                    IsActive = isActive,
                    IsDisconnected = string.Equals(row.Get("Disconnected"), "Yes", StringComparison.OrdinalIgnoreCase) || !isActive,
                    MonitorName = row.Get("Monitor Name"),
                    ShortId = row.Get("Short Monitor ID"),
                    LeftTop = row.Get("Left-Top")
                });
            }

            _cache = [.. monitors.OrderBy(m => m.WindowsDisplayNumber > 0 ? m.WindowsDisplayNumber : 999).ThenBy(m => m.Name)];
            return _cache;
        }
        finally
        {
            try { File.Delete(csvPath); } catch { /* ignore */ }
        }
    }

    public string? GetPrimaryName()
    {
        var monitors = GetMonitors(true);
        return monitors.FirstOrDefault(m => m.IsPrimary && m.IsActive)?.Name
               ?? monitors.FirstOrDefault(m => m.IsPrimary)?.Name;
    }

    public void SaveBackup()
    {
        InvokeMmt("/SaveConfig", AppPaths.BackupMonitorConfig);
    }

    public void SaveBackupMeta(MonitorBackupMeta meta)
    {
        File.WriteAllText(AppPaths.BackupMonitorMeta, JsonSerializer.Serialize(meta, JsonUtil.Options));
    }

    public MonitorRestoreContext GetRestoreContext(ConsoleRuntimeState state)
    {
        var ctx = new MonitorRestoreContext
        {
            OriginalPrimary = state.OriginalPrimary,
            FocusWasInactive = state.FocusWasInactive,
            FocusMonitor = state.FocusMonitor,
            HideMonitors = [.. state.HideMonitors],
            HideStrategy = state.HideStrategy
        };

        if (!File.Exists(AppPaths.BackupMonitorMeta)) return ctx;
        try
        {
            var meta = JsonSerializer.Deserialize<MonitorBackupMeta>(File.ReadAllText(AppPaths.BackupMonitorMeta), JsonUtil.Options);
            if (meta is null) return ctx;
            if (!string.IsNullOrWhiteSpace(meta.OriginalPrimary)) ctx.OriginalPrimary = meta.OriginalPrimary;
            ctx.FocusWasInactive = meta.FocusWasInactive;
            if (!string.IsNullOrWhiteSpace(meta.FocusMonitor)) ctx.FocusMonitor = meta.FocusMonitor;
            if (meta.HideMonitors.Count > 0) ctx.HideMonitors = meta.HideMonitors;
            if (!string.IsNullOrWhiteSpace(meta.HideStrategy)) ctx.HideStrategy = meta.HideStrategy;
        }
        catch { /* keep in-memory context */ }

        return ctx;
    }

    public RestoreResult RestoreBackup(ConsoleRuntimeState state)
    {
        var result = new RestoreResult();
        if (!File.Exists(AppPaths.BackupMonitorConfig))
        {
            result.Success = false;
            result.Issues.Add($"Backup de monitores não encontrado: {AppPaths.BackupMonitorConfig}");
            return result;
        }

        var ctx = GetRestoreContext(state);
        var backupSpecs = GetBackupMonitorSpecs(AppPaths.BackupMonitorConfig);
        AppLog.Write($"Restore: iniciando (primário={ctx.OriginalPrimary}, foco={ctx.FocusMonitor}, focoInativo={ctx.FocusWasInactive}, esconder={string.Join('+', ctx.HideMonitors)})");

        if (ctx.HideStrategy == "turnOff" && ctx.HideMonitors.Count > 0)
        {
            foreach (var name in ctx.HideMonitors.Distinct())
                InvokeMmt("/TurnOn", name);
            Thread.Sleep(300);
        }

        var allDeviceNames = backupSpecs.Values
            .Select(s => s.GetValueOrDefault("Name") ?? "")
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var monitorsToVerify = backupSpecs
            .Where(kv => IsBackupSpecActive(kv.Value))
            .Select(kv => kv.Key)
            .ToList();

        if (allDeviceNames.Count > 0)
        {
            var enableArgs = new List<string> { "/enable" };
            enableArgs.AddRange(allDeviceNames);
            InvokeMmt(enableArgs.ToArray());

            if (monitorsToVerify.Count > 0 && !WaitMonitorsActive(monitorsToVerify))
            {
                foreach (var name in monitorsToVerify)
                    InvokeMmt("/enable", name);
                if (!WaitMonitorsActive(monitorsToVerify, 3000))
                {
                    AppLog.Write($"Restore: /enable não confirmou todos ({string.Join('+', monitorsToVerify)}); usando ExtendAll");
                    CcdHelper.ExtendAll();
                    if (!WaitMonitorsActive(monitorsToVerify))
                        result.Issues.Add($"Nem todos os monitores foram reativados: {string.Join(", ", monitorsToVerify)}");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(ctx.OriginalPrimary))
        {
            backupSpecs.TryGetValue(ctx.OriginalPrimary, out var primarySpec);
            if (!RestorePrimaryWithRetry(ctx.OriginalPrimary, primarySpec))
                result.Issues.Add($"Não foi possível restaurar o primário para {ctx.OriginalPrimary}");
        }

        InvokeMmt("/LoadConfig", AppPaths.BackupMonitorConfig);
        Thread.Sleep(800);

        if (!string.IsNullOrWhiteSpace(ctx.OriginalPrimary))
        {
            backupSpecs.TryGetValue(ctx.OriginalPrimary, out var primarySpec);
            if (!RestorePrimaryWithRetry(ctx.OriginalPrimary, primarySpec))
                result.Issues.Add($"Primário não confirmado em {ctx.OriginalPrimary} após LoadConfig");
        }

        var monitorsToDisable = backupSpecs
            .Where(kv => !IsBackupSpecActive(kv.Value))
            .Select(kv => kv.Key)
            .ToList();
        if (ctx.FocusWasInactive && !string.IsNullOrWhiteSpace(ctx.FocusMonitor) &&
            !monitorsToDisable.Contains(ctx.FocusMonitor, StringComparer.OrdinalIgnoreCase))
        {
            monitorsToDisable.Add(ctx.FocusMonitor);
        }

        foreach (var name in monitorsToDisable)
        {
            if (string.Equals(name, ctx.OriginalPrimary, StringComparison.OrdinalIgnoreCase)) continue;
            if (!DisableWithRetry(name))
                result.Issues.Add($"Não foi possível desconectar {name}");
        }

        ClearCache();
        result.Success = result.Issues.Count == 0;
        AppLog.Write(result.Success ? "Restore: concluído com sucesso" : $"Restore: problemas: {string.Join(" | ", result.Issues)}");
        return result;
    }

    public void SetPrimary(string monitorName)
    {
        InvokeMmt("/SetPrimary", monitorName);
        if (WaitPrimary(monitorName, 2000)) return;
        CcdHelper.SetPrimary(monitorName);
        WaitPrimary(monitorName, 3000);
    }

    public void EnableMonitors(IReadOnlyList<string> names, bool windowsEnable)
    {
        if (names.Count == 0) return;
        if (windowsEnable)
        {
            var args = new List<string> { "/enable" };
            args.AddRange(names);
            InvokeMmt(args.ToArray());
            MarkActive(names);
            return;
        }

        foreach (var name in names)
        {
            InvokeMmt("/TurnOn", name);
            InvokeMmt("/enable", name);
        }
        MarkActive(names);
    }

    public void DisableWindows(IReadOnlyList<string> names)
    {
        if (names.Count == 0) return;
        var args = new List<string> { "/disable" };
        args.AddRange(names);
        InvokeMmt(args.ToArray());
    }

    public void DisableDdc(IReadOnlyList<string> names)
    {
        foreach (var name in names)
            InvokeMmt("/TurnOff", name);
    }

    public void ApplyFocusMode(string focusMonitor, SavedDisplayMode mode)
    {
        var monitors = GetMonitors(true);
        var focusInfo = monitors.FirstOrDefault(m => m.Name == focusMonitor);
        ParseLeftTop(focusInfo?.LeftTop, out var posX, out var posY);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            SetMonitorMode(focusMonitor, mode.Width, mode.Height, mode.Frequency, focusInfo?.Colors, posX, posY, primary: true);
            Thread.Sleep(400);
            if (IsCurrentMode(focusMonitor, mode.Width, mode.Height, mode.Frequency)) break;
            Thread.Sleep(400);
        }

        if (!IsCurrentMode(focusMonitor, mode.Width, mode.Height, mode.Frequency))
            AppLog.Write($"Não foi possível aplicar {mode.Width}x{mode.Height}@{mode.Frequency} em {focusMonitor}");

        SetPrimary(focusMonitor);
        Thread.Sleep(300);
    }

    public List<DisplayModeOption> GetDisplayModes(string monitorName, MonitorInfo? monitor, bool forceRefresh = false)
    {
        if (!forceRefresh && _modesCache.TryGetValue(monitorName, out var cached)) return cached;

        var live = new List<DisplayModeOption>();
        try
        {
            foreach (var mode in NativeWindows.EnumerateDisplayModes(monitorName))
                live.Add(ToOption(mode.Width, mode.Height, mode.Frequency));
        }
        catch { /* native enum can fail for disconnected displays */ }

        if (live.Count > 0) PersistModes(monitorName, live);

        var persisted = LoadPersistedModes().GetValueOrDefault(monitorName) ?? [];
        var modes = MergeModes(live, persisted, " (cache)");
        if (modes.Count == 0)
        {
            modes = FallbackModes(monitor);
        }
        else if (live.Count == 0 && monitor is { IsActive: false })
        {
            modes = MergeModes(modes, FallbackModes(monitor), " (estimado)");
        }

        _modesCache[monitorName] = modes;
        return modes;
    }

    public ScreenRect? GetMonitorRect(string monitorName, ConsoleRuntimeState state)
    {
        var screen = DisplayScreens.GetBounds(monitorName);
        if (screen is not null) return screen;
        if (state.FocusMonitorRect is not null && state.FocusMonitor == monitorName) return state.FocusMonitorRect;

        var info = GetMonitors().FirstOrDefault(m => m.Name == monitorName)
                   ?? GetMonitors(true).FirstOrDefault(m => m.Name == monitorName);
        return RectFromInfo(info);
    }

    public bool UpdateFocusRect(string monitorName, ConsoleRuntimeState state, bool allowMmtFallback)
    {
        var screen = DisplayScreens.GetBounds(monitorName);
        if (screen is not null)
        {
            state.FocusMonitorRect = screen;
            return true;
        }

        if (!allowMmtFallback) return false;
        var rect = RectFromInfo(GetMonitors().FirstOrDefault(m => m.Name == monitorName));
        if (rect is null) return false;
        state.FocusMonitorRect = rect;
        return true;
    }

    public void MoveWindowViaMmt(string monitorName, ScreenRect rect, string processName)
    {
        InvokeMmt(
            "/MoveWindow", monitorName,
            "Process", processName,
            "/WindowLeft", rect.X.ToString(),
            "/WindowTop", rect.Y.ToString(),
            "/WindowWidth", rect.Width.ToString(),
            "/WindowHeight", rect.Height.ToString());
    }

    private void SetMonitorMode(string name, int width, int height, int frequency, string? colors, int? x, int? y, bool primary)
    {
        var spec = primary ? $"Name={name} Primary=Yes" : $"Name={name}";
        if (width > 0) spec += $" Width={width}";
        if (height > 0) spec += $" Height={height}";
        if (frequency > 0) spec += $" DisplayFrequency={frequency}";
        if (!string.IsNullOrWhiteSpace(colors)) spec += $" BitsPerPixel={colors}";
        if (x is not null) spec += $" PositionX={x}";
        if (y is not null) spec += $" PositionY={y}";
        InvokeMmt("/SetMonitors", spec);
    }

    private bool IsCurrentMode(string name, int width, int height, int frequency)
    {
        var current = NativeWindows.GetCurrentDisplayMode(name);
        if (current is null) return false;
        if (width > 0 && current.Width != width) return false;
        if (height > 0 && current.Height != height) return false;
        if (frequency > 0 && current.Frequency != frequency) return false;
        return true;
    }

    private bool RestorePrimaryWithRetry(string monitorName, Dictionary<string, string>? spec, int maxAttempts = 3)
    {
        if (GetPrimaryName() == monitorName) return true;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt == 1)
            {
                CcdHelper.SetPrimary(monitorName);
            }
            else if (attempt == 2 && spec is not null)
            {
                SetPrimaryFromSpec(monitorName, spec);
            }
            else
            {
                InvokeMmt("/SetPrimary", monitorName);
            }

            if (WaitPrimary(monitorName)) return true;
        }
        return false;
    }

    private void SetPrimaryFromSpec(string monitorName, Dictionary<string, string> spec)
    {
        if (int.TryParse(spec.GetValueOrDefault("Width"), out var w) &&
            int.TryParse(spec.GetValueOrDefault("Height"), out var h) &&
            w > 0 && h > 0)
        {
            var freq = spec.GetValueOrDefault("DisplayFrequency") ?? spec.GetValueOrDefault("Frequency") ?? "";
            int.TryParse(freq, out var frequency);
            SetMonitorMode(monitorName, w, h, frequency, spec.GetValueOrDefault("BitsPerPixel"), null, null, true);
            return;
        }
        InvokeMmt("/SetPrimary", monitorName);
    }

    private bool DisableWithRetry(string monitorName, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt == 1) InvokeMmt("/disable", monitorName);
            else
            {
                var code = CcdHelper.DetachDisplay(monitorName);
                AppLog.Write($"Disable: fallback CCD DetachDisplay({monitorName}) => {code}");
            }
            if (WaitInactive(monitorName)) return true;
        }
        return false;
    }

    private bool WaitMonitorsActive(IReadOnlyList<string> names, int timeoutMs = 6000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            var monitors = GetMonitors(true);
            if (names.All(n => monitors.Any(m => m.Name == n && m.IsActive))) return true;
            Thread.Sleep(400);
        } while (DateTime.UtcNow < deadline);
        return false;
    }

    private bool WaitPrimary(string name, int timeoutMs = 4000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            if (GetPrimaryName() == name) return true;
            Thread.Sleep(400);
        } while (DateTime.UtcNow < deadline);
        return false;
    }

    private bool WaitInactive(string name, int timeoutMs = 4000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            var monitor = GetMonitors(true).FirstOrDefault(m => m.Name == name);
            if (monitor is null || !monitor.IsActive) return true;
            Thread.Sleep(400);
        } while (DateTime.UtcNow < deadline);
        return false;
    }

    private void MarkActive(IReadOnlyList<string> names)
    {
        if (_cache is null) return;
        foreach (var m in _cache)
        {
            if (!names.Contains(m.Name, StringComparer.OrdinalIgnoreCase)) continue;
            m.IsActive = true;
            m.IsDisconnected = false;
        }
    }

    private static Dictionary<string, int> BuildDisplayNumberMap(List<Dictionary<string, string>> rows)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ordered = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Get("Name")))
            .OrderBy(r => string.Equals(r.Get("Active"), "Yes", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
        var n = 1;
        foreach (var row in ordered)
        {
            var name = row.Get("Name");
            if (map.ContainsKey(name)) continue;
            map[name] = n++;
        }
        return map;
    }

    private static Dictionary<string, Dictionary<string, string>> GetBackupMonitorSpecs(string path)
    {
        var specs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;
        string? currentName = null;
        foreach (var line in File.ReadAllLines(path))
        {
            if (Regex.IsMatch(line, @"^\[Monitor\d+\]"))
            {
                if (currentName is not null && current is not null) specs[currentName] = current;
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                currentName = null;
                continue;
            }

            var m = Regex.Match(line, @"^Name=(.+)$");
            if (m.Success && current is not null)
            {
                currentName = m.Groups[1].Value.Trim();
                current["Name"] = currentName;
                continue;
            }

            var kv = Regex.Match(line, @"^(\w+)=(.+)$");
            if (kv.Success && current is not null)
                current[kv.Groups[1].Value] = kv.Groups[2].Value.Trim();
        }

        if (currentName is not null && current is not null) specs[currentName] = current;
        return specs;
    }

    private static bool IsBackupSpecActive(Dictionary<string, string> spec)
    {
        int.TryParse(spec.GetValueOrDefault("Width"), out var w);
        int.TryParse(spec.GetValueOrDefault("Height"), out var h);
        return w > 0 && h > 0;
    }

    private static void ParseRes(string? text, out int w, out int h)
    {
        w = 0; h = 0;
        if (string.IsNullOrWhiteSpace(text)) return;
        var m = Regex.Match(text, @"(\d+)\s*[Xx]\s*(\d+)");
        if (!m.Success) return;
        int.TryParse(m.Groups[1].Value, out w);
        int.TryParse(m.Groups[2].Value, out h);
    }

    private static ScreenRect? RectFromInfo(MonitorInfo? monitor)
    {
        if (monitor is null || string.IsNullOrWhiteSpace(monitor.LeftTop)) return null;
        var m = Regex.Match(monitor.LeftTop, @"(-?\d+)\s*,\s*(-?\d+)");
        if (!m.Success || monitor.Width <= 0 || monitor.Height <= 0) return null;
        return new ScreenRect
        {
            X = int.Parse(m.Groups[1].Value),
            Y = int.Parse(m.Groups[2].Value),
            Width = monitor.Width,
            Height = monitor.Height
        };
    }

    private static void ParseLeftTop(string? leftTop, out int? x, out int? y)
    {
        x = y = null;
        if (string.IsNullOrWhiteSpace(leftTop)) return;
        var m = Regex.Match(leftTop, @"(-?\d+)\s*,\s*(-?\d+)");
        if (!m.Success) return;
        x = int.Parse(m.Groups[1].Value);
        y = int.Parse(m.Groups[2].Value);
    }

    private static DisplayModeOption ToOption(int width, int height, int frequency, string suffix = "")
    {
        var freqPart = frequency > 0 ? $" @ {frequency} Hz" : "";
        return new DisplayModeOption
        {
            Width = width,
            Height = height,
            Frequency = frequency,
            Key = $"{width}x{height}@{frequency}",
            Text = $"{width} x {height}{freqPart}{suffix}"
        };
    }

    private static List<DisplayModeOption> MergeModes(List<DisplayModeOption> primary, List<DisplayModeOption> secondary, string secondarySuffix)
    {
        var merged = new List<DisplayModeOption>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in new[] { primary, secondary })
        {
            foreach (var mode in list)
            {
                if (!seen.Add(mode.Key)) continue;
                var suffix = list == secondary ? secondarySuffix : "";
                merged.Add(ToOption(mode.Width, mode.Height, mode.Frequency, suffix));
            }
        }
        return [.. merged.OrderByDescending(m => m.Width).ThenByDescending(m => m.Height).ThenByDescending(m => m.Frequency)];
    }

    private static List<DisplayModeOption> FallbackModes(MonitorInfo? monitor)
    {
        var resolutions = new List<(int W, int H)>();
        var maxW = monitor?.MaxWidth ?? 0;
        var maxH = monitor?.MaxHeight ?? 0;
        if (maxW <= 0) maxW = monitor?.Width ?? 0;
        if (maxH <= 0) maxH = monitor?.Height ?? 0;
        if (maxW > 0 && maxH > 0)
        {
            resolutions.Add((maxW, maxH));
            if (maxW >= 3840 && maxH >= 2160)
            {
                resolutions.Add((2560, 1440));
                resolutions.Add((1920, 1080));
            }
            else if (maxW >= 2560 && maxH >= 1440)
            {
                resolutions.Add((1920, 1080));
            }
        }
        else
        {
            resolutions.Add((3840, 2160));
            resolutions.Add((2560, 1440));
            resolutions.Add((1920, 1080));
        }

        int[] hz = [24, 30, 50, 59, 60, 75, 100, 120, 144, 165, 240, 300];
        var modes = new List<DisplayModeOption>();
        var seen = new HashSet<string>();
        foreach (var (w, h) in resolutions)
        {
            foreach (var rate in hz)
            {
                if (w >= 3840 && rate > 120) continue;
                if (w >= 2560 && rate > 165) continue;
                var key = $"{w}x{h}@{rate}";
                if (!seen.Add(key)) continue;
                modes.Add(ToOption(w, h, rate, " (estimado)"));
            }
        }
        return modes;
    }

    private Dictionary<string, List<DisplayModeOption>> LoadPersistedModes()
    {
        var result = new Dictionary<string, List<DisplayModeOption>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(AppPaths.MonitorModesCacheFile)) return result;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(AppPaths.MonitorModesCacheFile));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var list = new List<DisplayModeOption>();
                foreach (var entry in prop.Value.EnumerateArray())
                {
                    var w = entry.GetProperty("width").GetInt32();
                    var h = entry.GetProperty("height").GetInt32();
                    var f = entry.TryGetProperty("frequency", out var fp) ? fp.GetInt32() : 0;
                    if (w > 0 && h > 0) list.Add(ToOption(w, h, f));
                }
                if (list.Count > 0) result[prop.Name] = list;
            }
        }
        catch { /* cache is optional */ }
        return result;
    }

    private void PersistModes(string monitorName, List<DisplayModeOption> modes)
    {
        var cache = LoadPersistedModes();
        cache[monitorName] = modes;
        var output = cache.OrderBy(kv => kv.Key).ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(m => new { width = m.Width, height = m.Height, frequency = m.Frequency }));
        File.WriteAllText(AppPaths.MonitorModesCacheFile, JsonSerializer.Serialize(output, JsonUtil.Options));
    }
}
