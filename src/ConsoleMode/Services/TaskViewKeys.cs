namespace ConsoleMode.Services;

/// <summary>A key the controller stands for while Windows' Task View is open.</summary>
public enum TaskViewKey
{
    Left,
    Right,
    Up,
    Down,
    /// <summary>A: opens the window in focus (and Task View closes).</summary>
    Enter,
    /// <summary>B: closes Task View, back to where you were.</summary>
    Escape
}

/// <summary>What the controller does at one moment, from any pad (XInput bits; a Sony pad's left stick sets the D-pad bits too).</summary>
public readonly record struct TaskViewPad(ushort Buttons, double StickX, double StickY);

/// <summary>
/// Turns the controller into the keys Task View understands: the D-pad and the left stick move (a held direction
/// repeats, like a held arrow key), A is Enter and B is Escape. Nothing counts until every button has been let go
/// once: the shortcut that opened Task View is usually still held. Pure, so it is tested.
/// </summary>
public sealed class TaskViewKeys
{
    public const ushort DpadUp = 0x0001;
    public const ushort DpadDown = 0x0002;
    public const ushort DpadLeft = 0x0004;
    public const ushort DpadRight = 0x0008;
    public const ushort A = 0x1000;
    public const ushort B = 0x2000;

    /// <summary>How far the stick goes (0 to 1) before it counts as a direction.</summary>
    public const double StickThreshold = 0.55;
    public static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(420);
    public static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(140);

    private bool _armed;
    private TaskViewKey? _direction;
    private TimeSpan _nextRepeat;
    private bool _aDown;
    private bool _bDown;

    /// <summary>The keys to send for this sample, taken at <paramref name="now"/>.</summary>
    public IReadOnlyList<TaskViewKey> Step(TaskViewPad pad, TimeSpan now)
    {
        var keys = new List<TaskViewKey>();
        var direction = Direction(pad);
        var a = (pad.Buttons & A) != 0;
        var b = (pad.Buttons & B) != 0;

        if (!_armed)
        {
            if (pad.Buttons != 0 || direction is not null) return keys;
            _armed = true;
            return keys;
        }

        if (direction != _direction)
        {
            _direction = direction;
            if (direction is { } pressed)
            {
                keys.Add(pressed);
                _nextRepeat = now + RepeatDelay;
            }
        }
        else if (direction is { } held && now >= _nextRepeat)
        {
            keys.Add(held);
            _nextRepeat = now + RepeatInterval;
        }

        // A and B act when pressed, once per press.
        if (a && !_aDown) keys.Add(TaskViewKey.Enter);
        if (b && !_bDown) keys.Add(TaskViewKey.Escape);
        _aDown = a;
        _bDown = b;
        return keys;
    }

    /// <summary>The D-pad first; otherwise the stick, along its stronger axis once it is pushed far enough.</summary>
    public static TaskViewKey? Direction(TaskViewPad pad)
    {
        if ((pad.Buttons & DpadLeft) != 0) return TaskViewKey.Left;
        if ((pad.Buttons & DpadRight) != 0) return TaskViewKey.Right;
        if ((pad.Buttons & DpadUp) != 0) return TaskViewKey.Up;
        if ((pad.Buttons & DpadDown) != 0) return TaskViewKey.Down;
        var x = pad.StickX;
        var y = pad.StickY;
        if (Math.Max(Math.Abs(x), Math.Abs(y)) < StickThreshold) return null;
        if (Math.Abs(x) >= Math.Abs(y)) return x < 0 ? TaskViewKey.Left : TaskViewKey.Right;
        // Stick up is positive.
        return y > 0 ? TaskViewKey.Up : TaskViewKey.Down;
    }
}
