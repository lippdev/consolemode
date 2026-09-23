using System.Text.Json;
using ConsoleMode.Models;

namespace ConsoleMode.Services;

public static class ConfigService
{
    public static AppConfig Load()
    {
        if (!File.Exists(AppPaths.ConfigPath)) return new AppConfig();
        try
        {
            var json = File.ReadAllText(AppPaths.ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonUtil.Options) ?? new AppConfig();
            config.HideMonitors ??= [];
            config.MonitorModes = new Dictionary<string, SavedDisplayMode>(
                config.MonitorModes ?? new Dictionary<string, SavedDisplayMode>(),
                StringComparer.OrdinalIgnoreCase);
            return config;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Config load failed: {ex.Message}");
            return new AppConfig();
        }
    }

    public const int CurrentVersion = 2;

    public static bool Exists => File.Exists(AppPaths.ConfigPath);

    /// <summary>
    /// Rewrites monitor references saved as GDI names (\\.\DISPLAYn, config v1) to stable ids.
    /// Names that no longer match any monitor are kept so nothing is silently lost.
    /// </summary>
    public static bool Migrate(AppConfig config, IReadOnlyList<MonitorInfo> monitors)
    {
        if (config.Version >= CurrentVersion) return false;
        if (monitors.Count == 0) return false;

        string ToId(string value) =>
            monitors.FirstOrDefault(m => string.Equals(m.Name, value, StringComparison.OrdinalIgnoreCase))?.StableId ?? value;

        config.FocusMonitor = ToId(config.FocusMonitor);
        config.HideMonitors = [.. config.HideMonitors.Select(ToId).Distinct(StringComparer.OrdinalIgnoreCase)];
        var modes = new Dictionary<string, SavedDisplayMode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, mode) in config.MonitorModes)
            modes[ToId(key)] = mode;
        config.MonitorModes = modes;
        config.Version = CurrentVersion;
        AppLog.Write("Config: migrada para ids estáveis de monitor");
        return true;
    }

    public static void Save(AppConfig config)
    {
        config.Version = CurrentVersion;
        var json = JsonSerializer.Serialize(config, JsonUtil.Options);
        File.WriteAllText(AppPaths.ConfigPath, json);
    }
}
