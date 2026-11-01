using System.Reflection;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class AppConfigTests
{
    [Fact]
    public void Copy_for_a_session_swaps_only_the_monitor_fields()
    {
        var config = new AppConfig { FocusMonitor = "id-tv", HideMonitors = ["id-desk"] };
        var hide = new List<string> { @"\\.\DISPLAY1" };
        var modes = new Dictionary<string, SavedDisplayMode>();

        var copy = config.WithMonitors(@"\\.\DISPLAY2", hide, modes);

        Assert.Equal(@"\\.\DISPLAY2", copy.FocusMonitor);
        Assert.Same(hide, copy.HideMonitors);
        Assert.Same(modes, copy.MonitorModes);
        Assert.Equal("id-tv", config.FocusMonitor);
        Assert.Equal(["id-desk"], config.HideMonitors);
    }

    // The session used to get a field-by-field copy that forgot the FPS counter: the saved style
    // was never applied when console mode started.
    [Fact]
    public void Copy_for_a_session_keeps_the_fps_counter()
    {
        var layout = new FpsOverlayLayout { SingleLine = true };
        var config = new AppConfig { FpsOverlay = RtssOverlay.Compact, FpsOverlayLayout = layout };

        var copy = config.WithMonitors("", [], []);

        Assert.Equal(RtssOverlay.Compact, copy.FpsOverlay);
        Assert.Same(layout, copy.FpsOverlayLayout);
    }

    [Fact]
    public void Copy_for_a_session_keeps_every_other_setting()
    {
        string[] swapped = [nameof(AppConfig.FocusMonitor), nameof(AppConfig.HideMonitors), nameof(AppConfig.MonitorModes), nameof(AppConfig.SetupKey)];
        var config = new AppConfig();
        var properties = typeof(AppConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !swapped.Contains(p.Name))
            .ToList();
        foreach (var property in properties)
        {
            // A value that differs from the default, so a property left behind shows up.
            if (property.PropertyType == typeof(string)) property.SetValue(config, "changed-" + property.Name);
            else if (property.PropertyType == typeof(bool)) property.SetValue(config, !(bool)property.GetValue(config)!);
            else if (property.PropertyType == typeof(int)) property.SetValue(config, (int)property.GetValue(config)! + 7);
            else property.SetValue(config, Activator.CreateInstance(property.PropertyType));
        }

        var copy = config.WithMonitors("", [], []);

        foreach (var property in properties)
            Assert.Equal(property.GetValue(config), property.GetValue(copy));
    }

    // The Task View shortcut is on from the start (Select + X), also for a config saved before it existed;
    // turned off, it stays off.
    [Fact]
    public void Task_view_shortcut_defaults_to_select_x_and_stays_off_once_turned_off()
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        var old = System.Text.Json.JsonSerializer.Deserialize<AppConfig>("{\"menuShortcut\":32800}", options)!;
        Assert.Equal(ControllerShortcuts.Back | ControllerShortcuts.X, old.TaskViewShortcut);
        Assert.Equal(ControllerShortcuts.Suggested(ShortcutSlot.TaskView), old.TaskViewShortcut);
        var off = System.Text.Json.JsonSerializer.Deserialize<AppConfig>("{\"taskViewShortcut\":0}", options)!;
        Assert.Equal(0, off.TaskViewShortcut);
        Assert.Equal(0, new AppConfig().TaskViewHintsShown);
        // Windows already drives Task View with an Xbox pad; a PlayStation one is driven from here unless turned off.
        Assert.True(old.TaskViewPlayStation);
    }
}
