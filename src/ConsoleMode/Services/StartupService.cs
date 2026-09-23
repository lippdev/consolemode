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
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Caminho do executável desconhecido.");
            key.SetValue(ValueName, $"\"{exe}\" {TrayArgument}");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        AppLog.Write($"Iniciar com o Windows: {(enabled ? "ligado" : "desligado")}");
    }
}
