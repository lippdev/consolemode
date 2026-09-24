using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

public enum ControllerAction
{
    Confirm,
    Back
}

/// <summary>Buttons as the app sees them, whatever the pad (Cross = A, Circle = B, Options = Start).</summary>
[Flags]
public enum ControllerButtons
{
    None = 0,
    Confirm = 1,
    Back = 2,
    Up = 4,
    Down = 8,
    Left = 16,
    Right = 32,
    Menu = 64
}

public enum ControllerFamily
{
    None,
    Xbox,
    PlayStation,
    Other
}

/// <summary>
/// Polls game controllers for button presses (prompt answers and window navigation).
/// Xbox-style pads are read through XInput (works even without window focus); PlayStation
/// (DualShock 4 / DualSense) and other HID pads through Windows.Gaming.Input.RawGameController,
/// which Windows only feeds while this app owns the foreground window.
/// </summary>
public sealed class ControllerInput : IDisposable
{
    private const ushort SonyVendorId = 0x054C;
    private const ushort NintendoVendorId = 0x057E;
    private const ushort XInputDpadUp = 0x0001;
    private const ushort XInputDpadDown = 0x0002;
    private const ushort XInputDpadLeft = 0x0004;
    private const ushort XInputDpadRight = 0x0008;
    private const ushort XInputStart = 0x0010;
    private const ushort XInputA = 0x1000;
    private const ushort XInputB = 0x2000;
    private const short XInputStickThreshold = 16000;
    private const double RawStickThreshold = 0.25;
    private const ControllerButtons Directions =
        ControllerButtons.Up | ControllerButtons.Down | ControllerButtons.Left | ControllerButtons.Right;
    private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(120);

    private readonly DispatcherQueueTimer _timer;
    private bool _primed;
    private ControllerButtons _prev;
    private long _nextRepeatTick;
    private bool _loggedError;

    /// <summary>Confirm / Back only, for yes-no prompts.</summary>
    public event Action<ControllerAction>? Pressed;

    /// <summary>
    /// Every newly pressed button; a held direction repeats like a keyboard arrow.
    /// </summary>
    public event Action<ControllerButtons>? ButtonDown;

    /// <summary>
    /// Only read the pads while this returns true (e.g. while a window is in the foreground).
    /// Reading resumes primed, so a button held while inactive doesn't fire.
    /// </summary>
    public Func<bool>? IsActive { get; set; }

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
        if (IsActive is not null && !IsActive())
        {
            _primed = false;
            return;
        }

        ControllerButtons state;
        try
        {
            state = ReadXInput() | ReadRaw();
        }
        catch (Exception ex)
        {
            // A pad unplugged mid-read throws; skip the sample instead of going deaf for good.
            if (!_loggedError) AppLog.Write($"Controles: {ex.Message}");
            _loggedError = true;
            _primed = false;
            return;
        }

