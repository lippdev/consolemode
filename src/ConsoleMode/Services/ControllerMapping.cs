namespace ConsoleMode.Services;

/// <summary>Pure rules behind ControllerInput, kept apart so the tests cover them.</summary>
public static class ControllerMapping
{
    public const ushort SonyVendorId = 0x054C;
    public const ushort NintendoVendorId = 0x057E;

    /// <summary>DualSense and DualSense Edge; any other Sony pad is read with the DS4 layout.</summary>
    public static bool IsDualSense(ushort productId) => productId is 0x0CE6 or 0x0DF2;

    /// <summary>
    /// XInput-style button bits from a raw DS4/DualSense input report (first byte = report ID):
    /// Cross = A, Circle = B, Square = X, Triangle = Y, Options = Start, Create/Share = Back,
    /// PS = Guide; the hat and the left stick set the D-pad bits. Unknown reports give 0.
    /// </summary>
    public static ushort SonyButtons(ReadOnlySpan<byte> report, bool dualSense)
    {
        if (report.Length == 0) return 0;
        // Offset of the buttons byte (hat + face) and of the left stick X, per report layout.
        var (start, stick) = report[0] switch
        {
            0x31 => (9, 2),                                     // DualSense, Bluetooth
            0x11 => (7, 3),                                     // DS4, Bluetooth
            0x01 when dualSense && report.Length >= 64 => (8, 1), // DualSense, USB
            0x01 => (5, 1),                                     // DS4 USB, or either pad's basic Bluetooth report
            _ => (-1, -1)
        };
        if (start < 0 || report.Length < start + 3) return 0;
        var face = report[start];
        var hat = face & 0x0F;
        ushort bits = 0;
        if ((face & 0x10) != 0) bits |= 0x4000;              // Square -> X
        if ((face & 0x20) != 0) bits |= 0x1000;              // Cross -> A
        if ((face & 0x40) != 0) bits |= 0x2000;              // Circle -> B
        if ((face & 0x80) != 0) bits |= 0x8000;              // Triangle -> Y
        if ((report[start + 1] & 0x10) != 0) bits |= 0x0020; // Create/Share -> Back
        if ((report[start + 1] & 0x20) != 0) bits |= 0x0010; // Options -> Start
        if ((report[start + 2] & 0x01) != 0) bits |= 0x0400; // PS -> Guide
        // Hat: 0 = up, clockwise to 7 = up-left, 8 = released.
        if (hat is 7 or 0 or 1) bits |= 0x0001;
        if (hat is 1 or 2 or 3) bits |= 0x0008;
        if (hat is 3 or 4 or 5) bits |= 0x0002;
        if (hat is 5 or 6 or 7) bits |= 0x0004;
        // Left stick: 0..255, 128 = center, Y grows downward.
        var x = report[stick];
        var y = report[stick + 1];
        if (y < 64) bits |= 0x0001;
        if (y > 192) bits |= 0x0002;
        if (x < 64) bits |= 0x0004;
        if (x > 192) bits |= 0x0008;
        return bits;
    }

    /// <summary>HID button indexes (confirm, back, option, alt, menu) for a vendor's layout.</summary>
    /// <remarks>
    /// Sony (DS4/DualSense): Square 0, Cross 1, Circle 2, Triangle 3, L1 4, R1 5, L2 6, R2 7,
    /// Create 8, Options 9, L3 10, R3 11, PS 12. Nintendo: B 0, A 1, Y 2, X 3 … Plus 9.
    /// A confirms and B goes back in both layouts.
    /// </remarks>
    public static (int Confirm, int Back, int Option, int Alt, int Menu) HidIndices(ushort vendorId) => vendorId switch
    {
        SonyVendorId => (1, 2, 0, 3, 9),
        NintendoVendorId => (1, 0, 3, 2, 9),
        _ => (0, 1, 2, 3, 7)
    };

    /// <summary>
    /// Whether to read a HID pad that Windows also exposes as a Gamepad. Normally XInput covers
    /// those, but when no XInput slot answered (e.g. a DualSense the OS wraps without XInput),
    /// skipping it would leave the pad mute.
    /// </summary>
    public static bool ShouldReadHid(bool isAlsoGamepad, int xinputPads) => !isAlsoGamepad || xinputPads == 0;

    /// <summary>Whether to read Windows.Gaming.Input.Gamepad objects: only when XInput saw nothing.</summary>
    public static bool ShouldReadGamepads(int xinputPads) => xinputPads == 0;

    /// <summary>Stick axis from 0..1 (0.5 centred) to a direction; ±0.25 dead zone.</summary>
    public static (bool Left, bool Right, bool Up, bool Down) StickDirections(double x, double y) =>
        (x < 0.25, x > 0.75, y < 0.25, y > 0.75);
}
