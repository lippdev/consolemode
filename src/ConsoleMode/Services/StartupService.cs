using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// "Iniciar com o Windows": a per-user Run entry that opens the app straight to the tray.
/// The installer's "start with Windows" task writes the same value, so both stay in sync.
/// </summary>
public static class StartupService
{
    public const string TrayArgument = "--tray";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ConsoleMode";

    private const string AppKey = @"Software\ConsoleMode";

    /// <summary>
    /// The language picked in the installer's dialog (ConsoleMode.iss writes it), so the first
    /// run doesn't come up in the wrong language. Null for portable builds or older installs.
    /// </summary>
    public static string? ReadInstallerLanguage()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppKey);
            return key?.GetValue("Language") as string;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string value && value.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException(LocalizationService.Get("UnknownExecutablePath"));
            key.SetValue(ValueName, $"\"{exe}\" {TrayArgument}");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        AppLog.Write($"Iniciar com o Windows: {(enabled ? "ligado" : "desligado")}");
    }
}
