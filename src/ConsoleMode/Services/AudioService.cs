using ConsoleMode.Models;

namespace ConsoleMode.Services;

public sealed class AudioService
{
    private List<AudioDevice>? _cache;
    private string? _cachedDefaultId;

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
        if (!AppPaths.HasSvv) return [];
        if (!forceRefresh && _cache is not null) return _cache;

        var csvPath = Path.Combine(Path.GetTempPath(), $"consolemode_audio_{Guid.NewGuid():N}.csv");
        try
        {
            InvokeSvv("/ShowDisabledDevices", "1", "/ShowUnpluggedDevices", "1", "/scomma", csvPath);
            if (!File.Exists(csvPath))
            {
                _cache = [];
                return _cache;
            }

            var devices = new List<AudioDevice>();
            foreach (var row in CsvReader.Read(csvPath))
            {
                var friendlyId = row.Get("Command-Line Friendly ID");
                if (string.IsNullOrWhiteSpace(friendlyId)) continue;
                if (!string.Equals(row.Get("Type"), "Device", StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(row.Get("Direction"), "Render", StringComparison.OrdinalIgnoreCase)) continue;

                var deviceState = row.Get("Device State");
                var isActive = deviceState.Contains("Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(deviceState);
                var friendlyName = row.Get("Name");
                var driverName = row.Get("Device Name");
                string displayName;
                if (string.IsNullOrWhiteSpace(friendlyName)) displayName = driverName;
                else if (!string.IsNullOrWhiteSpace(driverName) && driverName != friendlyName) displayName = $"{friendlyName} ({driverName})";
                else displayName = friendlyName;


                devices.Add(new AudioDevice
                {
                    Name = displayName,
                    FriendlyId = friendlyId,
                    IsDefault = row.Get("Default").Contains("Render", StringComparison.OrdinalIgnoreCase),
                    IsActive = isActive
                });
            }

            _cache = [.. devices.OrderBy(d => d.IsActive ? 0 : 1).ThenBy(d => d.Name)];
            _cachedDefaultId = _cache.FirstOrDefault(d => d.IsDefault)?.FriendlyId;
            return _cache;
        }
        catch
        {
            _cache = [];
            return _cache;
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
        InvokeSvv("/Enable", friendlyId);
        InvokeSvv("/SetDefault", friendlyId, "all");
        _cachedDefaultId = friendlyId;
        _cache = null;
    }

    public void Restore(string? backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId) || !AppPaths.HasSvv) return;
        try { InvokeSvv("/SetDefault", backupId, "all"); }
        catch (Exception ex) { AppLog.Write($"Não foi possível restaurar o áudio: {ex.Message}"); }
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