        // First sample only records state, so a button already held when the prompt opens
        // (e.g. the A that launched console mode) doesn't answer it.
        if (_primed)
        {
            var pressed = state & ~_prev;
            if ((pressed & ControllerButtons.Confirm) != 0) Pressed?.Invoke(ControllerAction.Confirm);
            else if ((pressed & ControllerButtons.Back) != 0) Pressed?.Invoke(ControllerAction.Back);

            var now = Environment.TickCount64;
            if ((pressed & Directions) != 0) _nextRepeatTick = now + (long)RepeatDelay.TotalMilliseconds;
            else if ((state & Directions) != 0 && now >= _nextRepeatTick)
            {
                pressed |= state & Directions;
                _nextRepeatTick = now + (long)RepeatInterval.TotalMilliseconds;
            }

            if (pressed != ControllerButtons.None) ButtonDown?.Invoke(pressed);
        }
        _primed = true;
        _prev = state;
    }

    private static ControllerButtons ReadXInput()
    {
        var state = ControllerButtons.None;
        for (uint i = 0; i < 4; i++)
        {
            if (!TryGetXInput(i, out var pad)) continue;
            var buttons = pad.wButtons;
            if ((buttons & XInputA) != 0) state |= ControllerButtons.Confirm;
            if ((buttons & XInputB) != 0) state |= ControllerButtons.Back;
            if ((buttons & XInputStart) != 0) state |= ControllerButtons.Menu;
            if ((buttons & XInputDpadUp) != 0 || pad.sThumbLY > XInputStickThreshold) state |= ControllerButtons.Up;
            if ((buttons & XInputDpadDown) != 0 || pad.sThumbLY < -XInputStickThreshold) state |= ControllerButtons.Down;
            if ((buttons & XInputDpadLeft) != 0 || pad.sThumbLX < -XInputStickThreshold) state |= ControllerButtons.Left;
            if ((buttons & XInputDpadRight) != 0 || pad.sThumbLX > XInputStickThreshold) state |= ControllerButtons.Right;
        }
        return state;
    }

    private static ControllerButtons ReadRaw()
    {
        var state = ControllerButtons.None;
        foreach (var raw in RawGameController.RawGameControllers)
        {
            // Xbox pads also show up here; XInput already covers them.
            if (Gamepad.FromGameController(raw) is not null) continue;
            if (raw.ButtonCount < 2) continue;

            var buttons = new bool[raw.ButtonCount];
            var switches = new GameControllerSwitchPosition[raw.SwitchCount];
            var axes = new double[raw.AxisCount];
            raw.GetCurrentReading(buttons, switches, axes);

            // HID button order: Sony = Square, Cross, Circle, Triangle, L1, R1, L2, R2, Create, Options…;
            // Nintendo = B, A, Y, X, L, R, ZL, ZR, Minus, Plus… (A confirms, B goes back in both layouts).
            var (confirmIndex, backIndex, menuIndex) = raw.HardwareVendorId switch
            {
                SonyVendorId => (1, 2, 9),
                NintendoVendorId => (1, 0, 9),
                _ => (0, 1, -1)
            };
            if (IsDown(buttons, confirmIndex)) state |= ControllerButtons.Confirm;
            if (IsDown(buttons, backIndex)) state |= ControllerButtons.Back;
            if (IsDown(buttons, menuIndex)) state |= ControllerButtons.Menu;

            if (switches.Length > 0) state |= FromSwitch(switches[0]);

            // Left stick: axes 0/1 are X/Y from 0 to 1, centred on 0.5, Y growing downwards.
            if (axes.Length >= 2)
            {
                if (axes[0] < 0.5 - RawStickThreshold) state |= ControllerButtons.Left;
                if (axes[0] > 0.5 + RawStickThreshold) state |= ControllerButtons.Right;
                if (axes[1] < 0.5 - RawStickThreshold) state |= ControllerButtons.Up;
                if (axes[1] > 0.5 + RawStickThreshold) state |= ControllerButtons.Down;
            }
        }
        return state;

        static bool IsDown(bool[] buttons, int index) => index >= 0 && index < buttons.Length && buttons[index];
    }

    private static ControllerButtons FromSwitch(GameControllerSwitchPosition position) => position switch
    {
        GameControllerSwitchPosition.Up => ControllerButtons.Up,
        GameControllerSwitchPosition.UpRight => ControllerButtons.Up | ControllerButtons.Right,
        GameControllerSwitchPosition.Right => ControllerButtons.Right,
        GameControllerSwitchPosition.DownRight => ControllerButtons.Down | ControllerButtons.Right,
        GameControllerSwitchPosition.Down => ControllerButtons.Down,
        GameControllerSwitchPosition.DownLeft => ControllerButtons.Down | ControllerButtons.Left,
        GameControllerSwitchPosition.Left => ControllerButtons.Left,
        GameControllerSwitchPosition.UpLeft => ControllerButtons.Up | ControllerButtons.Left,
        _ => ControllerButtons.None
    };

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

    private static bool TryGetXInput(uint index, out XInputGamepad pad)
    {
        pad = default;
        try
        {
            if (XInputGetState(index, out var state) != 0) return false;
            pad = state.Gamepad;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
}
