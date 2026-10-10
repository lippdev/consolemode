using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMode.Services;

/// <summary>
/// How our FPS counter looks (Settings → FPS counter). Size, card and color apply to every style;
/// the items and the one-line layout are what Custom shows.
/// </summary>
public sealed class FpsOverlayLayout
{
    public bool ShowApi { get; set; } = true;
    public bool ShowFps { get; set; } = true;
    public bool ShowFrameTime { get; set; } = true;
    public bool ShowCpu { get; set; }
    public bool ShowCpuClock { get; set; }
    public bool ShowGpu { get; set; }
    public bool ShowGpuClock { get; set; }
    public bool ShowGpuTemp { get; set; }
    public bool ShowVram { get; set; }
    public bool ShowRam { get; set; }
    /// <summary>Everything on one line instead of one row per group.</summary>
    public bool SingleLine { get; set; }
    /// <summary>RRGGBB of the numbers; one of <see cref="RtssOverlay.Colors"/>.</summary>
    public string Color { get; set; } = RtssOverlay.Colors[0];
    /// <summary>One of <see cref="RtssOverlay.Sizes"/>.</summary>
    public string Size { get; set; } = RtssOverlay.Small;
    /// <summary>Draw the counter on a translucent rounded card instead of straight over the game.</summary>
    public bool Card { get; set; } = true;
    /// <summary>One of <see cref="RtssOverlay.Positions"/>: where RTSS puts its OSD, or a corner of the screen.</summary>
    public string Position { get; set; } = RtssOverlay.PositionRtss;
}

/// <summary>
/// Our FPS counter on the game, drawn by RTSS in its own OSD slot (<see cref="Owner"/>).
/// Every style except <see cref="External"/> takes the screen: MSI Afterburner's OSD is hidden
/// while it is on, so "Off" really means nothing on screen. External leaves Afterburner/RTSS alone.
/// FPS and frame time come from RTSS itself; CPU, GPU and memory from a <see cref="HardwareSample"/>,
/// so styles that show them are rewritten every second (<see cref="NeedsHardware"/>).
/// </summary>
public static partial class RtssOverlay
{
    public const string External = "external";
    public const string Off = "off";
    /// <summary>Only the FPS. Its id is "compact", the name this style had up to 1.6.0, so saved configs keep it.</summary>
    public const string FpsOnly = "compact";
    /// <summary>FPS plus CPU, GPU and RAM usage on one line.</summary>
    public const string Compact = "stats";
    public const string Detailed = "detailed";
    public const string Custom = "custom";

    /// <summary>szOSDOwner of our slot.</summary>
    public const string Owner = "ConsoleMode";

    /// <summary>Menu order; External first, the default, so nobody's Afterburner disappears unasked.</summary>
    public static readonly string[] Styles = [External, Off, FpsOnly, Compact, Detailed, Custom];

    /// <summary>White, blue, green, yellow, orange.</summary>
    public static readonly string[] Colors = ["FFFFFF", "4CC2FF", "6FE07A", "FFD54A", "FF8A3D"];

    public const string Small = "small";
    public const string Medium = "medium";
    public const string Large = "large";
    public static readonly string[] Sizes = [Small, Medium, Large];

    public const string PositionRtss = "rtss";
    public const string TopLeft = "top-left";
    public const string TopRight = "top-right";
    public const string BottomLeft = "bottom-left";
    public const string BottomRight = "bottom-right";
    /// <summary>The RTSS position first (the default): it keeps whatever the user set in RTSS.</summary>
    public static readonly string[] Positions = [PositionRtss, TopLeft, TopRight, BottomLeft, BottomRight];

    public static string Normalize(string? style) =>
        Styles.FirstOrDefault(s => string.Equals(s, style, StringComparison.OrdinalIgnoreCase)) ?? External;

    /// <summary>Whether this style keeps Afterburner's OSD off the screen.</summary>
    public static bool HidesOthers(string? style) => Normalize(style) != External;

    /// <summary>A color from the config, back to white if it isn't plain RRGGBB (it goes inside an RTSS tag).</summary>
    public static string NormalizeColor(string? color) =>
        color is not null && HexColor().IsMatch(color) ? color.ToUpperInvariant() : Colors[0];

    public static string NormalizeSize(string? size) =>
        Sizes.FirstOrDefault(s => string.Equals(s, size, StringComparison.OrdinalIgnoreCase)) ?? Small;

    public static string NormalizePosition(string? position) =>
        Positions.FirstOrDefault(p => string.Equals(p, position, StringComparison.OrdinalIgnoreCase)) ?? PositionRtss;

