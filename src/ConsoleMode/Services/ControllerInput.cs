using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Windows.Gaming.Input;

namespace ConsoleMode.Services;

public enum ControllerAction
{
    Confirm,
    Back,
    Up,
    Down,
    Left,
    Right,
    /// <summary>Start / Options / +</summary>
    Menu,
    /// <summary>X on Xbox, Square on PlayStation.</summary>
    Option,
    /// <summary>Y on Xbox, Triangle on PlayStation.</summary>
    Alt
}

public enum ControllerFamily
{
    None,
    Xbox,
    PlayStation,
    Other
}

/// <summary>
/// Polls game controllers and raises <see cref="Pressed"/> for face buttons and directions.
/// Xbox-style pads are read through XInput (works even without window focus); PlayStation
/// (DualShock 4 / DualSense) and other HID pads through Windows.Gaming.Input.RawGameController,
/// which Windows only feeds while this app owns the foreground window.
/// Directions auto-repeat while held, like a console menu.
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
    private const ushort XInputX = 0x4000;
    private const ushort XInputY = 0x8000;
    private const short StickThreshold = 16000;
    private const double RawAxisThreshold = 0.5;

    private static readonly ControllerAction[] AllActions = Enum.GetValues<ControllerAction>();
    private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(120);

    private readonly DispatcherQueueTimer _timer;
    private readonly bool[] _prev = new bool[AllActions.Length];
    private readonly DateTime[] _nextRepeat = new DateTime[AllActions.Length];
    private bool _primed;

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

    public bool IsRunning => _timer.IsRunning;

    public void Start()
    {
        _primed = false;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Stop();

    /// <summary>
    /// Only act while this returns true (e.g. our window is in the foreground). Reading resumes
    /// primed, so a button held while inactive doesn't fire on the way back.
    /// </summary>
    public Func<bool>? IsActive { get; set; }

    private bool _loggedError;

    private void Poll()
    {
        if (IsActive is not null && !IsActive())
        {
            _primed = false;
            return;
        }

        var held = new bool[AllActions.Length];
        try
        {
            ReadXInput(held);
            ReadRaw(held);
        }
        catch (Exception ex)
        {
            // A pad unplugged mid-read throws; skip the sample instead of going deaf for good
            // (ported from nextestudios' controller PR, #17).
            if (!_loggedError) AppLog.Write($"Controles: {ex.Message}");
            _loggedError = true;
            _primed = false;
            return;
        }

        // A diagonal on the D-pad or stick moves vertically, the common case in these lists.
        if (held[(int)ControllerAction.Up] || held[(int)ControllerAction.Down])
            held[(int)ControllerAction.Left] = held[(int)ControllerAction.Right] = false;

        // First sample only records state, so a button already held when the prompt opens
        // (e.g. the A that launched console mode) doesn't answer it.
        var now = DateTime.UtcNow;
        for (var i = 0; i < held.Length; i++)
        {
            var action = AllActions[i];
            if (held[i] && !_prev[i])
            {
                if (_primed) Pressed?.Invoke(action);
                _nextRepeat[i] = now + RepeatDelay;
            }
            else if (held[i] && IsDirection(action) && now >= _nextRepeat[i])
            {
                if (_primed) Pressed?.Invoke(action);
                _nextRepeat[i] = now + RepeatInterval;
            }
            _prev[i] = held[i];
        }
        _primed = true;
    }

    private static bool IsDirection(ControllerAction action) =>
        action is ControllerAction.Up or ControllerAction.Down or ControllerAction.Left or ControllerAction.Right;

    private static void ReadXInput(bool[] held)
    {
        for (uint i = 0; i < 4; i++)
        {
            if (!TryGetXInput(i, out var state)) continue;
            var b = state.Gamepad.wButtons;
            held[(int)ControllerAction.Confirm] |= (b & XInputA) != 0;
            held[(int)ControllerAction.Back] |= (b & XInputB) != 0;
            held[(int)ControllerAction.Option] |= (b & XInputX) != 0;
            held[(int)ControllerAction.Alt] |= (b & XInputY) != 0;
            held[(int)ControllerAction.Menu] |= (b & XInputStart) != 0;
            held[(int)ControllerAction.Up] |= (b & XInputDpadUp) != 0 || state.Gamepad.sThumbLY > StickThreshold;
            held[(int)ControllerAction.Down] |= (b & XInputDpadDown) != 0 || state.Gamepad.sThumbLY < -StickThreshold;
            held[(int)ControllerAction.Left] |= (b & XInputDpadLeft) != 0 || state.Gamepad.sThumbLX < -StickThreshold;
            held[(int)ControllerAction.Right] |= (b & XInputDpadRight) != 0 || state.Gamepad.sThumbLX > StickThreshold;
        }
    }

    private static void ReadRaw(bool[] held)
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

            // HID button order: Sony = Square, Cross, Circle, Triangle, L1, R1, L2, R2, Share, Options…;
            // Nintendo = B, A, Y, X… (A confirms, B goes back in both layouts).
            var (confirmIndex, backIndex, optionIndex, altIndex, menuIndex) = raw.HardwareVendorId switch
            {
                SonyVendorId => (1, 2, 0, 3, 9),
                NintendoVendorId => (1, 0, 3, 2, 9),
                _ => (0, 1, 2, 3, 7)
            };
            Set(held, ControllerAction.Confirm, buttons, confirmIndex);
            Set(held, ControllerAction.Back, buttons, backIndex);
            Set(held, ControllerAction.Option, buttons, optionIndex);
            Set(held, ControllerAction.Alt, buttons, altIndex);
            Set(held, ControllerAction.Menu, buttons, menuIndex);

            // D-pad is the first hat switch; the left stick is axes 0 (X) and 1 (Y, 0 = up).
            if (switches.Length > 0)
            {
                var s = switches[0];
                held[(int)ControllerAction.Up] |= s is GameControllerSwitchPosition.Up or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.UpRight;
                held[(int)ControllerAction.Down] |= s is GameControllerSwitchPosition.Down or GameControllerSwitchPosition.DownLeft or GameControllerSwitchPosition.DownRight;
                held[(int)ControllerAction.Left] |= s is GameControllerSwitchPosition.Left or GameControllerSwitchPosition.UpLeft or GameControllerSwitchPosition.DownLeft;
                held[(int)ControllerAction.Right] |= s is GameControllerSwitchPosition.Right or GameControllerSwitchPosition.UpRight or GameControllerSwitchPosition.DownRight;
            }
            if (axes.Length > 1)
            {
                held[(int)ControllerAction.Left] |= axes[0] < 0.5 - RawAxisThreshold / 2;
                held[(int)ControllerAction.Right] |= axes[0] > 0.5 + RawAxisThreshold / 2;
                held[(int)ControllerAction.Up] |= axes[1] < 0.5 - RawAxisThreshold / 2;
                held[(int)ControllerAction.Down] |= axes[1] > 0.5 + RawAxisThreshold / 2;
            }
        }
    }

    private static void Set(bool[] held, ControllerAction action, bool[] buttons, int index)
    {
        if (index < buttons.Length) held[(int)action] |= buttons[index];
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

    private static bool TryGetXInput(uint index, out XInputState state)
    {
        state = default;
        try
        {
            return XInputGetState(index, out state) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
}
