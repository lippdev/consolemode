using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class PointerParkingTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void The_pointer_goes_a_few_pixels_inside_the_bottom_right_corner()
    {
        var tv = new ScreenRect { X = 0, Y = 0, Width = 3840, Height = 2160 };
        Assert.Equal((3833, 2153), PointerParking.BottomRight(tv));
    }

    [Fact]
    public void A_screen_left_of_or_above_the_main_one_has_negative_coordinates()
    {
        var left = new ScreenRect { X = -1920, Y = 0, Width = 1920, Height = 1080 };
        Assert.Equal((-7, 1073), PointerParking.BottomRight(left));

        var above = new ScreenRect { X = 0, Y = -2160, Width = 3840, Height = 2160 };
        Assert.Equal((3833, -7), PointerParking.BottomRight(above));
    }

    [Fact]
    public void A_screen_to_the_right_uses_its_own_offset()
    {
        var right = new ScreenRect { X = 4480, Y = 0, Width = 3840, Height = 2160 };
        Assert.Equal((8313, 2153), PointerParking.BottomRight(right));
    }

    [Fact]
    public void A_degenerate_screen_never_pushes_the_pointer_outside_it()
    {
        var tiny = new ScreenRect { X = 100, Y = 50, Width = 3, Height = 3 };
        Assert.Equal((100, 50), PointerParking.BottomRight(tiny));
    }

    [Fact]
    public void Nothing_happens_while_the_setting_is_off_or_the_launcher_is_not_up()
    {
        Assert.False(PointerParking.IsDue(false, 0, true, null, T0));
        Assert.False(PointerParking.IsDue(true, 0, false, null, T0));
    }

    [Fact]
    public void The_first_park_is_due_as_soon_as_the_launcher_is_up() =>
        Assert.True(PointerParking.IsDue(true, 0, true, null, T0));

    [Fact]
    public void The_second_park_waits_for_the_launcher_to_settle()
    {
        Assert.False(PointerParking.IsDue(true, 1, true, T0, T0.AddSeconds(1)));
        Assert.False(PointerParking.IsDue(true, 1, true, T0, T0.AddSeconds(2.9)));
        Assert.True(PointerParking.IsDue(true, 1, true, T0, T0.AddSeconds(3)));
    }

    [Fact]
    public void It_stops_after_two_parks_per_session()
    {
        Assert.Equal(2, PointerParking.Parks);
        Assert.False(PointerParking.IsDue(true, 2, true, T0, T0.AddMinutes(5)));
    }
}
