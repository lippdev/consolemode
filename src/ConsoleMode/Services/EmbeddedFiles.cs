using System.Reflection;

namespace ConsoleMode.Services;

public static class EmbeddedFiles
{
    public static void ExtractTools(string toolsDir)
    {
        Directory.CreateDirectory(toolsDir);
        foreach (var name in new[] { "MultiMonitorTool.exe", "SoundVolumeView.exe", "rtss-cli.exe" })
        {
            Extract($"ConsoleMode.Tools.{name}", Path.Combine(toolsDir, name));
        }
    }

    public static string? ExtractIcon(string destPath)
    {
        return Extract("ConsoleMode.Assets.icon.ico", destPath) ? destPath : null;
    }

    private static bool Extract(string resourceName, string destPath)
    {
        if (File.Exists(destPath)) return true;
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null) return false;

        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        using var file = File.Create(destPath);
        stream.CopyTo(file);
        return true;
    }
}
