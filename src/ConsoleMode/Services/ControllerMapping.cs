namespace ConsoleMode.Services;

/// <summary>Pure rules behind ControllerInput, kept apart so the tests cover them.</summary>
public static class ControllerMapping
{
    public const ushort SonyVendorId = 0x054C;
    public const ushort NintendoVendorId = 0x057E;

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
