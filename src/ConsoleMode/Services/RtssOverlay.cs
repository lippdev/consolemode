using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>What the custom FPS counter shows and how (Settings → FPS counter → Custom).</summary>
public sealed class FpsOverlayLayout
{
    public bool ShowApi { get; set; } = true;
    public bool ShowFps { get; set; } = true;
    public bool ShowFrameTime { get; set; } = true;
    /// <summary>Everything on one line instead of one item per line.</summary>
    public bool SingleLine { get; set; }
    /// <summary>RRGGBB of the numbers; one of <see cref="RtssOverlay.Colors"/>.</summary>
    public string Color { get; set; } = RtssOverlay.Colors[0];
}

/// <summary>
/// Our FPS counter on the game, drawn by RTSS in its own OSD slot (<see cref="Owner"/>).
/// Every style except <see cref="External"/> takes the screen: MSI Afterburner's OSD is hidden
/// while it is on, so "Off" really means nothing on screen. External leaves Afterburner/RTSS alone.
/// </summary>
public static partial class RtssOverlay
{
    public const string External = "external";
    public const string Off = "off";
    public const string Compact = "compact";
    public const string Detailed = "detailed";
    public const string Custom = "custom";

    /// <summary>szOSDOwner of our slot.</summary>
    public const string Owner = "ConsoleMode";

    /// <summary>Menu order; External first, the default, so nobody's Afterburner disappears unasked.</summary>
    public static readonly string[] Styles = [External, Off, Compact, Detailed, Custom];

    /// <summary>White, blue, green, yellow, orange.</summary>
    public static readonly string[] Colors = ["FFFFFF", "4CC2FF", "6FE07A", "FFD54A", "FF8A3D"];

    public static string Normalize(string? style) =>
        Styles.FirstOrDefault(s => string.Equals(s, style, StringComparison.OrdinalIgnoreCase)) ?? External;

    /// <summary>A on the session menu row walks <see cref="Styles"/> and wraps around.</summary>
    public static string Next(string? style) => Styles[(Array.IndexOf(Styles, Normalize(style)) + 1) % Styles.Length];

    /// <summary>Whether this style keeps Afterburner's OSD off the screen.</summary>
    public static bool HidesOthers(string? style) => Normalize(style) != External;

    /// <summary>A color from the config, back to white if it isn't plain RRGGBB (it goes inside an RTSS tag).</summary>
    public static string NormalizeColor(string? color) =>
        color is not null && HexColor().IsMatch(color) ? color.ToUpperInvariant() : Colors[0];

    // RTSS hypertext: <Cn=RRGGBB> / <Sn=size> define, <Cn>…<C> / <Sn>…<S> use; <FR> framerate,
    // <FT> frametime and <APP> 3D API are filled in by RTSS for the game being drawn.
    private const string Api = "<C2><APP><C>";
    private const string Fps = "<C0><FR><C><C1><S0> FPS<S><C>";
    private const string FrameTime = "<C0><FT><C><C1><S0> ms<S><C>";

    /// <summary>The OSD text for a style; empty for Off and External (nothing of ours on screen).</summary>
    public static string Text(string? style, FpsOverlayLayout? layout = null)
    {
        string[] items;
        var color = Colors[0];
        var separator = "\n";
        switch (Normalize(style))
        {
            case Compact:
                items = [Fps];
                break;
            case Detailed:
                items = [Api, Fps, FrameTime];
                break;
            case Custom:
                layout ??= new FpsOverlayLayout();
                items = [.. new[] { (layout.ShowApi, Api), (layout.ShowFps, Fps), (layout.ShowFrameTime, FrameTime) }
                    .Where(i => i.Item1).Select(i => i.Item2)];
                if (items.Length == 0) items = [Fps];   // nothing ticked still shows something
                color = NormalizeColor(layout.Color);
                if (layout.SingleLine) separator = "   ";
                break;
            default:
                return "";
        }
        return $"<C0={color}><C1=9AA4B2><C2=4CC2FF><S0=-55>" + string.Join(separator, items);
    }

    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