    /// <summary>Whether the style shows CPU/GPU/memory, which RTSS doesn't know: the text then needs a fresh sample every second.</summary>
    public static bool NeedsHardware(string? style, FpsOverlayLayout? layout = null) =>
        (ItemsOf(Normalize(style), layout) & PcItems) != 0;

    [Flags]
    private enum Item
    {
        None = 0,
        Fps = 1, FrameTime = 2, Api = 4,
        Cpu = 8, CpuClock = 16, Gpu = 32, GpuClock = 64, GpuTemp = 128, Vram = 256, Ram = 512
    }

    private const Item PcItems = Item.Cpu | Item.CpuClock | Item.Gpu | Item.GpuClock | Item.GpuTemp | Item.Vram | Item.Ram;

    private static Item ItemsOf(string style, FpsOverlayLayout? layout)
    {
        switch (style)
        {
            case FpsOnly: return Item.Fps;
            case Compact: return Item.Fps | Item.Cpu | Item.Gpu | Item.Ram;
            case Detailed: return Item.Fps | Item.FrameTime | Item.Api | PcItems;
            case Custom:
                layout ??= new FpsOverlayLayout();
                var items = Item.None;
                if (layout.ShowFps) items |= Item.Fps;
                if (layout.ShowFrameTime) items |= Item.FrameTime;
                if (layout.ShowApi) items |= Item.Api;
                if (layout.ShowCpu) items |= Item.Cpu;
                if (layout.ShowCpuClock) items |= Item.CpuClock;
                if (layout.ShowGpu) items |= Item.Gpu;
                if (layout.ShowGpuClock) items |= Item.GpuClock;
                if (layout.ShowGpuTemp) items |= Item.GpuTemp;
                if (layout.ShowVram) items |= Item.Vram;
                if (layout.ShowRam) items |= Item.Ram;
                return items == Item.None ? Item.Fps : items;   // nothing ticked still shows something
            default:
                return Item.None;
        }
    }

    // RTSS hypertext (SDK sample and OverlayEditor): <Cn=AARRGGBB> / <Sn=size> / <An=width> define a
    // variable, <Cn>…<C> / <Sn>…<S> / <An>…<A> use it (<S> and <C> go back to 100% / default color).
    // Sizes are percent, negative = sits on the baseline; widths are in characters, negative = right
    // aligned, so numbers don't make the text jump. <FR> framerate, <FT> frametime and <APP> 3D API
    // are filled in by RTSS. The card is a layer (<L>) with padding (<M>) whose background is a
    // rounded bar (<B=0,0,Rn>, 0,0 = the whole layer) drawn in C3; \b puts the cursor back on its origin.
    private const string Value = "C0", Label = "C1", Accent = "C2", CardFill = "C3";
    private const string ValueSize = "S0", UnitSize = "S1", LabelSize = "S2", FpsSize = "S3";
    private const string LabelColor = "9AA4B2", AccentColor = "4CC2FF", CardColor = "C0101418";
    private const string Missing = "--";

