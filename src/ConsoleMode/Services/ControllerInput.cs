using System.Runtime.InteropServices;
using System.Text;
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
/// Three sources, in this order: XInput (Xbox-style pads, works even without focus),
/// Windows.Gaming.Input.Gamepad (pads Windows standardises but XInput didn't answer for),
/// and RawGameController (PlayStation / other HID pads, foreground only).
/// Directions auto-repeat while held, like a console menu. A device that throws is skipped
/// on its own, so one bad pad never silences the others.
/// </summary>
public sealed class ControllerInput : IDisposable
{
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

    private static readonly ControllerAction[] AllActions = Enum.GetValues<ControllerAction>();
    private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(120);
    private static readonly HashSet<string> LoggedDeviceErrors = [];

    private readonly DispatcherQueueTimer _timer;
    private readonly bool[] _prev = new bool[AllActions.Length];
    private readonly DateTime[] _nextRepeat = new DateTime[AllActions.Length];
    private bool _primed;
    private bool _loggedError;

    public event Action<ControllerAction>? Pressed;

    /// <summary>
    /// Only act while this returns true (e.g. our window is in the foreground). Reading resumes
    /// primed, so a button held while inactive doesn't fire on the way back.
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
            _ = Gamepad.Gamepads.Count;
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
                if (raw.HardwareVendorId == ControllerMapping.SonyVendorId) return ControllerFamily.PlayStation;
            }
        }
        catch { /* WGI missing: fall through to XInput */ }

        for (uint i = 0; i < 4; i++)
        {
            if (TryGetXInput(i, out _)) return ControllerFamily.Xbox;
        }

        try
        {
            if (RawGameController.RawGameControllers.Count > 0 || Gamepad.Gamepads.Count > 0) return ControllerFamily.Other;
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
            ReadAll(held, null);
        }
        catch (Exception ex)
        {
            // Enumeration itself failed; skip the sample instead of going deaf for good (from PR #17).
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

    /// <summary>One sample from every source. <paramref name="diag"/> collects what the test screen shows.</summary>
    private static void ReadAll(bool[] held, StringBuilder? diag)
    {
        var xinputPads = ReadXInput(held, diag);
        if (ControllerMapping.ShouldReadGamepads(xinputPads)) ReadGamepads(held, diag);
        ReadRaw(held, xinputPads, diag);
    }

    private static int ReadXInput(bool[] held, StringBuilder? diag)
    {
        var count = 0;
        for (uint i = 0; i < 4; i++)
        {
            if (!TryGetXInput(i, out var state)) continue;
            count++;
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
            diag?.AppendLine($"XInput #{i}: buttons=0x{b:X4} LX={state.Gamepad.sThumbLX} LY={state.Gamepad.sThumbLY}");
        }
        return count;
    }

    private static void ReadGamepads(bool[] held, StringBuilder? diag)
    {
        foreach (var pad in Gamepad.Gamepads)
        {
            try
            {
                var r = pad.GetCurrentReading();
                var b = r.Buttons;
                held[(int)ControllerAction.Confirm] |= b.HasFlag(GamepadButtons.A);
                held[(int)ControllerAction.Back] |= b.HasFlag(GamepadButtons.B);
                held[(int)ControllerAction.Option] |= b.HasFlag(GamepadButtons.X);
                held[(int)ControllerAction.Alt] |= b.HasFlag(GamepadButtons.Y);
                held[(int)ControllerAction.Menu] |= b.HasFlag(GamepadButtons.Menu);
                held[(int)ControllerAction.Up] |= b.HasFlag(GamepadButtons.DPadUp) || r.LeftThumbstickY > 0.5;
                held[(int)ControllerAction.Down] |= b.HasFlag(GamepadButtons.DPadDown) || r.LeftThumbstickY < -0.5;
                held[(int)ControllerAction.Left] |= b.HasFlag(GamepadButtons.DPadLeft) || r.LeftThumbstickX < -0.5;
                held[(int)ControllerAction.Right] |= b.HasFlag(GamepadButtons.DPadRight) || r.LeftThumbstickX > 0.5;
                diag?.AppendLine($"Gamepad: buttons={b} LX={r.LeftThumbstickX:F2} LY={r.LeftThumbstickY:F2}");
            }
            catch (Exception ex)
            {
                LogDeviceOnce("Gamepad", ex);
                diag?.AppendLine($"Gamepad: erro {ex.Message}");
            }
        }
    }

    private static void ReadRaw(bool[] held, int xinputPads, StringBuilder? diag)
    {
        foreach (var raw in RawGameController.RawGameControllers)
        {
            var name = SafeName(raw);
            try
            {
                var isGamepad = Gamepad.FromGameController(raw) is not null;
                // Xbox pads also show up here; XInput already covers them, unless XInput saw nothing.
                if (!ControllerMapping.ShouldReadHid(isGamepad, xinputPads)) { diag?.AppendLine($"HID {name}: coberto pelo XInput"); continue; }
                if (raw.ButtonCount < 2) continue;

                var buttons = new bool[raw.ButtonCount];
                var switches = new GameControllerSwitchPosition[raw.SwitchCount];
                var axes = new double[raw.AxisCount];
                raw.GetCurrentReading(buttons, switches, axes);

                var (confirmIndex, backIndex, optionIndex, altIndex, menuIndex) = ControllerMapping.HidIndices(raw.HardwareVendorId);
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
                    var (l, r, u, d) = ControllerMapping.StickDirections(axes[0], axes[1]);
                    held[(int)ControllerAction.Left] |= l;
                    held[(int)ControllerAction.Right] |= r;
                    held[(int)ControllerAction.Up] |= u;
                    held[(int)ControllerAction.Down] |= d;
                }

                if (diag is not null)
                {
                    var pressed = string.Join(",", buttons.Select((on, i) => on ? i.ToString() : null).Where(s => s is not null));
                    var axisText = string.Join(" ", axes.Take(4).Select((a, i) => $"a{i}={a:F2}"));
                    diag.AppendLine($"HID {name}: botões=[{pressed}] hat={(switches.Length > 0 ? switches[0].ToString() : "-")} {axisText}");
                }
            }
            catch (Exception ex)
            {
                // One pad that throws (unplugged mid-read, driver quirk) must not silence the rest.
                LogDeviceOnce(name, ex);
                diag?.AppendLine($"HID {name}: erro {ex.Message}");
            }
        }
    }

    private static void Set(bool[] held, ControllerAction action, bool[] buttons, int index)
    {
        if (index >= 0 && index < buttons.Length) held[(int)action] |= buttons[index];
    }

    private static string SafeName(RawGameController raw)
    {
        try { return $"{raw.DisplayName} ({raw.HardwareVendorId:X4}:{raw.HardwareProductId:X4})"; }
        catch { return "?"; }
    }

    private static void LogDeviceOnce(string name, Exception ex)
    {
        lock (LoggedDeviceErrors)
        {
            if (!LoggedDeviceErrors.Add(name)) return;
        }
        AppLog.Write($"Controles: {name}: {ex.Message}");
    }

    // ── Diagnostics (Settings → Test controller, and the startup log) ──

    /// <summary>Every device Windows lists, one line each, for the log and the test screen.</summary>
    public static string DescribeDevices()
    {
        var sb = new StringBuilder();
        var xinput = 0;
        for (uint i = 0; i < 4; i++) if (TryGetXInput(i, out _)) { xinput++; sb.AppendLine($"XInput #{i}: conectado"); }
        try
        {
            foreach (var pad in Gamepad.Gamepads) sb.AppendLine("Gamepad (Windows.Gaming.Input): " + (ControllerMapping.ShouldReadGamepads(xinput) ? "lido" : "coberto pelo XInput"));
            foreach (var raw in RawGameController.RawGameControllers)
            {
                var isGamepad = Gamepad.FromGameController(raw) is not null;
                sb.AppendLine($"HID {SafeName(raw)}: botões={raw.ButtonCount} switches={raw.SwitchCount} eixos={raw.AxisCount} gamepad={(isGamepad ? "sim" : "não")} " +
                              $"leitura={(ControllerMapping.ShouldReadHid(isGamepad, xinput) ? "sim" : "não (XInput cobre)")}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Windows.Gaming.Input: {ex.Message}");
        }
        return sb.Length == 0 ? "nenhum controle detectado" : sb.ToString().TrimEnd();
    }

    /// <summary>One live sample with raw values, plus the actions the app would derive from it.</summary>
    public static string SampleDiagnostics()
    {
        var held = new bool[AllActions.Length];
        var diag = new StringBuilder();
        try { ReadAll(held, diag); }
        catch (Exception ex) { diag.AppendLine($"erro: {ex.Message}"); }
        var actions = string.Join(" ", AllActions.Where(a => held[(int)a]));
        diag.AppendLine($"→ {(actions.Length == 0 ? "(nada)" : actions)}");
        return diag.ToString().TrimEnd();
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
