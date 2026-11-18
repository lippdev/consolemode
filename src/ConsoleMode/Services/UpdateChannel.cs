namespace ConsoleMode.Services;

/// <summary>Which releases the update check offers. Pure, so it's tested.</summary>
public static class UpdateChannel
{
    /// <summary>
    /// Test versions (beta) are offered when the user opted in, or when a test version is
    /// already installed. Turning the option off never downgrades: a test build keeps getting test
    /// builds until a newer stable one comes out.
    /// </summary>
    public static bool IncludePrereleases(bool currentIsPrerelease, bool optedIn) => currentIsPrerelease || optedIn;

    /// <summary>
    /// Whether a version's pre-release label (the "beta" of 1.7.1-beta.2, empty for a stable one) can be offered at all.
    /// Betas and release candidates go to testers; alphas never do, whatever the setting: the 2.0 alphas are a closed
    /// test of another app and must not reach the 1.x test channel.
    /// </summary>
    public static bool IsOfferedLabel(string? preLabel) =>
        string.IsNullOrEmpty(preLabel)
        || preLabel.StartsWith("beta", StringComparison.OrdinalIgnoreCase)
        || preLabel.StartsWith("rc", StringComparison.OrdinalIgnoreCase);
}
