namespace ConsoleMode.Services;

/// <summary>What a controller shortcut does.</summary>
public enum ShortcutSlot
{
    /// <summary>Idle in the tray: enter console mode.</summary>
    Home,
    /// <summary>In a session: open the menu over the game.</summary>
    Menu,
    /// <summary>In a session: go back to the PC (restore the desk).</summary>
    Exit
}

/// <summary>
/// Pure rules for the shortcuts the user picks: which buttons are allowed, what a combo is
/// called, which two shortcuts collide and how a "press the buttons you want" capture ends.
/// Buttons are XInput bits (Sony pads are translated to the same bits), 0 = not set.
/// </summary>
public static class ControllerShortcuts
{
    public const ushort Guide = 0x0400;
    public const ushort Start = 0x0010;
    public const ushort Back = 0x0020;
    public const ushort A = 0x1000;
    public const ushort B = 0x2000;
    public const ushort X = 0x4000;
    public const ushort Y = 0x8000;

    /// <summary>
    /// Buttons a shortcut can use: the ones every pad reports, on XInput and on Sony HID alike.
    /// The D-pad is left out because a Sony pad's left stick also sets those bits.
    /// </summary>
    public const ushort Allowed = Guide | Start | Back | A | B | X | Y;

    public static readonly ShortcutSlot[] Slots = [ShortcutSlot.Home, ShortcutSlot.Menu, ShortcutSlot.Exit];

    /// <summary>The combo offered (never applied by itself) for each shortcut.</summary>
    public static ushort Suggested(ShortcutSlot slot) => slot switch
    {
        ShortcutSlot.Home => Guide,
        ShortcutSlot.Menu => Back | Y,
        _ => Start | Back
    };

    /// <summary>Config value to a valid mask: unknown bits are dropped, junk becomes "not set".</summary>
    public static ushort Sanitize(int raw) => raw is < 0 or > ushort.MaxValue ? (ushort)0 : (ushort)(raw & Allowed);

    public static int ButtonCount(ushort mask) => System.Numerics.BitOperations.PopCount(mask);

    /// <summary>
    /// A combo of two or more buttons, or the Guide button alone. A single face button would
    /// fire in the middle of any game.
    /// </summary>
    public static bool IsAcceptable(ushort mask) =>
        mask != 0 && (mask & ~Allowed) == 0 && ((mask & Guide) != 0 || ButtonCount(mask) >= 2);

    /// <summary>Two combos collide when one is the other or contains it: both would fire together.</summary>
    public static bool Overlaps(ushort a, ushort b)
    {
        if (a == 0 || b == 0) return false;
        var both = a & b;
        return both == a || both == b;
    }

    /// <summary>The other shortcut that <paramref name="mask"/> would collide with, if any.</summary>
    /// <param name="current">Saved masks indexed by <see cref="ShortcutSlot"/>.</param>
    public static ShortcutSlot? FindConflict(ShortcutSlot slot, ushort mask, IReadOnlyList<ushort> current)
    {
        foreach (var other in Slots)
        {
            if (other != slot && Overlaps(mask, current[(int)other])) return other;
        }
        return null;
    }

    /// <summary>"Select + Y" on an Xbox pad, "Create + △" on a PlayStation one; "" when not set.</summary>
    public static string Format(ushort mask, bool playStation)
    {
        var parts = new List<string>();
        void Add(ushort bit, string xbox, string sony)
        {
            if ((mask & bit) != 0) parts.Add(playStation ? sony : xbox);
        }
        Add(Guide, "Xbox", "PS");
        Add(Back, "Select", "Create");
        Add(Start, "Start", "Options");
        Add(A, "A", "✕");
        Add(B, "B", "○");
        Add(X, "X", "□");
        Add(Y, "Y", "△");
        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Turns polled button states into one combo: every button pressed until the pad is let go
/// counts, and the combo is reported on release. Starts only after a poll with nothing held,
/// so the press that opened the capture (a click, the A of the gamepad navigation) is not it.
/// </summary>
public sealed class ShortcutCapture
{
    private bool _armed;
    private ushort _seen;

    /// <summary>Feed the buttons held right now; returns the combo once it is complete.</summary>
    public ushort? Feed(ushort held)
    {
        held &= ControllerShortcuts.Allowed;
        if (!_armed)
        {
            if (held == 0) _armed = true;
            return null;
        }
        if (held != 0)
        {
            _seen |= held;
            return null;
        }
        if (_seen == 0) return null;
        var result = _seen;
        _seen = 0;
        return result;
    }
}
