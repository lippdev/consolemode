namespace ConsoleMode.Services;

/// <summary>Which releases the update check offers. Pure, so it's tested.</summary>
public static class UpdateChannel
{
    /// <summary>
    /// Test versions (alpha/beta) are offered when the user opted in, or when a test version is
    /// already installed. Turning the option off never downgrades: a test build keeps getting test
    /// builds until a newer stable one comes out.
    /// </summary>
    public static bool IncludePrereleases(bool currentIsPrerelease, bool optedIn) => currentIsPrerelease || optedIn;
}
