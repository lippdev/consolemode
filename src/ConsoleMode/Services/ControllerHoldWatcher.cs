using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.Services;

/// <summary>
/// Fires when a set of XInput buttons is held together for <see cref="HoldDuration"/>
/// (zero = fire on press). Used for the Xbox Guide button while the app idles in the tray
/// (enter console mode) and for Start + Back during a session (restore the desk), both of
/// which work without focus. XInputGetStateEx (ordinal 100) is the only entry point that
/// reports the Guide button; PlayStation pads are only readable in the foreground.
/// </summary>
public sealed class ControllerHoldWatcher : IDisposable
{
    public const ushort GuideButton = 0x0400;
    public const ushort StartButton = 0x0010;
    public const ushort BackButton = 0x0020;
    public static readonly TimeSpan LongHold = TimeSpan.FromMilliseconds(900);

    private static bool _unavailable;

    private readonly DispatcherQueueTimer _timer;
    private readonly ushort _mask;
    private readonly string _name;
    private DateTime? _heldSince;
    private bool _fired;

    /// <summary>Raised once per hold, on the dispatcher thread.</summary>
    public event Action? Held;

    public TimeSpan HoldDuration { get; set; } = LongHold;

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
        if (_unavailable || _timer.IsRunning) return;
        _heldSince = null;
        // A combo already held when the watch starts must be released first.
        _fired = true;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _heldSince = null;
        _fired = false;
    }

    public void Dispose() => _timer.Stop();

    private void Poll()
    {
        if (!IsHeld(_mask))
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

    private static bool IsHeld(ushort mask)
    {
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
