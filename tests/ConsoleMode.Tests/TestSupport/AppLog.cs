namespace ConsoleMode.Services;

/// <summary>Stands in for the app's AppLog (AppPaths.cs pulls in WinUI-only code).</summary>
public static class AppLog
{
    public static readonly List<string> Lines = [];

    public static void Write(string message) => Lines.Add(message);
}
