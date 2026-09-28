using ConsoleMode.Models;
using ConsoleMode.Native;

namespace ConsoleMode.Services;

public sealed class AudioService
{
    private static bool? _useNative;
    private List<AudioDevice>? _cache;
    private string? _cachedDefaultId;
    // Native backend: friendly ID (what config and backups store) -> Core Audio endpoint ID.
    private Dictionary<string, string> _endpointIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Core Audio instead of SoundVolumeView (issue #91). Off by default while it's verified on real
    /// hardware: set <c>"NativeAudio": true</c> in config.json and restart the app.
    /// </summary>
    public static bool UseNative => _useNative ??= LoadNativeFlag();

    /// <summary>Audio switching works: the native backend is on, or SoundVolumeView is present.</summary>
    public static bool IsAvailable => UseNative || AppPaths.HasSvv;

    private static bool LoadNativeFlag()
    {
        try { return ConfigService.Load().NativeAudio; }
        catch { return false; }
    }

    public void ClearCache()
    {
        _cache = null;
        _cachedDefaultId = null;
    }

    public int InvokeSvv(params string[] args)
    {
        if (!AppPaths.HasSvv) throw new InvalidOperationException(LocalizationService.Get("SvvMissing"));
        return ProcessRunner.Run(AppPaths.SvvPath, args);
    }

    public IReadOnlyList<AudioDevice> GetDevices(bool forceRefresh = false)
    {
        if (!IsAvailable) return [];
        if (!forceRefresh && _cache is not null) return _cache;

        try
        {
            var devices = UseNative ? ReadNativeDevices() : ReadSvvDevices();
            _cache = [.. devices.OrderBy(d => d.IsActive ? 0 : 1).ThenBy(d => d.Name)];
        }
        catch (Exception ex)
        {
            AppLog.Write($"Áudio: não foi possível listar as saídas: {ex.Message}");
            _cache = [];
        }
        _cachedDefaultId = _cache.FirstOrDefault(d => d.IsDefault)?.FriendlyId;
        return _cache;
    }

    private List<AudioDevice> ReadNativeDevices()
    {
        var devices = new List<AudioDevice>();
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in CoreAudio.ListRenderEndpoints())
        {
            var friendlyId = AudioNaming.FriendlyId(endpoint.AdapterName, endpoint.Name);
            // Two outputs with the same names: keep the active / default one, like the ID can only name one.
            if (ids.ContainsKey(friendlyId) && !endpoint.IsActive && !endpoint.IsDefault) continue;
            ids[friendlyId] = endpoint.Id;
            devices.RemoveAll(d => string.Equals(d.FriendlyId, friendlyId, StringComparison.OrdinalIgnoreCase));
            devices.Add(new AudioDevice
            {
                Name = AudioNaming.DisplayName(endpoint.Name, endpoint.AdapterName),
                FriendlyId = friendlyId,
                IsDefault = endpoint.IsDefault,
                IsActive = endpoint.IsActive
            });
        }
        _endpointIds = ids;
        return devices;
    }

