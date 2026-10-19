namespace ConsoleMode.Services;

/// <summary>
/// How each launcher looks as a tile in the console Home row: a Segoe Fluent glyph and the two colours
/// of its gradient (ARGB), plus a brighter Glow colour lit behind the tile when it is the one that opens.
/// Unknown modes get the neutral tile, so a new mode never shows up blank.
/// </summary>
public static class LaunchArt
{
    public readonly record struct Art(string Glyph, uint From, uint To, uint Glow);

    public static readonly Art Neutral = new("", 0xFF2A3038, 0xFF161A20, 0xFF8A96A8);

    public static Art For(string? mode) => mode switch
    {
        "bigPicture" => new("", 0xFF2A475E, 0xFF15202C, 0xFF66C0F4),
        "playnite" => new("", 0xFF4A3470, 0xFF1D1530, 0xFFA77BFF),
        "xboxMode" => new("", 0xFF178A17, 0xFF0A440A, 0xFF3DDC3D),
        "customApp" => new("", 0xFF3A4250, 0xFF1A1E25, 0xFFA8B4C8),
        _ => Neutral
    };
}