    /// <summary>
    /// The OSD text for a style; empty for Off and External (nothing of ours on screen).
    /// <paramref name="sample"/> null = not measured yet: hardware values show as "--".
    /// The text is Latin-1 (°), what RTSS's slot holds.
    /// </summary>
    public static string Text(string? style, FpsOverlayLayout? layout = null, HardwareSample? sample = null)
    {
        style = Normalize(style);
        var items = ItemsOf(style, layout);
        if (items == Item.None) return "";
        layout ??= new FpsOverlayLayout();
        var singleLine = style switch
        {
            FpsOnly or Compact => true,
            Custom => layout.SingleLine,
            _ => false
        };

        var scale = NormalizeSize(layout.Size) switch { Medium => 1.5, Large => 2.0, _ => 1.0 };
        var rows = new List<string>();
        var game = new List<string>();
        if (items.HasFlag(Item.Fps)) game.Add(Run(FpsSize, Value, "<FR>") + Run(UnitSize, Label, " FPS"));
        if (items.HasFlag(Item.FrameTime)) game.Add(Run(ValueSize, Value, "<FT>") + Run(UnitSize, Label, " ms"));
        if (items.HasFlag(Item.Api)) game.Add(Run(LabelSize, Accent, "<APP>"));
        if (game.Count > 0) rows.Add(string.Join(Gap, game));

        // Compact is one short line: RAM as a percentage, like the other two.
        var ramAsPercent = style == Compact;
        AddRow(rows, "CPU", singleLine,
            items.HasFlag(Item.Cpu) ? Number(sample, sample?.CpuUsage, "F0", 3, "%") : null,
            items.HasFlag(Item.CpuClock) ? Number(sample, sample?.CpuClockMhz / 1000, "F1", 3, " GHz") : null);
        AddRow(rows, "GPU", singleLine,
            items.HasFlag(Item.Gpu) ? Number(sample, sample?.GpuUsage, "F0", 3, "%") : null,
            items.HasFlag(Item.GpuClock) ? Number(sample, sample?.GpuClockMhz, "F0", 4, " MHz") : null,
            items.HasFlag(Item.GpuTemp) ? Number(sample, sample?.GpuTempC, "F0", 3, "°C") : null);
        AddRow(rows, "VRAM", singleLine,
            items.HasFlag(Item.Vram) ? Memory(sample, sample?.VramUsedMb, sample?.VramTotalMb, false) : null);
        AddRow(rows, "RAM", singleLine,
            items.HasFlag(Item.Ram) ? Memory(sample, sample?.RamUsedMb, sample?.RamTotalMb, ramAsPercent) : null);
        if (rows.Count == 0) rows.Add(Run(FpsSize, Value, "<FR>") + Run(UnitSize, Label, " FPS"));

        var header = new StringBuilder()
            .Append($"<{Value}={NormalizeColor(layout.Color)}><{Label}={LabelColor}><{Accent}={AccentColor}><{CardFill}={CardColor}>")
            .Append($"<{ValueSize}={Percent(100, scale)}><{UnitSize}=-{Percent(55, scale)}><{LabelSize}=-{Percent(70, scale)}>")
            .Append($"<{FpsSize}={Percent(singleLine ? 100 : 130, scale)}>")
            .Append("<A0=-3><A1=-4><A2=5>");
        // A corner is a sticky position (<P0> top left, <P2> top right, <P6> bottom left, <P8> bottom
        // right of RTSS's 3x3 screen grid), which ignores the OSD position set in RTSS; it needs a layer.
        var corner = NormalizePosition(layout.Position) switch
        {
            TopLeft => "<P0>", TopRight => "<P2>", BottomLeft => "<P6>", BottomRight => "<P8>", _ => ""
        };
        header.Append(corner);
        if (layout.Card)
        {
            var pad = (int)Math.Round(6 * scale);
            header.Append($"<M={pad + 2},{pad},{pad + 2},{pad}><L0><{CardFill}><B=0,0,R{pad + 2}>\b<C>");
        }
        else if (corner.Length > 0)
        {
            header.Append("<L0>");
        }
        return header + string.Join(singleLine ? Gap + Gap : "\n", rows);
    }

    private static string Gap => Run(LabelSize, Label, "  ");

    private static string Run(string size, string color, string text) => $"<{size}><{color}>{text}<C><S>";

    private static string Percent(int percent, double scale) =>
        ((int)Math.Round(percent * scale)).ToString(CultureInfo.InvariantCulture);

    /// <summary>"CPU 45% 4.2 GHz": the label column is fixed-width when stacked so the values line up.</summary>
    private static void AddRow(List<string> rows, string label, bool singleLine, params string?[] values)
    {
        var shown = values.Where(v => !string.IsNullOrEmpty(v)).ToList();
        if (shown.Count == 0) return;
        var name = singleLine ? Run(LabelSize, Label, label + " ") : $"<{LabelSize}><{Label}><A2>{label}<A><C><S>";
        rows.Add(name + string.Join(Gap, shown));
    }

    /// <summary>A right-aligned value in a box of <paramref name="width"/> characters, then its unit. "" = not available here.</summary>
    private static string Number(HardwareSample? sample, double? value, string format, int width, string unit)
    {
        string text;
        if (sample is null) text = Missing;
        else if (value is { } v && double.IsFinite(v)) text = v.ToString(format, CultureInfo.InvariantCulture);
        else return "";
        var box = width >= 4 ? "A1" : "A0";
        return $"<{ValueSize}><{Value}><{box}>{text}<A><C><S>" + Run(UnitSize, Label, unit);
    }

    /// <summary>"5.2 / 8.0 GB", or a percentage on the Compact line.</summary>
    private static string Memory(HardwareSample? sample, double? usedMb, double? totalMb, bool asPercent)
    {
        if (asPercent)
            return Number(sample, usedMb is { } u && totalMb is > 0 ? u / totalMb * 100 : null, "F0", 3, "%");
        if (sample is null) return Number(null, null, "F1", 3, " GB");
        if (usedMb is not { } used) return "";
        var text = Number(sample, used / 1024, "F1", 3, "");
        var total = totalMb is > 0 ? " / " + (totalMb.Value / 1024).ToString("F0", CultureInfo.InvariantCulture) : "";
        return text + Run(UnitSize, Label, total + " GB");
    }

    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
