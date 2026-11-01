namespace ConsoleMode.Services;

/// <summary>
/// How each launcher looks as a tile in the console Home row: a Segoe Fluent glyph and the two colours
/// of its gradient (ARGB). Unknown modes get the neutral tile, so a new mode never shows up blank.
/// </summary>
public static class LaunchArt
{
    public readonly record struct Art(string Glyph, uint From, uint To);

    public static readonly Art Neutral = new("", 0xFF2A3038, 0xFF161A20);

    /// <summary>The Xbox app, whose logo stands for the Xbox mode (the glyph shows if it is not installed).</summary>
    public const string XboxAppId = "Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App";

    /// <summary>The last tile of the Home row: adds a custom app (a picked exe) instead of opening anything.</summary>
    public const string AddCustomApp = "addCustomApp";

    public static Art For(string? mode) => mode switch
    {
        AddCustomApp => new("\uE710", 0x33FFFFFF, 0x14FFFFFF),
        // Each custom app ("customApp:<id>") shows its exe's icon over this tile.
        _ when mode?.StartsWith("customApp:", StringComparison.Ordinal) == true => new("\uE756", 0xFF3A4250, 0xFF1A1E25),
        "bigPicture" => new("", 0xFF2A475E, 0xFF15202C),
        "playnite" => new("", 0xFF4A3470, 0xFF1D1530),
        "xboxMode" => new("", 0xFF178A17, 0xFF0A440A),
        "customApp" => new("", 0xFF3A4250, 0xFF1A1E25),
        _ => Neutral
    };
}
