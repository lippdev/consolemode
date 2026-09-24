using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

public sealed record UpdateInfo(string Version, string Name, string Notes, string PageUrl, string? AssetUrl, string? AssetName, string? AssetDigest, bool IsPrerelease);

/// <summary>
/// Checks GitHub Releases for a newer version and applies it: the installed build runs the
/// new setup silently; the portable build swaps its exe and restarts.
/// </summary>
public static partial class UpdateService
{
    public const string Repository = "lippdev/consolemode";
    public const string SetupAssetPrefix = "ConsoleMode-Setup";
    public const string PortableAssetPrefix = "ConsoleMode-Portable";

    // Declared before Http: static initializers run in order and the User-Agent needs it.
    public static string CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";

    private static readonly HttpClient Http = CreateClient();

    // Deadlines are per request: a whole-client Timeout also cut off downloads that were
    // still progressing on slow connections.
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        // GitHub rejects API calls without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"ConsoleMode/{CurrentVersion}");
        return client;
    }

    /// <summary>Newest published release above the running version, or null when up to date.</summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(CheckTimeout);
        ct = timeout.Token;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=15");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var current = SemVer.Parse(CurrentVersion);
        // Betas see betas; stable builds only get stable releases.
        var allowPrerelease = current.IsPrerelease;

        UpdateInfo? best = null;
        SemVer? bestVersion = null;
        foreach (var release in doc.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()) continue;
            var prerelease = release.GetProperty("prerelease").GetBoolean();
            if (prerelease && !allowPrerelease) continue;

            var tag = release.GetProperty("tag_name").GetString() ?? "";
            var name = release.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            // Old releases were tagged "release"/"app"; fall back to the version in the title.
            var version = SemVer.TryFind(tag) ?? SemVer.TryFind(name);
            if (version is null || version.CompareTo(current) <= 0) continue;
            if (bestVersion is not null && version.CompareTo(bestVersion) <= 0) continue;

            var (assetUrl, assetName, assetDigest) = PickAsset(release);
            best = new UpdateInfo(
                version.ToString(),
                string.IsNullOrWhiteSpace(name) ? tag : name,
                release.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                release.GetProperty("html_url").GetString() ?? $"https://github.com/{Repository}/releases",
                assetUrl,
                assetName,
                assetDigest,
                prerelease);
            bestVersion = version;
        }

        return best;
    }

    private static (string? Url, string? Name, string? Digest) PickAsset(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets)) return (null, null, null);
        var prefix = AppPaths.IsInstalled ? SetupAssetPrefix : PortableAssetPrefix;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var digest = asset.TryGetProperty("digest", out var d) ? d.GetString() : null;
                // Without GitHub's SHA-256 digest, show the release page but never auto-run the asset.
                if (!UpdateIntegrity.IsValidSha256Digest(digest)) return (null, null, null);
                return (asset.GetProperty("browser_download_url").GetString(), name, digest);
            }
        }
        return (null, null, null);
    }

    /// <summary>
    /// Downloads the update and hands over to it. The caller must exit the app afterwards
    /// (the setup / swap script waits for this process to end).
    /// </summary>
    public static async Task ApplyAsync(UpdateInfo update, IProgress<double>? progress, CancellationToken ct = default)
    {
        if (update.AssetUrl is null || update.AssetName is null || update.AssetDigest is null)
            throw new InvalidOperationException(LocalizationService.Get("UpdateAssetMissing"));

        var dir = Path.Combine(Path.GetTempPath(), "ConsoleModeUpdate");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, update.AssetName);
        await DownloadAsync(update.AssetUrl, file, progress, ct);
        if (!await UpdateIntegrity.VerifyFileAsync(file, update.AssetDigest, ct))
        {
            try { File.Delete(file); } catch { /* do not keep an untrusted update */ }
            throw new InvalidDataException(LocalizationService.Get("UpdateHashMismatch"));
        }
        AppLog.Write($"Atualização {update.Version}: baixada e verificada em {file}");

        if (AppPaths.IsInstalled)
        {
            // Inno Setup: closes this app through Restart Manager and reopens it when done.
            Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS",
                UseShellExecute = true
            });
            return;
        }

        // Portable: a running exe can't be overwritten, so a tiny script waits for us to exit,
        // swaps the file and starts the new version.
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException(LocalizationService.Get("UnknownExecutablePath"));
        var script = Path.Combine(dir, "swap.cmd");
        File.WriteAllText(script,
            "@echo off\r\n" +
            // ping as a sleep: timeout.exe fails without an interactive console.
            $":wait\r\ntasklist /fi \"PID eq {Environment.ProcessId}\" | find \"{Environment.ProcessId}\" >nul && (ping -n 2 127.0.0.1 >nul & goto wait)\r\n" +
            $"move /y \"{file}\" \"{exe}\" >nul\r\n" +
            $"start \"\" \"{exe}\"\r\n");
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{script}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = dir
        });
    }

    private static async Task DownloadAsync(string url, string file, IProgress<double>? progress, CancellationToken ct)
    {
        // Cancelled only when no bytes arrive for StallTimeout, not after a fixed total time.
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stall.CancelAfter(StallTimeout);
        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, stall.Token);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1;
            await using var source = await response.Content.ReadAsStreamAsync(stall.Token);
            await using var target = File.Create(file);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, stall.Token)) > 0)
            {
                stall.CancelAfter(StallTimeout);
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
                done += read;
                if (total > 0) progress?.Report((double)done / total);
            }
        }
        catch (OperationCanceledException) when (stall.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Download stalled: no data for {StallTimeout.TotalSeconds:0} s.");
        }
    }

    /// <summary>Minimal semantic version: 1.3.0, 1.3.0-beta.2 (prerelease sorts below release).</summary>
    public sealed partial class SemVer : IComparable<SemVer>
    {
        private readonly int[] _core;
        private readonly string[] _pre;

        private SemVer(int[] core, string[] pre)
        {
            _core = core;
            _pre = pre;
        }

        public bool IsPrerelease => _pre.Length > 0;

        [GeneratedRegex(@"(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z.\-]+))?")]
        private static partial Regex Pattern();

        public static SemVer Parse(string text) => TryFind(text) ?? new SemVer([0, 0, 0], []);

        public static SemVer? TryFind(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Pattern().Match(text);
            if (!m.Success) return null;
            var core = new[]
            {
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value),
                m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0
            };
            var pre = m.Groups[4].Success ? m.Groups[4].Value.Split('.') : [];
            return new SemVer(core, pre);
        }

        public int CompareTo(SemVer? other)
        {
            if (other is null) return 1;
            for (var i = 0; i < 3; i++)
            {
                var c = _core[i].CompareTo(other._core[i]);
                if (c != 0) return c;
            }
            if (_pre.Length == 0 || other._pre.Length == 0) return other._pre.Length.CompareTo(_pre.Length);
            for (var i = 0; i < Math.Min(_pre.Length, other._pre.Length); i++)
            {
                var a = _pre[i];
                var b = other._pre[i];
                var c = int.TryParse(a, out var ai) && int.TryParse(b, out var bi)
                    ? ai.CompareTo(bi)
                    : string.CompareOrdinal(a, b);
                if (c != 0) return c;
            }
            return _pre.Length.CompareTo(other._pre.Length);
        }

        public override string ToString() =>
            $"{_core[0]}.{_core[1]}.{_core[2]}{(_pre.Length > 0 ? "-" + string.Join('.', _pre) : "")}";
    }
}
