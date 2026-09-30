using System.Text.Json;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ControllerShortcutsTests
{
    private static ushort[] Masks(ushort home = 0, ushort menu = 0, ushort exit = 0) => [home, menu, exit];

    [Fact]
    public void A_new_config_has_no_shortcut_set_and_no_setup_done()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("{}")!;
        Assert.Equal(0, config.HomeShortcut);
        Assert.Equal(0, config.MenuShortcut);
        Assert.Equal(0, config.ExitShortcut);
        Assert.False(config.ShortcutsOnboardingDone);
    }

    [Fact]
    public void An_old_config_with_the_home_button_on_still_starts_with_nothing_set()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("""{"homeButtonLaunch":true}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(0, config.HomeShortcut);
        Assert.False(config.ShortcutsOnboardingDone);
    }

    [Fact]
    public void Suggested_shortcuts_are_valid_and_do_not_collide()
    {
        foreach (var slot in ControllerShortcuts.Slots)
        {
            var suggested = ControllerShortcuts.Suggested(slot);
            Assert.True(ControllerShortcuts.IsAcceptable(suggested), slot.ToString());
            Assert.Null(ControllerShortcuts.FindConflict(slot, suggested, Masks()));
        }
        var all = ControllerShortcuts.Slots.Select(ControllerShortcuts.Suggested).ToArray();
        foreach (var slot in ControllerShortcuts.Slots)
            Assert.Null(ControllerShortcuts.FindConflict(slot, all[(int)slot], all));
    }

    [Theory]
    [InlineData(ControllerShortcuts.Guide, true)]
    [InlineData(ControllerShortcuts.Back | ControllerShortcuts.Y, true)]
    [InlineData(ControllerShortcuts.Start | ControllerShortcuts.Back | ControllerShortcuts.A, true)]
    [InlineData(ControllerShortcuts.Guide | ControllerShortcuts.A, true)]
    [InlineData(ControllerShortcuts.A, false)]          // a lone face button fires in every game
    [InlineData(ControllerShortcuts.Start, false)]
    [InlineData(0, false)]
    [InlineData(0x0001 | 0x0002, false)]                 // D-pad: a Sony stick also sets these bits
    [InlineData(ControllerShortcuts.A | 0x0100, false)]  // shoulder bits are not reported by every pad
    public void Only_combos_or_the_home_button_are_acceptable(int mask, bool expected) =>
        Assert.Equal(expected, ControllerShortcuts.IsAcceptable((ushort)mask));

    [Fact]
    public void The_same_combo_cannot_serve_two_actions()
    {
        var current = Masks(menu: ControllerShortcuts.Back | ControllerShortcuts.Y);
        Assert.Equal(ShortcutSlot.Menu, ControllerShortcuts.FindConflict(ShortcutSlot.Exit, ControllerShortcuts.Back | ControllerShortcuts.Y, current));
    }

    [Fact]
    public void A_combo_that_contains_or_is_inside_another_collides()
    {
        // Start + Back is held on the way to Start + Back + Y: both would fire together.
        var current = Masks(exit: ControllerShortcuts.Start | ControllerShortcuts.Back);
        Assert.Equal(ShortcutSlot.Exit,
            ControllerShortcuts.FindConflict(ShortcutSlot.Menu, ControllerShortcuts.Start | ControllerShortcuts.Back | ControllerShortcuts.Y, current));
        Assert.Equal(ShortcutSlot.Exit,
            ControllerShortcuts.FindConflict(ShortcutSlot.Home, ControllerShortcuts.Start | ControllerShortcuts.Back, current));
    }

    [Fact]
    public void Sharing_one_button_is_fine_and_an_unset_shortcut_never_collides()
    {
        var current = Masks(menu: ControllerShortcuts.Back | ControllerShortcuts.Y);
        Assert.Null(ControllerShortcuts.FindConflict(ShortcutSlot.Exit, ControllerShortcuts.Start | ControllerShortcuts.Back, current));
        Assert.Null(ControllerShortcuts.FindConflict(ShortcutSlot.Home, ControllerShortcuts.Guide, current));
        Assert.False(ControllerShortcuts.Overlaps(0, 0));
    }

    [Fact]
    public void Re_picking_a_slot_does_not_collide_with_itself()
    {
        var current = Masks(menu: ControllerShortcuts.Back | ControllerShortcuts.Y);
        Assert.Null(ControllerShortcuts.FindConflict(ShortcutSlot.Menu, ControllerShortcuts.Back | ControllerShortcuts.Y, current));
    }

    [Theory]
    [InlineData(ControllerShortcuts.Back | ControllerShortcuts.Y, false, "Select + Y")]
    [InlineData(ControllerShortcuts.Back | ControllerShortcuts.Y, true, "Create + △")]
    [InlineData(ControllerShortcuts.Start | ControllerShortcuts.Back, false, "Select + Start")]
    [InlineData(ControllerShortcuts.Start | ControllerShortcuts.Back, true, "Create + Options")]
    [InlineData(ControllerShortcuts.Guide, false, "Xbox")]
    [InlineData(ControllerShortcuts.Guide, true, "PS")]
    [InlineData(0, false, "")]
    public void Combos_are_named_for_the_pad_in_use(int mask, bool playStation, string expected) =>
        Assert.Equal(expected, ControllerShortcuts.Format((ushort)mask, playStation));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(70000, 0)]
    [InlineData(0x0001, 0)]                                  // unknown bits are dropped
    [InlineData(0x0020 | 0x8000, 0x8020)]
    public void Saved_values_are_cleaned_up_on_load(int raw, int expected) =>
        Assert.Equal((ushort)expected, ControllerShortcuts.Sanitize(raw));

    [Fact]
    public void Capture_reports_the_whole_combo_once_the_pad_is_let_go()
    {
        var capture = new ShortcutCapture();
        Assert.Null(capture.Feed(0));                       // armed
        Assert.Null(capture.Feed(ControllerShortcuts.Back));
        Assert.Null(capture.Feed(ControllerShortcuts.Back | ControllerShortcuts.Y));
        Assert.Null(capture.Feed(ControllerShortcuts.Y));   // released one first: still counts both
        Assert.Equal((ushort)(ControllerShortcuts.Back | ControllerShortcuts.Y), capture.Feed(0));
        Assert.Null(capture.Feed(0));                       // reported only once
    }

    [Fact]
    public void Capture_ignores_a_button_still_held_from_before_it_opened()
    {
        var capture = new ShortcutCapture();
        // The A that clicked "Set" is still down when capturing starts.
        Assert.Null(capture.Feed(ControllerShortcuts.A));
        Assert.Null(capture.Feed(ControllerShortcuts.A));
        Assert.Null(capture.Feed(0));                       // released: now armed, and nothing was captured
        Assert.Null(capture.Feed(0));
        Assert.Null(capture.Feed(ControllerShortcuts.Y | ControllerShortcuts.B));
        Assert.Equal((ushort)(ControllerShortcuts.Y | ControllerShortcuts.B), capture.Feed(0));
    }

    [Fact]
    public void Capture_ignores_the_dpad_and_the_stick()
    {
        var capture = new ShortcutCapture();
        Assert.Null(capture.Feed(0));
        Assert.Null(capture.Feed(0x0001 | 0x0008));         // D-pad up + right
        Assert.Null(capture.Feed(0));
    }
}
