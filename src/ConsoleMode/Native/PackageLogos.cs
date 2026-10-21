using System.Xml.Linq;
using ConsoleMode.Services;

namespace ConsoleMode.Native;

/// <summary>The app-list logo file of an installed Store app, at the biggest size it ships.</summary>
internal static class PackageLogos
{
    /// <summary>Never throws: null when the app isn't installed or its logo can't be found.</summary>
    public static string? Find(string appUserModelId)
    {
        try
        {
            var parts = appUserModelId.Split('!', 2);
            if (parts.Length != 2) return null;
            var package = new Windows.Management.Deployment.PackageManager()
                .FindPackagesForUser(string.Empty, parts[0]).FirstOrDefault();
            var folder = package?.InstalledLocation?.Path;
            if (string.IsNullOrWhiteSpace(folder)) return null;

            var manifest = XDocument.Load(Path.Combine(folder, "AppxManifest.xml"));
            var app = manifest.Descendants().FirstOrDefault(e => e.Name.LocalName == "Application" && (string?)e.Attribute("Id") == parts[1]);
            var visuals = app?.Descendants().FirstOrDefault(e => e.Name.LocalName == "VisualElements");
            var logo = (string?)visuals?.Attribute("Square44x44Logo") ?? (string?)visuals?.Attribute("Square150x150Logo");
            if (string.IsNullOrWhiteSpace(logo)) return null;

            var logoFolder = Path.Combine(folder, Path.GetDirectoryName(logo) ?? "");
            return Directory.Exists(logoFolder)
                ? PackageLogo.Pick(logo, Directory.EnumerateFiles(logoFolder, Path.GetFileNameWithoutExtension(logo) + "*"))
                : null;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Início: logo de {appUserModelId}: {ex.Message}");
            return null;
        }
    }
}
