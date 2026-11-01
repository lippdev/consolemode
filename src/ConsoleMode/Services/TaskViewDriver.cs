using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

/// <summary>
/// The Task View shortcut: opens Windows' Task View (Win + Tab) and, while it is open, lets the controller drive it
/// with the keys it understands (<see cref="TaskViewKeys"/>: arrows, Enter, Escape). Reads XInput and Sony HID on its
/// own thread, like the controller mouse, since neither needs the focus. It stops when A or B closes Task View, when
/// something else does (a click, the keyboard), or after a while; and at once if Task View never took the
/// foreground, so the controller never types into the game behind it.
/// </summary>
public sealed class TaskViewDriver : IDisposable
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(16);
    /// <summary>Task View takes the foreground within this; after it, a different foreground window means it closed.</summary>
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan LongestOpen = TimeSpan.FromMinutes(2);

    private CancellationTokenSource? _cts;
    private Thread? _thread;
    private static bool _xinputMissing;

    /// <summary>Raised on the driver's thread once it stops driving Task View.</summary>
    public event Action? Stopped;

    public bool IsRunning => _cts is not null;

    /// <summary>Opens Task View and starts driving it; with Task View already open from here, closes it instead.</summary>
    public void Toggle()
    {
        if (IsRunning)
        {
            SendKey(VkEscape);
            Stop();
            return;
        }
        var before = GetForegroundWindow();
        SendWinTab();
        AppLog.Write("Visão de tarefas: aberta pelo controle");
        SonyHidReader.Acquire();
        var cts = new CancellationTokenSource();
        _cts = cts;
        _thread = new Thread(() => Run(before, cts)) { IsBackground = true, Name = "TaskViewDriver" };
        _thread.Start();
    }

    public void Stop()
    {
        if (_cts is not { } cts) return;
        _cts = null;
        cts.Cancel();
        if (Thread.CurrentThread != _thread) _thread?.Join(500);
        _thread = null;
    }

    public void Dispose() => Stop();

    private void Run(nint before, CancellationTokenSource run)
    {
        var ct = run.Token;
        var keys = new TaskViewKeys();
        var clock = Stopwatch.StartNew();
        nint view = 0;
        var why = "tempo esgotado";
        try
        {
            while (!ct.IsCancellationRequested && clock.Elapsed < LongestOpen)
            {
                Thread.Sleep(SampleInterval);
                var foreground = GetForegroundWindow();
                if (view == 0)
                {
                    if (clock.Elapsed < SettleTime) continue;
                    if (foreground == before || foreground == 0)
                    {
                        why = "não ficou em primeiro plano";
                        break;
                    }
                    view = foreground;
                }
                else if (foreground != view)
                {
                    why = "fechada";
                    break;
                }

                var ended = false;
                foreach (var key in keys.Step(Read(), clock.Elapsed))
                {
                    SendKey(key switch
                    {
                        TaskViewKey.Left => VkLeft,
                        TaskViewKey.Right => VkRight,
                        TaskViewKey.Up => VkUp,
                        TaskViewKey.Down => VkDown,
                        TaskViewKey.Enter => VkReturn,
                        _ => VkEscape
                    });
                    ended |= key is TaskViewKey.Enter or TaskViewKey.Escape;
                }
                if (ended)
                {
                    why = "escolha feita";
                    break;
                }
            }
            if (ct.IsCancellationRequested) why = "fechada pelo atalho";
        }
        catch (Exception ex)
        {
            why = ex.Message;
        }
        finally
        {
            SonyHidReader.Release();
            AppLog.Write($"Visão de tarefas: controle solto ({why})");
            // Only this run: a new one may already have started.
            Interlocked.CompareExchange(ref _cts, null, run);
            Stopped?.Invoke();
        }
    }

    /// <summary>Every pad at once: buttons from all of them, the stick pushed furthest.</summary>
    private static TaskViewPad Read()
    {
        ushort buttons = 0;
        double x = 0, y = 0;

        void Take(double sx, double sy)
        {
            if (sx * sx + sy * sy > x * x + y * y) { x = sx; y = sy; }
        }

        if (!_xinputMissing)
        {
            try
            {
                for (uint i = 0; i < 4; i++)
                {
                    if (XInputGetState(i, out var state) != 0) continue;
                    buttons |= state.Gamepad.wButtons;
                    Take(ControllerMouseMath.FromXInput(state.Gamepad.sThumbLX), ControllerMouseMath.FromXInput(state.Gamepad.sThumbLY));
                }
            }
            catch (DllNotFoundException)
            {
                _xinputMissing = true;
            }
        }
        buttons |= SonyHidReader.Held;
        foreach (var s in SonyHidReader.ReadSticks())
            Take(ControllerMouseMath.FromHidByte(s.LeftX, false), ControllerMouseMath.FromHidByte(s.LeftY, true));
        return new TaskViewPad(buttons, x, y);
    }

    private const byte VkLeftWin = 0x5B;
    private const byte VkTab = 0x09;
    private const byte VkReturn = 0x0D;
    private const byte VkEscape = 0x1B;
    private const byte VkLeft = 0x25;
    private const byte VkUp = 0x26;
    private const byte VkRight = 0x27;
    private const byte VkDown = 0x28;
    private const uint KeyEventExtended = 0x0001;
    private const uint KeyEventUp = 0x0002;

    private static void SendWinTab()
    {
        keybd_event(VkLeftWin, 0, KeyEventExtended, 0);
        keybd_event(VkTab, 0, 0, 0);
        keybd_event(VkTab, 0, KeyEventUp, 0);
        keybd_event(VkLeftWin, 0, KeyEventExtended | KeyEventUp, 0);
    }

    /// <summary>One key press; the arrows are extended keys (the ones outside the numeric keypad).</summary>
    private static void SendKey(byte key)
    {
        var flags = key is VkLeft or VkUp or VkRight or VkDown ? KeyEventExtended : 0;
        keybd_event(key, 0, flags, 0);
        keybd_event(key, 0, flags | KeyEventUp, 0);
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

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte key, byte scan, uint flags, nuint extraInfo);
}
