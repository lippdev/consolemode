using System.Diagnostics;
using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// The Windows setting "Open Xbox Game Bar using this button on a controller". Turning it off
/// frees a short press of the Guide button for us; Game Bar itself stays enabled (Win+G).
/// Steam's own "Guide button focuses Steam" lives in Steam's config files, which Steam
/// rewrites while running, so we only detect Steam and tell the user.
/// </summary>
public static class GameBarService
{
    private const string GameBarKey = @"Software\Microsoft\GameBar";
    private const string GuideValue = "UseNexusForGameBarEnabled";

    public static bool GuideOpensGameBar
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(GameBarKey);
                // Windows treats a missing value as enabled.
                return key?.GetValue(GuideValue) is not int value || value != 0;
            }
            catch
            {
                return true;
            }
        }
    }

    public static void SetGuideOpensGameBar(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(GameBarKey, writable: true);
        key.SetValue(GuideValue, enabled ? 1 : 0, RegistryValueKind.DWord);
        AppLog.Write($"Game Bar: botão Xbox {(enabled ? "abre" : "não abre")} a Game Bar");
    }

    public static bool IsSteamRunning()
    {
        try { return Process.GetProcessesByName("steam").Length > 0; }
        catch { return false; }
    }
}
