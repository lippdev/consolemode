using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.Services;

/// <summary>
/// Fires when a set of XInput buttons is held together for <see cref="HoldDuration"/>
/// (zero = fire on press). Used for the shortcuts the user picked (see ControllerShortcuts):
/// enter console mode from the tray, open the session menu and restore the desk, all of
/// which work without focus. XInputGetStateEx (ordinal 100) is the only entry point that
/// reports the Guide button; PlayStation pads are read through SonyHidReader.
/// </summary>
public sealed class ControllerHoldWatcher : IDisposable
{
    public const ushort GuideButton = 0x0400;
    public const ushort StartButton = 0x0010;
    public const ushort BackButton = 0x0020;
    public const ushort YButton = 0x8000;
    public static readonly TimeSpan LongHold = TimeSpan.FromMilliseconds(900);

    private static bool _unavailable;

    private readonly DispatcherQueueTimer _timer;
    private readonly string _name;
    private DateTime? _heldSince;
    private bool _fired;

    /// <summary>Raised once per hold, on the dispatcher thread.</summary>
    public event Action? Held;

    public TimeSpan HoldDuration { get; set; } = LongHold;

    /// <summary>The combo to watch; changing it re-arms the watch (it must be released first).</summary>
    public ushort Mask
    {
        get => _mask;
        set
        {
            if (_mask == value) return;
            _mask = value;
            _heldSince = null;
            _fired = true;
        }
    }

    private ushort _mask;

    public ControllerHoldWatcher(DispatcherQueue dispatcher, ushort mask, string name)
    {
        _mask = mask;
        _name = name;
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += (_, _) => Poll();
    }

    public bool IsRunning => _timer.IsRunning;

    public void Start()
    {
        if (_timer.IsRunning) return;
        SonyHidReader.Acquire();
        _heldSince = null;
        // A combo already held when the watch starts must be released first.
        _fired = true;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer.IsRunning) SonyHidReader.Release();
        _timer.Stop();
        _heldSince = null;
        _fired = false;
    }

    public void Dispose() => Stop();

    private void Poll()
    {
        // An empty combo is "held" by any pad state; a shortcut that isn't set never fires.
        if (_mask == 0 || !IsHeld(_mask))
        {
            _heldSince = null;
            _fired = false;
            return;
        }

        _heldSince ??= DateTime.UtcNow;
        if (_fired || DateTime.UtcNow - _heldSince < HoldDuration) return;
        _fired = true;
        AppLog.Write($"Controle: {_name}");
        Held?.Invoke();
    }

    /// <summary>Buttons held per XInput slot or Sony HID path, for shortcut capture.</summary>
    public static IReadOnlyDictionary<string, ushort> ReadHeldByDevice()
    {
        var held = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, buttons) in SonyHidReader.ReadStates()) held[$"sony:{path}"] = buttons;
        if (_unavailable) return held;
        try
        {
            for (uint i = 0; i < 4; i++)
            {
                if (XInputGetStateEx(i, out var state) == 0) held[$"xinput:{i}"] = state.Gamepad.wButtons;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            AppLog.Write($"Controle: atalhos indisponíveis: {ex.Message}");
            _unavailable = true;
        }
        return held;
    }

    private static bool IsHeld(ushort mask)
    {
        // Test each physical Sony pad separately; combining their masks creates impossible chords.
        if (ControllerShortcuts.IsHeldOnAnyDevice(mask, SonyHidReader.ReadStates().Values)) return true;
        if (_unavailable) return false;
        try
        {
            for (uint i = 0; i < 4; i++)
            {
                if (XInputGetStateEx(i, out var state) == 0 && (state.Gamepad.wButtons & mask) == mask)
                    return true;
            }
            return false;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // No XInput 1.4 (or no ordinal 100): controller shortcuts simply aren't available.
            AppLog.Write($"Controle: atalhos indisponíveis: {ex.Message}");
            _unavailable = true;
            return false;
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

    [DllImport("xinput1_4.dll", EntryPoint = "#100")]
    private static extern uint XInputGetStateEx(uint userIndex, out XInputState state);
}
