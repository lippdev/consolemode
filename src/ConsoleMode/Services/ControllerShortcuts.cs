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

    /// <summary>Whether any one device holds the full combo.</summary>
    public static bool IsHeldOnAnyDevice(ushort mask, IEnumerable<ushort> deviceStates) =>
        mask != 0 && deviceStates.Any(buttons => (buttons & mask) == mask);

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
/// Captures one device's buttons until that device is released, then reports the combo.
/// Each device arms independently after a neutral poll, so an unrelated held pad cannot delay
/// capture and inputs from different devices are never combined.
/// </summary>
public sealed class ShortcutCapture
{
    private readonly HashSet<string> _releasedSinceStart = new(StringComparer.OrdinalIgnoreCase);
    private string? _capturingDevice;
    private ushort _seen;

    /// <summary>The current allowed buttons on the device being captured.</summary>
    public ushort CurrentHeld { get; private set; }

    /// <summary>Feed one poll of device IDs and states; returns the combo on release/disconnect.</summary>
    public ushort? Feed(IReadOnlyDictionary<string, ushort> devices)
    {
        CurrentHeld = 0;
        foreach (var id in _releasedSinceStart.Where(id => !devices.ContainsKey(id)).ToArray())
            _releasedSinceStart.Remove(id);

        if (_capturingDevice is not null)
        {
            if (!devices.TryGetValue(_capturingDevice, out var held) ||
                (held &= ControllerShortcuts.Allowed) == 0)
            {
                var device = _capturingDevice;
                _capturingDevice = null;
                if (devices.ContainsKey(device)) _releasedSinceStart.Add(device);
                else _releasedSinceStart.Remove(device);
                var result = _seen;
                _seen = 0;
                return result == 0 ? null : result;
            }
            CurrentHeld = held;
            _seen |= held;
            return null;
        }

        foreach (var (id, buttons) in devices.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var held = (ushort)(buttons & ControllerShortcuts.Allowed);
            if (held == 0)
            {
                _releasedSinceStart.Add(id);
                continue;
            }
            if (!_releasedSinceStart.Contains(id)) continue;
            _capturingDevice = id;
            CurrentHeld = held;
            _seen = held;
            break;
        }
        return null;
    }
}
