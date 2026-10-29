using System.Text.Json;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

// The corner and timing cases come from @JarJarShinks' tests in #173.
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
    public void The_second_park_waits_for_the_launcher_to_settle_and_there_is_no_third()
    {
        Assert.False(PointerParking.IsDue(true, 1, true, T0, T0.AddSeconds(2.9)));
        Assert.True(PointerParking.IsDue(true, 1, true, T0, T0.AddSeconds(3)));
        Assert.False(PointerParking.IsDue(true, PointerParking.Parks, true, T0, T0.AddMinutes(5)));
    }

    [Fact]
    public void The_pointer_goes_back_only_after_a_park_and_onto_a_screen_that_is_on()
    {
        Assert.True(PointerParking.ShouldReturn((800, 600), parks: 1, onScreen: true));
        Assert.False(PointerParking.ShouldReturn((800, 600), parks: 0, onScreen: true));   // never parked: left alone
        Assert.False(PointerParking.ShouldReturn((800, 600), parks: 2, onScreen: false));  // that screen stayed off
        Assert.False(PointerParking.ShouldReturn(null, parks: 2, onScreen: true));         // position unknown
    }

    [Fact]
    public void It_is_on_by_default_also_for_configs_saved_before_it_existed()
    {
        Assert.True(new AppConfig().ParkPointer);
        Assert.True(JsonSerializer.Deserialize<AppConfig>("{}")!.ParkPointer);
        Assert.False(JsonSerializer.Deserialize<AppConfig>("""{"ParkPointer":false}""")!.ParkPointer);
    }
}
