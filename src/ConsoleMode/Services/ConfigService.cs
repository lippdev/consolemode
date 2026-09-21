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

    public static void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonUtil.Options);
        File.WriteAllText(AppPaths.ConfigPath, json);
    }
}
