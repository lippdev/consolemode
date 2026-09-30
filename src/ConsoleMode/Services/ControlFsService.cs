using System.Diagnostics;
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
