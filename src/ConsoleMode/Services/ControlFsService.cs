using System.Diagnostics;
using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.Win32;

namespace ConsoleMode.Services;

/// <summary>
/// Opens ControlFS from the session menu. ControlFS is a separate app (installed on its own): it is found the way
/// Windows lists it, started with its own <c>--start</c> flag (opens it, or brings the open one to the front; it
/// runs one instance per user) and never killed from here.
/// </summary>
public static class ControlFsService
{
    public static string? FindExe()
    {
        try
        {
            var folders = ControlFsLocator.Folders(
                ReadInstallLocation(),
                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                Environment.GetEnvironmentVariable("ProgramFiles"));
            return ControlFsLocator.Resolve(folders, File.Exists);
        }
        catch (Exception ex)
        {
            AppLog.Write($"ControlFS: procurar: {ex.Message}");
            return null;
        }
    }

    /// <summary>Starts (or brings to the front) ControlFS. False when it isn't installed or didn't start.</summary>
    public static bool Open()
    {
        var exe = FindExe();
        if (exe is null) return false;
        try
        {
            // Already open: bring that window forward. Launching it again can leave a second copy running
            // (seen with the installed build), so the flag is only for when there is no window yet.
            var open = FindOpenWindow();
            if (open != 0)
            {
                NativeWindows.ShowWindow(open, 9 /* SW_RESTORE */);
                NativeWindows.ForceForeground(open);
                AppLog.Write("ControlFS: já estava aberto, trazido para a frente");
                return true;
            }

            var psi = new ProcessStartInfo { FileName = exe, UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! };
            psi.ArgumentList.Add("--start");
            Process.Start(psi)?.Dispose();
            AppLog.Write("ControlFS: aberto");
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"ControlFS: abrir: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ControlFS has no start-up flag for full screen (it keeps the choice in its own settings), so once its window is
    /// up it is moved to the session screen and, if it isn't exactly covering it, F11 (its full-screen key) is sent.
    /// Maximized is the fallback if F11 didn't take. Runs off the UI thread; never throws.
    /// </summary>
    public static void EnsureFullScreen(ScreenRect? screen)
    {
        if (screen is null || screen.Width <= 0 || screen.Height <= 0) return;
        try
        {
            var hwnd = WaitForWindow(TimeSpan.FromSeconds(12));
            if (hwnd == 0) { AppLog.Write("ControlFS: janela não apareceu para colocar em tela cheia"); return; }
            if (IsCovering(hwnd, screen)) return;

            // A restored or maximized window first goes to the session screen, so F11 makes it cover that one.
            NativeWindows.ShowWindow(hwnd, 9 /* SW_RESTORE */);
            NativeWindows.MoveWindowToRect(hwnd, screen.X + 80, screen.Y + 80, Math.Max(400, screen.Width - 160), Math.Max(300, screen.Height - 160));
            Thread.Sleep(300);
            if (NativeWindows.GetForegroundWindow() != hwnd) NativeWindows.ForceForeground(hwnd);
            Thread.Sleep(200);
            if (NativeWindows.GetForegroundWindow() == hwnd)
            {
                NativeWindows.SendF11();
                Thread.Sleep(700);
            }
            else
            {
                AppLog.Write("ControlFS: F11 n\u00e3o enviado porque outra janela est\u00e1 em primeiro plano");
            }
            if (IsCovering(hwnd, screen)) { AppLog.Write("ControlFS: em tela cheia"); return; }

            NativeWindows.ShowWindow(hwnd, 3 /* SW_MAXIMIZE */);
            AppLog.Write("ControlFS: F11 não surtiu efeito, janela maximizada");
        }
        catch (Exception ex) { AppLog.Write($"ControlFS: tela cheia: {ex.Message}"); }
    }

    private static bool IsCovering(nint hwnd, ScreenRect screen) =>
        NativeWindows.TryGetWindowRect(hwnd, out var l, out var t, out var w, out var h)
        && ControlFsLocator.IsFullScreen(l, t, w, h, screen.X, screen.Y, screen.Width, screen.Height);

    private static nint FindOpenWindow()
    {
        foreach (var process in Process.GetProcessesByName("ControlFS"))
        {
            using (process)
            {
                process.Refresh();
                var hwnd = process.MainWindowHandle;
                if (hwnd != 0 && NativeWindows.IsWindowStillVisible(hwnd)) return hwnd;
            }
        }
        return 0;
    }

    private static nint WaitForWindow(TimeSpan timeout)
    {
        var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            var hwnd = FindOpenWindow();
            if (hwnd != 0) { Thread.Sleep(500); return hwnd; }
            Thread.Sleep(250);
        }
        return 0;
    }

    /// <summary>Not installed: the release page opens in the browser.</summary>
    public static void OpenDownloadPage()
    {
        try { Process.Start(new ProcessStartInfo(ControlFsLocator.ReleasesUrl) { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { AppLog.Write($"ControlFS: página de download: {ex.Message}"); }
    }

    private static string? ReadInstallLocation()
    {
        const string uninstall = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var (hive, view) in new[]
                 { (RegistryHive.CurrentUser, RegistryView.Default), (RegistryHive.LocalMachine, RegistryView.Registry64), (RegistryHive.LocalMachine, RegistryView.Registry32) })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view).OpenSubKey(uninstall);
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var key = root.OpenSubKey(name);
                    if (key?.GetValue("DisplayName") is string display
                        && display.Equals("ControlFS", StringComparison.OrdinalIgnoreCase)
                        && key.GetValue("InstallLocation") is string location
                        && !string.IsNullOrWhiteSpace(location))
                        return location;
                }
            }
            catch { /* this hive/view isn't readable: try the next */ }
        }
        return null;
    }
}
