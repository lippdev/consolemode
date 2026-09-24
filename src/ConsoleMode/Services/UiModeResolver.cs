namespace ConsoleMode.Services;

/// <summary>Which of the two interfaces to show. Pure so the tests can cover it.</summary>
public static class UiModeResolver
{
    public const string Auto = "auto";
    public const string Desktop = "desktop";
    public const string Console = "console";

    /// <summary>
    /// "console" and "desktop" are explicit. "auto" (or anything unknown) picks the console
    /// interface when a controller is connected or the app was opened from the controller.
    /// </summary>
    public static bool IsConsole(string? uiMode, bool controllerConnected, bool launchedByController) =>
        uiMode?.ToLowerInvariant() switch
        {
            Console => true,
            Desktop => false,
            _ => controllerConnected || launchedByController
        };

    /// <summary>Index of the next (or previous) item, wrapping around; -1 for an empty list.</summary>
    public static int Cycle(int count, int current, int step)
    {
        if (count <= 0) return -1;
        if (current < 0) return step >= 0 ? 0 : count - 1;
        return ((current + step) % count + count) % count;
    }
}
