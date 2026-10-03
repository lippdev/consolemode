using System.Diagnostics;
using System.IO.MemoryMappedFiles;
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

    /// <summary>
    /// Mid-session change from the session menu. Backs the user's settings up only the first
    /// time (Enable may already have), so Restore still puts back what they had. 0 = no limit.
    /// </summary>
    public OperationResult SetLimit(int fpsLimit, ConsoleRuntimeState state)
    {
        if (!IsReady || !EnsureRunning())
            return new OperationResult { Success = false, Message = "RTSS indisponível." };
        if (SessionMenuMath.NeedsRtssBackup(state.RtssLimitApplied) && !Backup(state))
            return new OperationResult { Success = false, Message = "Nao foi possivel ler as configuracoes atuais do RTSS." };
        try
        {
            if (fpsLimit > 0)
            {
                Cli("limit:set", fpsLimit.ToString());
                Cli("limiter:set", "1");
            }
            else
            {
                Cli("limiter:set", "0");
            }
            state.FpsLimit = fpsLimit;
            state.RtssLimitApplied = true;
            return new OperationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Message = ex.Message };
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

    /// <summary>
    /// Shows our FPS counter (RtssOverlay.Text) in our own OSD slot, or clears that slot when
    /// the text is empty. Other clients' slots (Afterburner) are left as they are.
    /// </summary>
    public bool SetOverlay(string text)
    {
        if (string.IsNullOrEmpty(text)) { WriteOverlaySlot(null); return true; }
        if (!EnsureRunning()) return false;
        // Right after RTSS starts its shared memory may not be there yet.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (WriteOverlaySlot(text)) return true;
            Thread.Sleep(300);
        }
        return false;
    }

    // RTSSSharedMemoryV2 (RTSS SDK, RTSSSharedMemory.h): header offsets and OSD entry layout.
    private const uint Signature = 0x52545353;   // 'RTSS'
    private const int OffVersion = 4, OffOsdEntrySize = 20, OffOsdArrOffset = 24, OffOsdArrSize = 28, OffOsdFrame = 32, OffBusy = 36;
    private const int EntryOsd = 0, EntryOwner = 256, EntryOsdEx = 512, OsdSize = 256, OwnerSize = 256, OsdExSize = 4096;

    /// <summary>null = release our slot. Mirrors UpdateOSD/ReleaseOSD from the SDK sample.</summary>
    private static unsafe bool WriteOverlaySlot(string? text)
    {
        try
        {
            using var map = MemoryMappedFile.OpenExisting("RTSSSharedMemoryV2", MemoryMappedFileRights.ReadWrite);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
            byte* mem = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref mem);
            try
            {
                mem += view.PointerOffset;
                var version = *(uint*)(mem + OffVersion);
                if (*(uint*)mem != Signature || version < 0x00020000) return false;

                var entrySize = *(uint*)(mem + OffOsdEntrySize);
                var arrOffset = *(uint*)(mem + OffOsdArrOffset);
                var arrSize = *(uint*)(mem + OffOsdArrSize);
                var owner = System.Text.Encoding.ASCII.GetBytes(RtssOverlay.Owner);
                var entries = mem + arrOffset;

                if (text is null)
                {
                    // Slot 0 belongs to the primary client (Afterburner); third parties start at 1.
                    for (uint i = 1; i < arrSize; i++)
                    {
                        var entry = entries + i * entrySize;
                        if (!IsOurs(entry, owner)) continue;
                        new Span<byte>(entry, (int)entrySize).Clear();
                        Interlocked.Increment(ref *(int*)(mem + OffOsdFrame));
                    }
                    return true;
                }

                // The slot we hold already, else the first free one.
                byte* slot = null;
                for (uint i = 1; i < arrSize && slot is null; i++)
                    if (IsOurs(entries + i * entrySize, owner)) slot = entries + i * entrySize;
                for (uint i = 1; i < arrSize && slot is null; i++)
                {
                    var entry = entries + i * entrySize;
                    if (entry[EntryOwner] != 0) continue;
                    owner.CopyTo(new Span<byte>(entry + EntryOwner, OwnerSize));
                    slot = entry;
                }
                if (slot is null) return false;

                var bytes = System.Text.Encoding.ASCII.GetBytes(text);
                var extended = version >= 0x00020007;
                var target = new Span<byte>(slot + (extended ? EntryOsdEx : EntryOsd), extended ? OsdExSize : OsdSize);
                // v2.14+: bit 0 of dwBusy locks the OSD while the renderer reads it.
                var lockable = version >= 0x0002000e;
                ref var busy = ref *(int*)(mem + OffBusy);
                for (var spin = 0; lockable && (Interlocked.Or(ref busy, 1) & 1) != 0; spin++)
                {
                    if (spin > 200) return false;
                    Thread.Sleep(1);
                }
                try
                {
                    target.Clear();
                    bytes.AsSpan(0, Math.Min(bytes.Length, target.Length - 1)).CopyTo(target);
                }
                finally
                {
                    if (lockable) Interlocked.And(ref busy, ~1);
                }
                Interlocked.Increment(ref *(int*)(mem + OffOsdFrame));
                return true;
            }
            finally
            {
                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
        catch (FileNotFoundException)
        {
            return false;   // RTSS isn't running: nothing on screen to show or clear
        }
        catch (Exception ex)
        {
            AppLog.Write($"RTSS: não foi possível atualizar o contador de FPS: {ex.Message}");
            return false;
        }
    }

    private static unsafe bool IsOurs(byte* entry, byte[] owner) =>
        new ReadOnlySpan<byte>(entry + EntryOwner, owner.Length + 1).SequenceEqual([.. owner, (byte)0]);

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
