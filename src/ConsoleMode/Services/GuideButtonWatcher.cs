using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.Services;

/// <summary>
/// Watches the Xbox Guide (Home) button while the app sits in the tray, so holding it
/// enters console mode from the couch. A short press is left alone: Game Bar and Steam
/// already react to it, and we only want the deliberate long hold.
/// Only XInput pads expose the Guide button without focus (through the undocumented
/// XInputGetStateEx, ordinal 100); PlayStation pads are only readable in the foreground.
/// </summary>
public sealed class GuideButtonWatcher : IDisposable
{
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMilliseconds(900);

    private const ushort XInputGuide = 0x0400;
    private static bool _unavailable;

    private readonly DispatcherQueueTimer _timer;
    private DateTime? _heldSince;
    private bool _fired;

    /// <summary>Raised once per hold, on the dispatcher thread.</summary>
    public event Action? Held;

    public GuideButtonWatcher(DispatcherQueue dispatcher)
    {
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += (_, _) => Poll();
    }

    public bool IsRunning => _timer.IsRunning;

    public void Start()
    {
        if (_unavailable || _timer.IsRunning) return;
        _heldSince = null;
        _fired = false;
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
        if (!IsGuideHeld())
        {
            _heldSince = null;
            _fired = false;
            return;
        }

        _heldSince ??= DateTime.UtcNow;
        if (_fired || DateTime.UtcNow - _heldSince < HoldDuration) return;
        _fired = true;
        AppLog.Write("Controle: botão Home segurado");
        Held?.Invoke();
    }

    private static bool IsGuideHeld()
    {
        if (_unavailable) return false;
        try
        {
            for (uint i = 0; i < 4; i++)
            {
                if (XInputGetStateEx(i, out var state) == 0 && (state.Gamepad.wButtons & XInputGuide) != 0)
                    return true;
            }
            return false;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // No XInput 1.4 (or no ordinal 100): the Home button simply isn't available.
            AppLog.Write($"Controle: botão Home indisponível: {ex.Message}");
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

    // Ordinal 100 is the only XInput entry point that reports the Guide button.
    [DllImport("xinput1_4.dll", EntryPoint = "#100")]
    private static extern uint XInputGetStateEx(uint userIndex, out XInputState state);
}
