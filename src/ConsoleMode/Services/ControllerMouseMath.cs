namespace ConsoleMode.Services;

/// <summary>Pure rules behind ControllerMouse and CursorHider, kept apart so the tests cover them.</summary>
public static class ControllerMouseMath
{
    /// <summary>Radial dead zone of the left stick (pointer); worn sticks rest a little off center.</summary>
    public const double MoveDeadZone = 0.18;
    /// <summary>The right stick (wheel) needs a clearer push, so aiming with the left one never scrolls by accident.</summary>
    public const double ScrollDeadZone = 0.3;
    /// <summary>Full tilt crosses about this many screen heights per second, so a 4K TV feels like a 1080p one.</summary>
    public const double ScreensPerSecond = 1.1;
    public const double MinNotchesPerSecond = 3;
    public const double MaxNotchesPerSecond = 16;
    /// <summary>The pointer is hidden after sitting still this long.</summary>
    public static readonly TimeSpan CursorIdleDelay = TimeSpan.FromSeconds(3);

    /// <summary>XInput stick value (-32768..32767, Y up) to -1..1.</summary>
    public static double FromXInput(short value) => Math.Max(-1, value / 32767.0);

    /// <summary>Sony HID stick byte (0..255, 128 = center, Y down) to -1..1 with Y up like XInput.</summary>
    public static double FromHidByte(byte value, bool invert) => Math.Clamp((value - 128) / 127.0, -1, 1) * (invert ? -1 : 1);

    /// <summary>
    /// Radial dead zone, then a squared curve: small tilts aim precisely, full tilt travels.
    /// Input and output in -1..1; the direction is kept.
    /// </summary>
    public static (double X, double Y) Curve(double x, double y, double deadZone)
    {
        var magnitude = Math.Sqrt(x * x + y * y);
        if (magnitude <= deadZone) return (0, 0);
        var scaled = Math.Min(1, (magnitude - deadZone) / (1 - deadZone));
        var factor = scaled * scaled / magnitude;
        return (x * factor, y * factor);
    }

    /// <summary>A pointer that has not moved for <see cref="CursorIdleDelay"/> is hidden; CursorHider shows it again on the next move.</summary>
    public static bool ShouldHideCursor(TimeSpan stillFor) => stillFor >= CursorIdleDelay;
}

/// <summary>Left stick to pointer pixels, carrying the fractions between samples so slow moves don't stall.</summary>
public sealed class PointerMotion
{
    private double _restX;
    private double _restY;

    /// <param name="x">Stick X, -1..1.</param>
    /// <param name="y">Stick Y, -1..1, up positive (the screen's Y grows downward).</param>
    /// <param name="seconds">Time since the previous sample.</param>
    /// <param name="screenHeight">Height of the game screen in pixels.</param>
    public (int Dx, int Dy) Step(double x, double y, double seconds, int screenHeight)
    {
        var (vx, vy) = ControllerMouseMath.Curve(x, y, ControllerMouseMath.MoveDeadZone);
        if (vx == 0 && vy == 0)
        {
            Reset();
            return (0, 0);
        }
        var speed = Math.Max(screenHeight, 720) * ControllerMouseMath.ScreensPerSecond;
        _restX += vx * speed * seconds;
        _restY -= vy * speed * seconds;
        var dx = (int)Math.Truncate(_restX);
        var dy = (int)Math.Truncate(_restY);
        _restX -= dx;
        _restY -= dy;
        return (dx, dy);
    }

    public void Reset() => _restX = _restY = 0;
}

/// <summary>
/// One right-stick axis to whole wheel notches (positive = up / right). Whole notches only, since some
/// programs ignore partial ones; the first notch goes out as soon as the stick leaves the dead zone.
/// </summary>
public sealed class WheelMotion
{
    private double _pending;
    private bool _moving;

    public int Step(double axis, double seconds)
    {
        var magnitude = Math.Abs(axis);
        if (magnitude <= ControllerMouseMath.ScrollDeadZone)
        {
            Reset();
            return 0;
        }
        var scaled = Math.Min(1, (magnitude - ControllerMouseMath.ScrollDeadZone) / (1 - ControllerMouseMath.ScrollDeadZone));
        var rate = ControllerMouseMath.MinNotchesPerSecond
                   + (ControllerMouseMath.MaxNotchesPerSecond - ControllerMouseMath.MinNotchesPerSecond) * scaled * scaled;
        if (!_moving)
        {
            _moving = true;
            _pending = 1;
        }
        else
        {
            _pending += rate * seconds;
        }
        var notches = (int)Math.Truncate(_pending);
        _pending -= notches;
        return notches * Math.Sign(axis);
    }

    public void Reset()
    {
        _pending = 0;
        _moving = false;
    }
}

public enum ClickEvent
{
    None,
    /// <summary>A tapped once: left down and up.</summary>
    LeftClick,
    /// <summary>A tapped twice: right down and up.</summary>
    RightClick,
    /// <summary>A held: left button down, to drag.</summary>
    LeftDown,
    /// <summary>A released after a hold.</summary>
    LeftUp
}

/// <summary>
/// The A button as a mouse: a tap is a left click, two quick taps a right click, a hold a left drag.
/// The single click waits <see cref="DoubleTapWindow"/> after the release, to know it isn't the first
/// half of a right click.
/// </summary>
public sealed class ClickGesture
{
    public static readonly TimeSpan HoldForDrag = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan DoubleTapWindow = TimeSpan.FromMilliseconds(250);

    private enum Phase { WaitRelease, Idle, FirstPress, WaitSecondTap, Dragging }

    private Phase _phase = Phase.WaitRelease;
    private TimeSpan _since;

    /// <summary>One sample of the A button at <paramref name="now"/> (any monotonic clock).</summary>
    public ClickEvent Update(bool held, TimeSpan now)
    {
        switch (_phase)
        {
            case Phase.WaitRelease:
                if (!held) _phase = Phase.Idle;
                return ClickEvent.None;

            case Phase.Idle:
                if (held) Enter(Phase.FirstPress, now);
                return ClickEvent.None;

            case Phase.FirstPress:
                if (!held)
                {
                    Enter(Phase.WaitSecondTap, now);
                    return ClickEvent.None;
                }
                if (now - _since < HoldForDrag) return ClickEvent.None;
                _phase = Phase.Dragging;
                return ClickEvent.LeftDown;

            case Phase.WaitSecondTap:
                if (held)
                {
                    _phase = Phase.WaitRelease;
                    return ClickEvent.RightClick;
                }
                if (now - _since < DoubleTapWindow) return ClickEvent.None;
                _phase = Phase.Idle;
                return ClickEvent.LeftClick;

            case Phase.Dragging:
                if (held) return ClickEvent.None;
                _phase = Phase.Idle;
                return ClickEvent.LeftUp;
        }
        return ClickEvent.None;
    }

    /// <summary>
    /// Starts over, ignoring A until it is released (the press that closed the session menu must not click).
    /// True when the left button is still down from a drag and must be released by the caller.
    /// </summary>
    public bool Reset()
    {
        var wasDragging = _phase == Phase.Dragging;
        _phase = Phase.WaitRelease;
        return wasDragging;
    }

    private void Enter(Phase phase, TimeSpan now)
    {
        _phase = phase;
        _since = now;
    }
}
