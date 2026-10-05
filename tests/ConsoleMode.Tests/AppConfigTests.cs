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
}