    private List<AudioDevice> ReadSvvDevices()
    {
        var csvPath = Path.Combine(Path.GetTempPath(), $"consolemode_audio_{Guid.NewGuid():N}.csv");
        try
        {
            InvokeSvv("/ShowDisabledDevices", "1", "/ShowUnpluggedDevices", "1", "/scomma", csvPath);
            if (!File.Exists(csvPath)) return [];

            var devices = new List<AudioDevice>();
            foreach (var row in CsvReader.Read(csvPath))
            {
                var friendlyId = row.Get("Command-Line Friendly ID");
                if (string.IsNullOrWhiteSpace(friendlyId)) continue;
                if (!string.Equals(row.Get("Type"), "Device", StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(row.Get("Direction"), "Render", StringComparison.OrdinalIgnoreCase)) continue;

                var deviceState = row.Get("Device State");
                devices.Add(new AudioDevice
                {
                    Name = AudioNaming.DisplayName(row.Get("Name"), row.Get("Device Name")),
                    FriendlyId = friendlyId,
                    IsDefault = row.Get("Default").Contains("Render", StringComparison.OrdinalIgnoreCase),
                    IsActive = deviceState.Contains("Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(deviceState)
                });
            }
            return devices;
        }
        finally
        {
            try { File.Delete(csvPath); } catch { /* ignore */ }
        }
    }

    public string? GetDefaultId()
    {
        if (_cachedDefaultId is not null) return _cachedDefaultId;
        return GetDevices().FirstOrDefault(d => d.IsDefault)?.FriendlyId;
    }

    public void SetOutput(string friendlyId)
    {
        if (UseNative)
        {
            var endpointId = ResolveEndpoint(friendlyId);
            if (GetDevices().FirstOrDefault(d => d.FriendlyId == friendlyId) is { IsActive: false })
                CoreAudio.Enable(endpointId);
            CoreAudio.SetDefault(endpointId);
        }
        else
        {
            InvokeSvv("/Enable", friendlyId);
            InvokeSvv("/SetDefault", friendlyId, "all");
        }
        AppLog.Write($"Áudio: saída padrão = {friendlyId}");
        _cachedDefaultId = friendlyId;
        _cache = null;
    }

    public void Restore(string? backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId) || !IsAvailable) return;
        try
        {
            if (UseNative) CoreAudio.SetDefault(ResolveEndpoint(backupId));
            else InvokeSvv("/SetDefault", backupId, "all");
        }
        catch (Exception ex) { AppLog.Write($"Não foi possível restaurar o áudio: {ex.Message}"); }
    }

    /// <summary>Output volume from 0 to 100, or null when it can't be read.</summary>
    public int? GetVolumePercent(string friendlyId) => UseNative
        ? CoreAudio.GetVolumePercent(ResolveEndpoint(friendlyId))
        : SessionMenuMath.ParseVolumeExitCode(InvokeSvv("/GetPercent", friendlyId));

    /// <summary>Sets the volume and unmutes, like turning the knob.</summary>
    public void SetVolumePercent(string friendlyId, int percent)
    {
        if (UseNative)
        {
            var endpointId = ResolveEndpoint(friendlyId);
            CoreAudio.SetVolumePercent(endpointId, percent);
            CoreAudio.SetMute(endpointId, false);
        }
        else
        {
            InvokeSvv("/SetVolume", friendlyId, percent.ToString());
            InvokeSvv("/Unmute", friendlyId);
        }
    }

    public void SetMute(string friendlyId, bool mute)
    {
        if (UseNative) CoreAudio.SetMute(ResolveEndpoint(friendlyId), mute);
        else InvokeSvv(mute ? "/Mute" : "/Unmute", friendlyId);
    }

    private string ResolveEndpoint(string friendlyId)
    {
        if (_endpointIds.TryGetValue(friendlyId, out var id)) return id;
        GetDevices(forceRefresh: true);
        return _endpointIds.TryGetValue(friendlyId, out id)
            ? id
            : throw new InvalidOperationException($"Saída de áudio não encontrada: {friendlyId}");
    }

    public AudioDevice? PickNewDevice(IReadOnlyList<AudioDevice> candidates, string? hint, MonitorInfo? focus)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        AudioDevice? best = null;
        var bestScore = -1;
        foreach (var device in candidates)
        {
            var score = Score(device, hint, focus);
            if (score <= bestScore) continue;
            bestScore = score;
            best = device;
        }
        return bestScore <= 0 ? candidates[0] : best;
    }

    private static int Score(AudioDevice device, string? hint, MonitorInfo? focus)
    {
        var text = device.Name
            .Replace(LocalizationService.Get("AudioDisabledSuffix"), "", StringComparison.OrdinalIgnoreCase)
            .Replace(" [Desabilitado]", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        var score = 0;
        if (!string.IsNullOrWhiteSpace(hint))
        {
            foreach (var part in hint.Split([' ', '/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length >= 3 && text.Contains(part, StringComparison.OrdinalIgnoreCase)) score += 8;
            }
        }
        if (!string.IsNullOrWhiteSpace(focus?.MonitorName))
        {
            foreach (var part in focus.MonitorName.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length >= 3 && text.Contains(part, StringComparison.OrdinalIgnoreCase)) score += 5;
            }
        }
        if (text.Contains("HDMI", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Display", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("High Definition Audio", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("SSCR", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("TV", StringComparison.OrdinalIgnoreCase))
        {
            score += 1;
        }
        return score;
    }
}
