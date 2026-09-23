using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

public enum ControllerAction
{
    Confirm,
    Back
}

public enum ControllerFamily
{
    None,
    Xbox,
    PlayStation,
    Other
}

/// <summary>
/// Polls game controllers for "confirm" / "back" presses while a prompt is open.
/// Xbox-style pads are read through XInput (works even without window focus); PlayStation
/// (DualShock 4 / DualSense) and other HID pads through Windows.Gaming.Input.RawGameController,
/// which Windows only feeds while this app owns the foreground window.
/// </summary>
public sealed class ControllerInput : IDisposable
{
    private const ushort SonyVendorId = 0x054C;
    private const ushort NintendoVendorId = 0x057E;
    private const ushort XInputA = 0x1000;
    private const ushort XInputB = 0x2000;

    private readonly DispatcherQueueTimer _timer;
    private bool _primed;
    private bool _prevConfirm;
    private bool _prevBack;

    public event Action<ControllerAction>? Pressed;

    public ControllerInput(DispatcherQueue dispatcher)
    {
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(30);
        _timer.Tick += (_, _) => Poll();
    }

    /// <summary>
    /// RawGameController.RawGameControllers fills in asynchronously after first use;
    /// touching it at startup means the list is ready when a prompt shows up.
    /// </summary>
    public static void Warmup()
    {
        try
        {
            _ = RawGameController.RawGameControllers.Count;
            RawGameController.RawGameControllerAdded += (_, c) => AppLog.Write($"Controle conectado: {c.DisplayName} (VID {c.HardwareVendorId:X4})");
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controles: Windows.Gaming.Input indisponível: {ex.Message}");
        }
    }

    /// <summary>Which button symbols to show: PlayStation wins if one is plugged in.</summary>
    public static ControllerFamily DetectFamily()
    {
        try
        {
            foreach (var raw in RawGameController.RawGameControllers)
            {
                if (raw.HardwareVendorId == SonyVendorId) return ControllerFamily.PlayStation;
            }
        }
        catch { /* WGI missing: fall through to XInput */ }

        for (uint i = 0; i < 4; i++)
        {
            if (TryGetXInput(i, out _)) return ControllerFamily.Xbox;
        }

        try
        {
            if (RawGameController.RawGameControllers.Count > 0) return ControllerFamily.Other;
        }
        catch { /* ignore */ }
        return ControllerFamily.None;
    }

    public void Start()
    {
        _primed = false;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Stop();

    private void Poll()
    {
        bool confirm = false, back = false;
        try
        {
            ReadXInput(ref confirm, ref back);
            ReadRaw(ref confirm, ref back);
        }
        catch (Exception ex)
        {
            AppLog.Write($"Controles: {ex.Message}");
            _timer.Stop();
            return;
        }

        // First sample only records state, so a button already held when the prompt opens
        // (e.g. the A that launched console mode) doesn't answer it.
        if (_primed)
        {
            if (confirm && !_prevConfirm) Pressed?.Invoke(ControllerAction.Confirm);
            else if (back && !_prevBack) Pressed?.Invoke(ControllerAction.Back);
        }
        _primed = true;
        _prevConfirm = confirm;
        _prevBack = back;
    }

    private static void ReadXInput(ref bool confirm, ref bool back)
    {
        for (uint i = 0; i < 4; i++)
        {
            if (!TryGetXInput(i, out var buttons)) continue;
            confirm |= (buttons & XInputA) != 0;
            back |= (buttons & XInputB) != 0;
        }
    }

    private static void ReadRaw(ref bool confirm, ref bool back)
    {
        foreach (var raw in RawGameController.RawGameControllers)
        {
            // Xbox pads also show up here; XInput already covers them.
            if (Gamepad.FromGameController(raw) is not null) continue;
            if (raw.ButtonCount < 2) continue;

            var buttons = new bool[raw.ButtonCount];
            var switches = new GameControllerSwitchPosition[raw.SwitchCount];
            var axes = new double[raw.AxisCount];
            raw.GetCurrentReading(buttons, switches, axes);

            // HID button order: Sony = Square, Cross, Circle, Triangle…;
            // Nintendo = B, A, Y, X… (A confirms, B goes back in both layouts).
            var (confirmIndex, backIndex) = raw.HardwareVendorId switch
            {
                SonyVendorId => (1, 2),
                NintendoVendorId => (1, 0),
                _ => (0, 1)
            };
            if (buttons.Length > Math.Max(confirmIndex, backIndex))
            {
                confirm |= buttons[confirmIndex];
                back |= buttons[backIndex];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint userIndex, out XInputState state);

    private static bool TryGetXInput(uint index, out ushort buttons)
    {
        buttons = 0;
        try
        {
            if (XInputGetState(index, out var state) != 0) return false;
            buttons = state.Gamepad.wButtons;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
}
