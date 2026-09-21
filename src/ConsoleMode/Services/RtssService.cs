using System.Diagnostics;
using System.Text.Json;
using ConsoleMode.Models;

namespace ConsoleMode.Services;

public sealed class RtssService
{
    private static readonly string[] InstallCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "RivaTuner Statistics Server", "RTSS.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RivaTuner Statistics Server", "RTSS.exe")
    ];

    public bool IsInstalled => !string.IsNullOrWhiteSpace(GetInstallPath());
    public bool IsReady => IsInstalled && AppPaths.HasRtssCli;

    public string? GetInstallPath() => InstallCandidates.FirstOrDefault(File.Exists);

    public bool IsRunning()
    {
        foreach (var name in new[] { "RTSS", "RTSSHooksLoader64", "RTSSHooksLoader" })
        {
            if (Process.GetProcessesByName(name).Length > 0) return true;
        }
        return false;
    }

    public bool EnsureRunning()
    {
        if (IsRunning()) return true;
        var exe = GetInstallPath();
        if (exe is null) return false;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = exe, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true });
            Thread.Sleep(1500);
            return IsRunning();
        }
        catch
        {
            return false;
        }
    }

    public OperationResult Enable(int fpsLimit, ConsoleRuntimeState state)
    {
        if (fpsLimit <= 0) return new OperationResult { Success = false };
        if (!IsInstalled)
            return new OperationResult { Success = false, Message = "RivaTuner Statistics Server nao encontrado. Instale via MSI Afterburner para usar limite de FPS." };
        if (!AppPaths.HasRtssCli)
            return new OperationResult { Success = false, Message = "rtss-cli.exe nao encontrado. Execute build\\Get-RtssCli.ps1." };
        if (!EnsureRunning())
            return new OperationResult { Success = false, Message = "Nao foi possivel iniciar o RivaTuner Statistics Server." };
        if (!Backup(state))
            return new OperationResult { Success = false, Message = "Nao foi possivel ler as configuracoes atuais do RTSS." };

        try
        {
            Cli("limit:set", fpsLimit.ToString());
            Cli("limiter:set", "1");
            state.FpsLimit = fpsLimit;
            state.RtssLimitApplied = true;
            return new OperationResult { Success = true, Message = $"Limite de FPS global definido para {fpsLimit} via RTSS." };
        }
        catch (Exception ex)
        {
            state.RtssBackup = null;
            return new OperationResult { Success = false, Message = $"Falha ao aplicar limite no RTSS: {ex.Message}" };
        }
    }

    public void Restore(ConsoleRuntimeState state)
    {
        var hasFile = File.Exists(AppPaths.BackupRtssFpsFile);
        if (!state.RtssLimitApplied && !hasFile) return;

        var backup = state.RtssBackup;
        if (backup is null && hasFile)
        {
            try { backup = JsonSerializer.Deserialize<RtssBackup>(File.ReadAllText(AppPaths.BackupRtssFpsFile), JsonUtil.Options); }
            catch { /* ignore */ }
        }

        try
        {
            if (AppPaths.HasRtssCli && EnsureRunning())
            {
                if (backup is not null)
                {
                    Cli("limit:set", backup.FramerateLimit.ToString());
                    Cli("limiter:set", backup.LimiterEnabled ? "1" : "0");
                }
                else
                {
                    Cli("limiter:set", "0");
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Nao foi possivel restaurar RTSS: {ex.Message}");
        }
        finally
        {
            state.RtssLimitApplied = false;
            state.RtssBackup = null;
            if (hasFile)
            {
                try { File.Delete(AppPaths.BackupRtssFpsFile); } catch { /* ignore */ }
            }
        }
    }

    private bool Backup(ConsoleRuntimeState state)
    {
        if (!AppPaths.HasRtssCli || !EnsureRunning()) return false;
        try
        {
            var limitRaw = Cli("limit:get");
            var limiterRaw = Cli("limiter:get");
            if (!int.TryParse(limitRaw, out var limit)) return false;
            var limiterOn = limiterRaw == "1" || (int.TryParse(limiterRaw, out var n) && n != 0);
            var backup = new RtssBackup
            {
                FramerateLimit = limit,
                LimiterEnabled = limiterOn,
                SavedAt = DateTime.Now.ToString("o")
            };
            state.RtssBackup = backup;
            File.WriteAllText(AppPaths.BackupRtssFpsFile, JsonSerializer.Serialize(backup, JsonUtil.Options));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Cli(params string[] args) => ProcessRunner.RunCapture(AppPaths.RtssCliPath, args);
}
