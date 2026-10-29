using System.Diagnostics;
using System.Runtime.InteropServices;
using ConsoleMode.Models;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

/// <summary>
/// Opens JoyChromium from the session menu, and installs it from there when it's missing (like ControlFS). It is a
/// separate app, never closed from here: going back to the PC only takes it out of full screen.
/// </summary>
public static class JoyChromiumService
{
    private static readonly TimeSpan SetupTimeout = TimeSpan.FromMinutes(5);

    public static string? FindExe() => LaunchService.GetJoyChromiumPath();

    /// <summary>
    /// Starts it full screen (<c>--tv</c>), or hands <c>--tv</c> to the open one, which comes to the front and goes full
    /// screen: a second run passes its arguments on and exits. False when it isn't installed or didn't start.
    /// </summary>
    public static bool Open()
    {
        var exe = FindExe();
        if (exe is null) return false;
        try
        {
            var psi = new ProcessStartInfo { FileName = exe, UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! };
            psi.ArgumentList.Add(JoyChromium.TvArgument);
            Process.Start(psi)?.Dispose();
            AppLog.Write("JoyChromium: aberto pelo menu da sessão");
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"JoyChromium: abrir: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// <c>--tv</c> opens it full screen wherever Windows puts it, which may be a screen other than the session's: once its
    /// window is up, it is moved over the session screen. Runs off the UI thread; never throws.
    /// </summary>
    public static void EnsureOnScreen(ScreenRect? screen)
    {
        if (screen is null || screen.Width <= 0 || screen.Height <= 0) return;
        try
        {
            var deadline = Environment.TickCount64 + 15000;
            nint hwnd = 0;
            while (Environment.TickCount64 < deadline)
            {
                // The largest of its windows: Windows can report a tooltip as the "main" one.
                hwnd = LaunchService.GetProcessWindows(JoyChromium.ProcessName).FirstOrDefault();
                if (hwnd != 0 && NativeWindows.GetWindowArea(hwnd) > 200000) break;
                hwnd = 0;
                Thread.Sleep(300);
            }
            if (hwnd == 0) { AppLog.Write("JoyChromium: janela não apareceu para levar à tela da sessão"); return; }
            Thread.Sleep(500);
            if (NativeWindows.TryGetWindowRect(hwnd, out var l, out var t, out var w, out var h)
                && ControlFsLocator.IsFullScreen(l, t, w, h, screen.X, screen.Y, screen.Width, screen.Height)) return;

            NativeWindows.MoveWindowToRect(hwnd, screen.X, screen.Y, screen.Width, screen.Height);
            if (NativeWindows.GetForegroundWindow() != hwnd) NativeWindows.ForceForeground(hwnd);
            AppLog.Write("JoyChromium: levado para a tela da sessão");
        }
        catch (Exception ex) { AppLog.Write($"JoyChromium: tela da sessão: {ex.Message}"); }
    }

    /// <summary>Not installed and the install didn't work: the release page opens in the browser.</summary>
    public static void OpenDownloadPage()
    {
        try { Process.Start(new ProcessStartInfo(JoyChromium.ReleasesUrl) { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { AppLog.Write($"JoyChromium: página de download: {ex.Message}"); }
    }

    /// <summary>
    /// Downloads the installer of its newest release for this processor, checks it against GitHub's SHA-256 and runs it
    /// silently (per user, no admin prompt). Returns the installed exe; throws with the reason otherwise.
    /// <paramref name="progress"/> gets the download from 0 to 1, then <see cref="double.PositiveInfinity"/> once the setup runs.
    /// </summary>
    public static async Task<string> InstallAsync(IProgress<double>? progress, CancellationToken ct = default)
    {
        if (IsSetupRunning()) throw new InvalidOperationException("outro instalador do JoyChromium ainda está rodando");

        var arm64 = RuntimeInformation.OSArchitecture == Architecture.Arm64;
        var setup = await UpdateService.FindJoyChromiumSetupAsync(arm64, ct)
            ?? throw new InvalidOperationException("nenhuma release com instalador verificável");
        var dir = Path.Combine(Path.GetTempPath(), "ConsoleModeUpdate");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, setup.Name);
        try
        {
            await UpdateService.DownloadAsync(setup.Url, file, progress, ct);
            if (!await UpdateIntegrity.VerifyFileAsync(file, setup.Digest, ct))
                throw new InvalidDataException("o SHA-256 do instalador não confere");
            AppLog.Write($"JoyChromium: instalador {setup.Version} baixado e verificado");

            progress?.Report(double.PositiveInfinity);
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = JoyChromium.SilentArgument,
                UseShellExecute = false
            }) ?? throw new InvalidOperationException("o instalador não iniciou");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(SetupTimeout);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                await process.WaitForExitAsync(CancellationToken.None);
                throw new TimeoutException($"o instalador não terminou em {SetupTimeout.TotalMinutes:0} min e foi encerrado");
            }
            if (process.ExitCode != 0) throw new InvalidOperationException($"o instalador saiu com o código {process.ExitCode}");
        }
        finally
        {
            try { File.Delete(file); } catch { /* the temp folder is cleaned by Windows */ }
        }

        return FindExe() ?? throw new InvalidOperationException("instalado, mas o JoyChromium.exe não foi encontrado");
    }

    /// <summary>The setup is "JoyChromium-0.1.3-alpha-win-x64"; the browser itself is "JoyChromium" (no dash).</summary>
    private static bool IsSetupRunning()
    {
        var running = false;
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try { running |= process.ProcessName.StartsWith("JoyChromium-", StringComparison.OrdinalIgnoreCase); }
                catch { /* exited while listing */ }
            }
        }
        return running;
    }
}
