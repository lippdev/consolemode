using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleMode.Services;

/// <summary>
/// "Control the mouse" in the session menu, for games and emulators whose menus don't take a controller:
/// the left stick moves the pointer, the right stick scrolls, A clicks (twice = right click, held = drag;
/// see ClickGesture). Reads XInput and Sony HID, which both work while the game has the focus
/// (Windows.Gaming.Input only reaches the foreground window). Runs on its own thread so the pointer stays
/// smooth when the UI is busy; <see cref="Paused"/> while the session menu is open, where the pad drives the menu.
/// </summary>
public sealed class ControllerMouse : IDisposable
{
    private const ushort XInputA = 0x1000;
    private const uint MouseMove = 0x0001;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint MouseRightDown = 0x0008;
    private const uint MouseRightUp = 0x0010;
    private const uint MouseWheel = 0x0800;
    private const uint MouseHWheel = 0x1000;
    private const int WheelDelta = 120;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(8);

    private CancellationTokenSource? _cts;
    private Thread? _thread;
    private static bool _xinputMissing;

    public bool IsRunning => _cts is not null;

    /// <summary>The pad stops driving the mouse (an ongoing drag is released) until this is false again.</summary>
    public bool Paused
    {
        get => _paused;
        set => _paused = value;
    }

    private volatile bool _paused;

    /// <param name="screenHeight">Height of the game screen, so the pointer speed is the same on any resolution.</param>
    public void Start(int screenHeight)
    {
        if (_cts is not null) return;
        SonyHidReader.Acquire();
        var cts = new CancellationTokenSource();
        _cts = cts;
        _thread = new Thread(() => Run(screenHeight, cts.Token)) { IsBackground = true, Name = "ControllerMouse" };
        _thread.Start();
        AppLog.Write("Mouse pelo controle: ligado");
    }

    public void Stop()
    {
        if (_cts is not { } cts) return;
        _cts = null;
        cts.Cancel();
        _thread?.Join(500);
        _thread = null;
        SonyHidReader.Release();
        AppLog.Write("Mouse pelo controle: desligado");
    }

    public void Dispose() => Stop();

    private void Run(int screenHeight, CancellationToken ct)
    {
        var pointer = new PointerMotion();
        var wheelY = new WheelMotion();
        var wheelX = new WheelMotion();
        var gesture = new ClickGesture();
        var clock = Stopwatch.StartNew();
        var last = clock.Elapsed;
        var wasPaused = true;
        var leftDown = false;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Thread.Sleep(SampleInterval);
                var now = clock.Elapsed;
                // A stall (sleep, debugger) must not throw the pointer across the screen.
                var seconds = Math.Min((now - last).TotalSeconds, 0.05);
                last = now;

                if (_paused)
                {
                    if (!wasPaused)
                    {
                        if (gesture.Reset()) Send(MouseLeftUp);
                        leftDown = false;
                        pointer.Reset();
                        wheelX.Reset();
                        wheelY.Reset();
                    }
                    wasPaused = true;
                    continue;
                }
                if (wasPaused)
                {
                    // Coming back (or starting): the A that closed the menu is probably still down.
                    gesture.Reset();
                    wasPaused = false;
                }

                var pad = Read();
                var (dx, dy) = pointer.Step(pad.LeftX, pad.LeftY, seconds, screenHeight);
                if (dx != 0 || dy != 0) Send(MouseMove, dx, dy);
                var notchesY = wheelY.Step(pad.RightY, seconds);
                if (notchesY != 0) Send(MouseWheel, data: notchesY * WheelDelta);
                var notchesX = wheelX.Step(pad.RightX, seconds);
                if (notchesX != 0) Send(MouseHWheel, data: notchesX * WheelDelta);

                switch (gesture.Update(pad.A, now))
                {
                    case ClickEvent.LeftClick:
                        Send(MouseLeftDown);
                        Send(MouseLeftUp);
                        break;
                    case ClickEvent.RightClick:
                        Send(MouseRightDown);
                        Send(MouseRightUp);
                        break;
                    case ClickEvent.LeftDown:
                        Send(MouseLeftDown);
                        leftDown = true;
                        break;
                    case ClickEvent.LeftUp:
                        Send(MouseLeftUp);
                        leftDown = false;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Mouse pelo controle: {ex.Message}");
        }
        finally
        {
            // Never leave the left button stuck down when the mode ends mid-drag.
            if (leftDown) Send(MouseLeftUp);
        }
    }

    private readonly record struct PadSample(double LeftX, double LeftY, double RightX, double RightY, bool A);

    /// <summary>Every pad at once: the stick pushed furthest wins, A counts on any pad.</summary>
    private static PadSample Read()
    {
        double lx = 0, ly = 0, rx = 0, ry = 0;
        var a = false;

        void Take(double x, double y, ref double bestX, ref double bestY)
        {
            if (x * x + y * y > bestX * bestX + bestY * bestY) { bestX = x; bestY = y; }
        }

        if (!_xinputMissing)
        {
            try
            {
                for (uint i = 0; i < 4; i++)
                {
                    if (XInputGetState(i, out var state) != 0) continue;
                    var g = state.Gamepad;
                    Take(ControllerMouseMath.FromXInput(g.sThumbLX), ControllerMouseMath.FromXInput(g.sThumbLY), ref lx, ref ly);
                    Take(ControllerMouseMath.FromXInput(g.sThumbRX), ControllerMouseMath.FromXInput(g.sThumbRY), ref rx, ref ry);
                    a |= (g.wButtons & XInputA) != 0;
                }
            }
            catch (DllNotFoundException)
            {
                _xinputMissing = true;
            }
        }

        foreach (var s in SonyHidReader.ReadSticks())
        {
            Take(ControllerMouseMath.FromHidByte(s.LeftX, false), ControllerMouseMath.FromHidByte(s.LeftY, true), ref lx, ref ly);
            Take(ControllerMouseMath.FromHidByte(s.RightX, false), ControllerMouseMath.FromHidByte(s.RightY, true), ref rx, ref ry);
        }
        a |= (SonyHidReader.Held & XInputA) != 0;
        return new PadSample(lx, ly, rx, ry, a);
    }

    private static void Send(uint flags, int dx = 0, int dy = 0, int data = 0)
    {
        var input = new Input
        {
            Type = 0,   // INPUT_MOUSE
            Mouse = new MouseInput { Dx = dx, Dy = dy, MouseData = unchecked((uint)data), Flags = flags }
        };
        // Fails silently against a window running as administrator (UIPI): nothing to do from here.
        _ = SendInput(1, [input], Marshal.SizeOf<Input>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput Mouse;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
}
