namespace ConsoleMode.Services;

/// <summary>
/// This build is "Legacy": Console Mode 1.6.2 with both interfaces, published next to 1.7.0 for those who prefer
/// the old one. It is frozen, so it has nothing to update to.
/// </summary>
public static class AppEdition
{
    public const string Name = "Legacy";

    /// <summary>Never: the only newer release is the current app, which a Legacy user chose not to use.</summary>
    public static readonly bool ChecksForUpdates = false;
}
