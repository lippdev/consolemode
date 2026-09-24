namespace ConsoleMode.Services;

/// <summary>Pure rules behind ControllerInput, kept apart so the tests cover them.</summary>
public static class ControllerMapping
{
    public const ushort SonyVendorId = 0x054C;
    public const ushort NintendoVendorId = 0x057E;

    /// <summary>DualSense and DualSense Edge; any other Sony pad is read with the DS4 layout.</summary>
    public static bool IsDualSense(ushort productId) => productId is 0x0CE6 or 0x0DF2;

    /// <summary>
    /// XInput-style bits (Guide, Start, Back, Y) from a raw DS4/DualSense input report, first
    /// byte = report ID. Create/Share maps to Back, Options to Start, Triangle to Y, PS to Guide.
    /// Unknown reports give 0.
    /// </summary>
    public static ushort SonyChordButtons(ReadOnlySpan<byte> report, bool dualSense)
    {
        if (report.Length == 0) return 0;
        var start = report[0] switch
        {
            0x31 => 9,                                    // DualSense, Bluetooth
            0x11 => 7,                                    // DS4, Bluetooth
            0x01 when dualSense && report.Length >= 64 => 8, // DualSense, USB
            0x01 => 5,                                    // DS4 USB, or either pad's basic Bluetooth report
            _ => -1
        };
        if (start < 0 || report.Length < start + 3) return 0;
        ushort bits = 0;
        if ((report[start] & 0x80) != 0) bits |= 0x8000;     // Triangle -> Y
        if ((report[start + 1] & 0x10) != 0) bits |= 0x0020; // Create/Share -> Back
        if ((report[start + 1] & 0x20) != 0) bits |= 0x0010; // Options -> Start
        if ((report[start + 2] & 0x01) != 0) bits |= 0x0400; // PS -> Guide
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
