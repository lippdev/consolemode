using System.Text.Json;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControlFsReleaseTests
{
    private const string Digest = "sha256:b97a98bb42b56c572f92e08003c251a541e8d740a5769b4ba85cf686a1095347";
    private const string Url = "https://github.com/nextestudios/ControlFS/releases/download/v0.17.0-alpha.1/ControlFS-Setup-x64.exe";

    private static string Release(string tag, string assets, bool draft = false) =>
        $$"""{"tag_name":"{{tag}}","draft":{{(draft ? "true" : "false")}},"assets":[{{assets}}]}""";

    private static string Asset(string name, string url, string? digest, long size = 49030428) =>
        $$"""{"name":"{{name}}","browser_download_url":"{{url}}","size":{{size}},"digest":{{(digest is null ? "null" : $"\"{digest}\"")}}}""";

    private static ControlFsSetup? Pick(params string[] releases)
    {
        using var doc = JsonDocument.Parse($"[{string.Join(",", releases)}]");
        return ControlFsRelease.Pick(doc.RootElement);
    }

    [Fact]
    public void Picks_the_x64_setup_of_the_newest_release()
    {
        var setup = Pick(
            Release("v0.17.0-alpha.1",
                Asset("ControlFS-Portable-x64.exe", Url.Replace("Setup", "Portable"), Digest) + "," + Asset("ControlFS-Setup-x64.exe", Url, Digest)),
            Release("v0.16.0-alpha.1", Asset("ControlFS-Setup-x64.exe", Url.Replace("0.17", "0.16"), Digest)));

        Assert.NotNull(setup);
        Assert.Equal("0.17.0-alpha.1", setup.Version);
        Assert.Equal(Url, setup.Url);
        Assert.Equal(Digest, setup.Digest);
        Assert.Equal(49030428, setup.Size);
    }

    [Fact]
    public void A_draft_is_skipped_for_the_next_published_release()
    {
        var setup = Pick(
            Release("v0.18.0", Asset("ControlFS-Setup-x64.exe", Url.Replace("0.17.0-alpha.1", "0.18.0"), Digest), draft: true),
            Release("v0.17.0-alpha.1", Asset("ControlFS-Setup-x64.exe", Url, Digest)));
        Assert.Equal("0.17.0-alpha.1", setup?.Version);
    }

    [Fact]
    public void A_release_without_the_setup_is_skipped()
    {
        var setup = Pick(
            Release("v0.18.0", Asset("release-manifest.json", Url, Digest)),
            Release("v0.17.0-alpha.1", Asset("ControlFS-Setup-x64.exe", Url, Digest)));
        Assert.Equal("0.17.0-alpha.1", setup?.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha256:abc")]
    [InlineData("md5:b97a98bb42b56c572f92e08003c251a541e8d740a5769b4ba85cf686a1095347")]
    public void A_setup_without_a_sha256_digest_is_never_installed(string? digest)
    {
        // The older release is not used instead: the newest setup decides.
        Assert.Null(Pick(
            Release("v0.17.0-alpha.1", Asset("ControlFS-Setup-x64.exe", Url, digest)),
            Release("v0.16.0-alpha.1", Asset("ControlFS-Setup-x64.exe", Url, Digest))));
    }

    [Theory]
    [InlineData("http://github.com/nextestudios/ControlFS/releases/download/v1/ControlFS-Setup-x64.exe")]
    [InlineData("https://github.com/someone-else/ControlFS/releases/download/v1/ControlFS-Setup-x64.exe")]
    [InlineData("https://example.com/ControlFS-Setup-x64.exe")]
    [InlineData("https://github.com/nextestudios/ControlFS-evil/releases/download/v1/ControlFS-Setup-x64.exe")]
    public void A_setup_from_anywhere_but_the_repository_downloads_is_never_installed(string url)
    {
        Assert.Null(Pick(Release("v0.17.0-alpha.1", Asset("ControlFS-Setup-x64.exe", url, Digest))));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[1, \"x\", null]")]
    [InlineData("[{\"tag_name\":\"v1\"}]")]
    public void Unexpected_json_gives_nothing(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Null(ControlFsRelease.Pick(doc.RootElement));
    }
}
