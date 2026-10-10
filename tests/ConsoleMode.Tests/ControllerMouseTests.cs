using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerMouseTests
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.1, 0.1)]     // a worn stick resting off center
    [InlineData(0.17, 0.0)]
    public void Stick_inside_the_dead_zone_does_not_move(double x, double y) =>
        Assert.Equal((0.0, 0.0), ControllerMouseMath.Curve(x, y, ControllerMouseMath.MoveDeadZone));

    [Fact]
    public void Curve_keeps_the_direction_and_reaches_full_speed()
    {
        var (x, y) = ControllerMouseMath.Curve(0, -1, ControllerMouseMath.MoveDeadZone);
        Assert.Equal(0, x, 6);
        Assert.Equal(-1, y, 6);
        var (dx, dy) = ControllerMouseMath.Curve(0.5, 0.5, ControllerMouseMath.MoveDeadZone);
        Assert.Equal(dx, dy, 6);
    }

    [Fact]
    public void Half_tilt_is_much_slower_than_full_tilt()
    {
        var (half, _) = ControllerMouseMath.Curve(0.5, 0, ControllerMouseMath.MoveDeadZone);
        var (full, _) = ControllerMouseMath.Curve(1, 0, ControllerMouseMath.MoveDeadZone);
        Assert.True(half < full / 3, $"half={half} full={full}");
    }

    [Theory]
    [InlineData(32767, 1.0)]
    [InlineData(-32768, -1.0)]
    [InlineData(0, 0.0)]
    public void XInput_sticks_are_normalized(short value, double expected) =>
        Assert.Equal(expected, ControllerMouseMath.FromXInput(value), 3);

    [Theory]
    [InlineData(128, false, 0.0)]
    [InlineData(255, false, 1.0)]
    [InlineData(0, false, -1.0)]
    [InlineData(0, true, 1.0)]      // Sony Y grows downward: pushed up reads 0
    public void Hid_sticks_are_normalized(byte value, bool invert, double expected) =>
        Assert.Equal(expected, ControllerMouseMath.FromHidByte(value, invert), 2);

    [Fact]
    public void Pointer_moves_right_and_up_on_screen()
    {
        var motion = new PointerMotion();
        var (dx, dy) = motion.Step(1, 1, 0.1, 1080);
        Assert.True(dx > 0);
        Assert.True(dy < 0, "stick up must move the pointer up (screen Y decreases)");
    }

    [Fact]
    public void Pointer_speed_follows_the_screen_height()
    {
        var hd = new PointerMotion().Step(1, 0, 0.1, 1080).Dx;
        var uhd = new PointerMotion().Step(1, 0, 0.1, 2160).Dx;
        Assert.InRange(uhd, hd * 2 - 1, hd * 2 + 1);
    }

    [Fact]
    public void Slow_moves_accumulate_instead_of_stalling()
    {
        var motion = new PointerMotion();
        var total = 0;
        for (var i = 0; i < 100; i++) total += motion.Step(0.25, 0, 0.008, 1080).Dx;
        Assert.True(total > 0, "a slight tilt must still move the pointer");
        Assert.Equal(0, new PointerMotion().Step(0.25, 0, 0.008, 1080).Dx);   // less than a pixel in one sample
    }

    [Fact]
    public void Wheel_sends_a_notch_at_once_then_by_rate()
    {
        var wheel = new WheelMotion();
        Assert.Equal(1, wheel.Step(0.6, 0.008));
        var total = 0;
        for (var i = 0; i < 125; i++) total += wheel.Step(1, 0.008);   // one second at full tilt
        Assert.InRange(total, ControllerMouseMath.MaxNotchesPerSecond - 1, ControllerMouseMath.MaxNotchesPerSecond + 1);
    }

    [Fact]
    public void Wheel_down_is_negative_and_the_dead_zone_resets_it()
    {
        var wheel = new WheelMotion();
        Assert.Equal(-1, wheel.Step(-0.9, 0.008));
        Assert.Equal(0, wheel.Step(0.1, 0.008));
        Assert.Equal(1, wheel.Step(0.9, 0.008));   // back out of the dead zone: a fresh first notch
    }

    /// <summary>Feeds A samples (time in ms, held) and collects the events.</summary>
    private static List<ClickEvent> Run(ClickGesture gesture, params (int Ms, bool Held)[] samples) =>
        samples.Select(s => gesture.Update(s.Held, Ms(s.Ms))).Where(e => e != ClickEvent.None).ToList();

    private static ClickGesture Ready()
    {
        var gesture = new ClickGesture();
        gesture.Update(false, TimeSpan.Zero);
        return gesture;
    }

    [Fact]
    public void A_tap_is_a_left_click_once_the_double_tap_window_passes()
    {
        var events = Run(Ready(), (10, true), (80, false), (200, false), (340, false));
        Assert.Equal([ClickEvent.LeftClick], events);
    }

    [Fact]
    public void A_tap_does_not_click_before_the_window_ends()
    {
        var events = Run(Ready(), (10, true), (80, false), (200, false));
        Assert.Empty(events);
    }

    [Fact]
    public void Two_quick_taps_are_one_right_click()
    {
        var events = Run(Ready(), (10, true), (80, false), (180, true), (250, false), (700, false));
        Assert.Equal([ClickEvent.RightClick], events);
    }

    [Fact]
    public void Holding_a_drags_with_the_left_button()
    {
        var events = Run(Ready(), (10, true), (200, true), (370, true), (900, true), (950, false), (1500, false));
        Assert.Equal([ClickEvent.LeftDown, ClickEvent.LeftUp], events);
    }

    [Fact]
    public void A_held_when_the_mode_starts_is_ignored_until_released()
    {
        var gesture = new ClickGesture();
        var events = Run(gesture, (0, true), (500, true), (600, false), (1200, false));
        Assert.Empty(events);
        Assert.Equal([ClickEvent.LeftClick], Run(gesture, (1300, true), (1350, false), (1700, false)));
    }

    [Fact]
    public void Reset_mid_drag_asks_for_the_left_button_to_be_released()
    {
        var gesture = Ready();
        Run(gesture, (10, true), (400, true));
        Assert.True(gesture.Reset());
        Assert.False(gesture.Reset());
        Assert.Empty(Run(gesture, (500, true), (900, true)));   // still waiting for A to go up
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2999, false)]
    [InlineData(3000, true)]
    public void Pointer_hides_after_three_seconds_still(int stillMs, bool expected) =>
        Assert.Equal(expected, ControllerMouseMath.ShouldHideCursor(Ms(stillMs)));
}
