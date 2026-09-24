using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

public static class ShortcutService
{
    public const string StartArgument = "--start";

    /// <summary>Creates "Console Mode 1 Click.lnk" on the desktop (a name of its own, so it never
    /// overwrites a regular "Console Mode" shortcut to the app) that runs this exe with --start.</summary>
    public static string CreateDesktopShortcut()
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException(LocalizationService.Get("UnknownExecutablePath"));
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var name = LocalizationService.Get("ShortcutName");
        var path = Path.Combine(desktop, $"{name}.lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new InvalidOperationException(LocalizationService.Get("ShortcutUnavailable"));
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic link = shell.CreateShortcut(path);
            try
            {
                link.TargetPath = exe;
                link.Arguments = StartArgument;
                link.WorkingDirectory = AppPaths.ExeDir;
                link.IconLocation = File.Exists(AppPaths.IconPath) ? AppPaths.IconPath : $"{exe},0";
                link.Description = LocalizationService.Get("ShortcutDescription");
                link.Save();
            }
            finally
            {
                Marshal.FinalReleaseComObject(link);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }

        AppLog.Write($"Atalho criado: {path}");
        return path;
    }
}
