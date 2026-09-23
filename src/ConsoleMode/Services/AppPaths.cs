using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleMode.Services;

public static class AppLog
{
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            // DataDir is empty until AppPaths.Initialize; early crashes still need a log.
            var dir = !string.IsNullOrEmpty(AppPaths.DataDir)
                ? AppPaths.DataDir
                : Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "ConsoleMode_Data");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "consolemode.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}";
            lock (Gate)
            {
                File.AppendAllText(path, line + Environment.NewLine);
                var info = new FileInfo(path);
                if (info.Length > 500 * 1024)
                {
                    var tail = File.ReadLines(path).TakeLast(1000);
                    File.WriteAllLines(path, tail);
                }
            }
        }
        catch
        {
            // logging must never throw
        }
    }
}

public static class JsonUtil
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

public static class AppPaths
{
    public static string ExeDir { get; private set; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static string DataDir { get; private set; } = "";
    public static string ToolsDir { get; private set; } = "";
    public static string MmtPath { get; private set; } = "";
    public static string SvvPath { get; private set; } = "";
    public static string RtssCliPath { get; private set; } = "";
    public static string ConfigPath { get; private set; } = "";
    public static string BackupMonitorConfig { get; private set; } = "";
    public static string BackupMonitorMeta { get; private set; } = "";
    public static string BackupAudioFile { get; private set; } = "";
    public static string BackupRtssFpsFile { get; private set; } = "";
    public static string MonitorModesCacheFile { get; private set; } = "";
    public static string IconPath { get; private set; } = "";

    public static void Initialize()
    {
        // Single-file extract goes to %TEMP%\.net — keep data next to the real exe.
        var processPath = Environment.ProcessPath;
        ExeDir = !string.IsNullOrWhiteSpace(processPath)
            ? Path.GetDirectoryName(processPath) ?? ""
            : AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        DataDir = Path.Combine(ExeDir, "ConsoleMode_Data");
        ToolsDir = Path.Combine(DataDir, "tools");
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ToolsDir);

        EmbeddedFiles.ExtractTools(ToolsDir);
        EmbeddedFiles.ExtractIcon(Path.Combine(DataDir, "icon.ico"));

        foreach (var name in new[] { "MultiMonitorTool.exe", "SoundVolumeView.exe", "rtss-cli.exe" })
        {
            var dest = Path.Combine(ToolsDir, name);
            if (File.Exists(dest)) continue;
            var beside = Path.Combine(ExeDir, name);
            if (File.Exists(beside))
            {
                try { File.Copy(beside, dest, true); } catch { /* ignore */ }
            }
        }

        MmtPath = FirstExisting(Path.Combine(ToolsDir, "MultiMonitorTool.exe"), Path.Combine(ExeDir, "MultiMonitorTool.exe"));
        SvvPath = FirstExisting(Path.Combine(ToolsDir, "SoundVolumeView.exe"), Path.Combine(ExeDir, "SoundVolumeView.exe"));
        RtssCliPath = FirstExisting(Path.Combine(ToolsDir, "rtss-cli.exe"), Path.Combine(ExeDir, "rtss-cli.exe"));

        ConfigPath = Path.Combine(DataDir, "config.json");
        BackupMonitorConfig = Path.Combine(DataDir, "backup_monitores.cfg");
        BackupMonitorMeta = Path.Combine(DataDir, "backup_monitores_meta.json");
        BackupAudioFile = Path.Combine(DataDir, "backup_audio.txt");
        BackupRtssFpsFile = Path.Combine(DataDir, "backup_rtss_fps.json");
        MonitorModesCacheFile = Path.Combine(DataDir, "monitor_modes_cache.json");

        IconPath = FirstExisting(
            Path.Combine(DataDir, "icon.ico"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"),
            Path.Combine(ExeDir, "assets", "icon.ico"),
            Path.Combine(ExeDir, "icon.ico"));

        MigrateLegacy(ExeDir, "config.json", ConfigPath);
        foreach (var file in new[] { "backup_monitores.cfg", "backup_monitores_meta.json", "backup_audio.txt", "backup_rtss_fps.json" })
        {
            MigrateLegacy(ExeDir, file, Path.Combine(DataDir, file));
        }
    }

    public static bool HasMmt => File.Exists(MmtPath);
    public static bool HasSvv => File.Exists(SvvPath);
    public static bool HasRtssCli => File.Exists(RtssCliPath);

    private static string FirstExisting(params string[] paths)
    {
        foreach (var p in paths)
        {
            if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) return p;
        }
        return paths[0];
    }

    private static void MigrateLegacy(string exeDir, string name, string dest)
    {
        var src = Path.Combine(exeDir, name);
        if (File.Exists(src) && !File.Exists(dest))
        {
            try { File.Move(src, dest); } catch { /* ignore */ }
        }
    }
}
