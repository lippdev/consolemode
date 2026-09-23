using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class UpdateIntegrityTests
{
    private const string HelloSha256 = "sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    [Fact]
    public async Task VerifyFileAsync_AcceptsMatchingSha256()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "hello");
            Assert.True(await UpdateIntegrity.VerifyFileAsync(path, HelloSha256));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task VerifyFileAsync_RejectsMismatchedSha256()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "hello");
            Assert.False(await UpdateIntegrity.VerifyFileAsync(path, "sha256:" + new string('0', 64)));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha1:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    [InlineData("sha256:bad")]
    public async Task VerifyFileAsync_RejectsMissingOrMalformedDigest(string? digest)
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "hello");
            Assert.False(await UpdateIntegrity.VerifyFileAsync(path, digest));
        }
        finally { File.Delete(path); }
    }
}
