namespace ConsoleMode.Services;

/// <summary>
/// Our FPS counter on the game, drawn by RTSS. It lives in its own OSD slot, so another client
/// (MSI Afterburner keeps slot 0) shows next to it and is never touched: we only ever write and
/// clear the slot that carries <see cref="Owner"/>.
/// </summary>
public static class RtssOverlay
{
    public const string Off = "off";
    public const string Compact = "compact";
    public const string Detailed = "detailed";

    /// <summary>szOSDOwner of our slot.</summary>
    public const string Owner = "ConsoleMode";

    public static readonly string[] Styles = [Off, Compact, Detailed];

    public static string Normalize(string? style) =>
        Styles.FirstOrDefault(s => string.Equals(s, style, StringComparison.OrdinalIgnoreCase)) ?? Off;

    /// <summary>Off → Compact → Detailed → Off (A on the menu row).</summary>
    public static string Next(string? style) => Styles[(Array.IndexOf(Styles, Normalize(style)) + 1) % Styles.Length];

    // RTSS hypertext: <Cn=RRGGBB> / <Sn=size> define, <Cn>…<C> / <Sn>…<S> use; <FR> framerate,
    // <FT> frametime and <APP> 3D API are filled in by RTSS for the game being drawn.
    private const string Palette = "<C0=FFFFFF><C1=9AA4B2><C2=4CC2FF><S0=-55>";
    private const string Fps = "<C0><FR><C><C1><S0> FPS<S><C>";

    /// <summary>The OSD text for a style; empty for Off (the slot is released instead).</summary>
    public static string Text(string? style) => Normalize(style) switch
    {
        Compact => Palette + Fps,
        Detailed => Palette + "<C2><APP><C>\n" + Fps + "\n<C0><FT><C><C1><S0> ms<S><C>",
        _ => ""
    };
}
