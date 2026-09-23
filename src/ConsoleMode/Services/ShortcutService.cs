using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

public static class ShortcutService
{
    public const string StartArgument = "--start";

    /// <summary>Creates "Modo Console.lnk" on the desktop that runs this exe with --start.</summary>
    public static string CreateDesktopShortcut()
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Caminho do executável desconhecido.");
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var path = Path.Combine(desktop, "Modo Console.lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new InvalidOperationException("WScript.Shell indisponível neste Windows.");
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
                link.Description = "Entra no modo console com 1 clique";
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
