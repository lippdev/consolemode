using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class RtssOverlayTests
{
    private static readonly HardwareSample Pc = new()
    {
        CpuUsage = 45.4, CpuClockMhz = 4321, GpuUsage = 97.6, GpuClockMhz = 1905, GpuTempC = 66.8,
        VramUsedMb = 5324, VramTotalMb = 8192, RamUsedMb = 15155, RamTotalMb = 32768
    };

    [Fact]
    public void Styles_are_listed_from_least_to_most()
    {
        Assert.Equal(["external", "off", "compact", "stats", "detailed", "custom"], RtssOverlay.Styles);
        Assert.Equal(RtssOverlay.External, RtssOverlay.Normalize("bogus"));
    }

    [Theory]
    [InlineData("top-left", "<P0>")]
    [InlineData("top-right", "<P2>")]
    [InlineData("bottom-left", "<P6>")]
    [InlineData("bottom-right", "<P8>")]
    public void A_corner_pins_the_counter_there_with_or_without_the_card(string position, string tag)
    {
        foreach (var card in new[] { true, false })
        {
            var text = RtssOverlay.Text(RtssOverlay.FpsOnly, new FpsOverlayLayout { Position = position, Card = card });
            Assert.Contains(tag + (card ? "<M=" : "<L0>"), text);
        }
    }

    [Theory]
    [InlineData("rtss")]
    [InlineData(null)]
    [InlineData("middle")]
    public void Otherwise_it_follows_the_rtss_osd_position(string? position)
    {
        var text = RtssOverlay.Text(RtssOverlay.FpsOnly, new FpsOverlayLayout { Position = position!, Card = false });
        Assert.DoesNotContain("<P", text);
        Assert.DoesNotContain("<L", text);
    }

    [Fact]
    public void Default_leaves_afterburner_alone()
    {
        Assert.Equal(RtssOverlay.External, RtssOverlay.Normalize(null));
        Assert.False(RtssOverlay.HidesOthers(null));
    }

    // 1.6.0 saved FPS-only as "compact": it must stay FPS-only, not grow into the new Compact.
    [Fact]
    public void A_saved_compact_from_1_6_0_stays_fps_only()
    {
        Assert.Equal(RtssOverlay.FpsOnly, RtssOverlay.Normalize("compact"));
        var text = RtssOverlay.Text("compact", null, Pc);
        Assert.Contains("<FR>", text);
        Assert.DoesNotContain("CPU", text);
    }

    [Fact]
    public void Off_draws_nothing_and_hides_afterburner()
    {
        Assert.Equal("", RtssOverlay.Text(RtssOverlay.Off));
        Assert.True(RtssOverlay.HidesOthers(RtssOverlay.Off));
    }

    [Fact]
    public void External_draws_nothing() => Assert.Equal("", RtssOverlay.Text(RtssOverlay.External));

    [Theory]
    [InlineData("compact", "small")]
    [InlineData("stats", "medium")]
    [InlineData("detailed", "large")]
    [InlineData("custom", "large")]
    public void Styles_show_the_live_framerate_and_fit_the_rtss_slot(string style, string size)
    {
        var all = new FpsOverlayLayout
        {
            ShowCpu = true, ShowCpuClock = true, ShowGpu = true, ShowGpuClock = true, ShowGpuTemp = true,
            ShowVram = true, ShowRam = true, Size = size
        };
        foreach (var sample in new[] { null, Pc })
        {
            var text = RtssOverlay.Text(style, all, sample);
            Assert.Contains("<FR>", text);
            Assert.True(text.Length < 4096);
            Assert.All(text, c => Assert.True(c < 256, "the RTSS slot is Latin-1"));
        }
    }

    [Fact]
    public void Compact_is_one_line_of_usages()
    {
        var text = RtssOverlay.Text(RtssOverlay.Compact, null, Pc);
        Assert.DoesNotContain("\n", text);
        Assert.Contains("CPU", text);
        Assert.Contains(">45<", text);
        Assert.Contains(">98<", text);
        Assert.Contains(">46<", text);   // RAM as a percentage
        Assert.DoesNotContain("GHz", text);
    }

    [Fact]
    public void Detailed_has_clocks_temperature_memory_frametime_and_api()
    {
        var text = RtssOverlay.Text(RtssOverlay.Detailed, null, Pc);
        Assert.Contains("<FT>", text);
        Assert.Contains("<APP>", text);
        Assert.Contains(">4.3<", text);
        Assert.Contains(" GHz", text);
        Assert.Contains(">1905<", text);
        Assert.Contains(">67<", text);
        Assert.Contains("°C", text);
        Assert.Contains(">5.2<", text);
        Assert.Contains(" / 8 GB", text);
        Assert.Contains(">14.8<", text);
        Assert.Contains("\n", text);
    }

    [Fact]
    public void Numbers_use_a_dot_whatever_the_windows_language()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
        try
        {
            Assert.Contains(">4.3<", RtssOverlay.Text(RtssOverlay.Detailed, null, Pc));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Before_the_first_sample_values_show_dashes()
    {
        var text = RtssOverlay.Text(RtssOverlay.Compact);
        Assert.Contains(">--<", text);
        Assert.Contains("CPU", text);
    }

    [Fact]
    public void What_this_pc_cannot_report_is_left_out()
    {
        var text = RtssOverlay.Text(RtssOverlay.Detailed, null, Pc with { GpuTempC = null, GpuClockMhz = null, VramUsedMb = null });
        Assert.DoesNotContain("°C", text);
        Assert.DoesNotContain("MHz", text);
        Assert.DoesNotContain("VRAM", text);
        Assert.Contains("GPU", text);
    }

    [Fact]
    public void Card_draws_a_rounded_background_that_can_be_turned_off()
    {
        var card = RtssOverlay.Text(RtssOverlay.FpsOnly);
        Assert.Contains("<B=0,0,R", card);
        Assert.Contains("\b", card);

        var plain = RtssOverlay.Text(RtssOverlay.FpsOnly, new FpsOverlayLayout { Card = false });
        Assert.DoesNotContain("<B=", plain);
        Assert.DoesNotContain("<L", plain);
    }

    [Theory]
    [InlineData("small", "<S0=100>")]
    [InlineData("medium", "<S0=150>")]
    [InlineData("large", "<S0=200>")]
    [InlineData("huge", "<S0=100>")]
    public void Size_scales_every_text(string size, string expected) =>
        Assert.Contains(expected, RtssOverlay.Text(RtssOverlay.Detailed, new FpsOverlayLayout { Size = size }));

    [Fact]
    public void Only_styles_with_pc_data_need_a_refresh()
    {
        Assert.False(RtssOverlay.NeedsHardware(RtssOverlay.FpsOnly));
        Assert.False(RtssOverlay.NeedsHardware(RtssOverlay.Off));
        Assert.True(RtssOverlay.NeedsHardware(RtssOverlay.Compact));
        Assert.True(RtssOverlay.NeedsHardware(RtssOverlay.Detailed));
        Assert.False(RtssOverlay.NeedsHardware(RtssOverlay.Custom, new FpsOverlayLayout()));
        Assert.True(RtssOverlay.NeedsHardware(RtssOverlay.Custom, new FpsOverlayLayout { ShowGpuTemp = true }));
    }

    [Fact]
    public void Custom_shows_only_what_is_ticked_on_one_line_in_its_color()
    {
        var text = RtssOverlay.Text(RtssOverlay.Custom,
            new FpsOverlayLayout { ShowApi = false, ShowFps = true, ShowFrameTime = true, ShowGpu = true, SingleLine = true, Color = "6fe07a" }, Pc);
        Assert.DoesNotContain("<APP>", text);
        Assert.Contains("<FT>", text);
        Assert.Contains("GPU", text);
        Assert.DoesNotContain("CPU", text);
        Assert.DoesNotContain("\n", text);
        Assert.StartsWith("<C0=6FE07A>", text);
    }

    [Fact]
    public void Custom_with_nothing_ticked_still_shows_the_fps()
    {
        var text = RtssOverlay.Text(RtssOverlay.Custom, new FpsOverlayLayout { ShowApi = false, ShowFps = false, ShowFrameTime = false });
        Assert.Contains("<FR>", text);
    }

    [Theory]
    [InlineData("FF0000><C0=000000", "FFFFFF")]
    [InlineData("", "FFFFFF")]
    [InlineData(null, "FFFFFF")]
    [InlineData("ffd54a", "FFD54A")]
    public void Colors_from_the_config_cannot_break_the_tags(string? color, string expected) =>
        Assert.Equal(expected, RtssOverlay.NormalizeColor(color));
}

public class GpuCountersTests
{
    private const string Dgpu = "luid_0x00000000_0x0000D1E4_phys_0";
    private const string Igpu = "luid_0x00000000_0x0000A2B1_phys_0";

    [Fact]
    public void Picks_the_busiest_gpu_summing_every_process_per_engine()
    {
        var gpu = GpuCounters.Busiest(
        [
            ($"pid_100_{Dgpu}_eng_0_engtype_3D", 40),
            ($"pid_200_{Dgpu}_eng_0_engtype_3D", 35),
            ($"pid_100_{Dgpu}_eng_3_engtype_Copy", 5),
            ($"pid_300_{Igpu}_eng_0_engtype_3D", 20),
            ($"pid_300_{Igpu}_eng_1_engtype_VideoDecode", 60),
        ]);

        Assert.NotNull(gpu);
        Assert.Equal(Dgpu, gpu.Value.Key, ignoreCase: true);
        Assert.Equal(75, gpu.Value.Usage, 3);
        Assert.Equal(0xD1E4u, gpu.Value.LuidLow);
        Assert.Equal(0, gpu.Value.LuidHigh);
        Assert.Equal(0u, gpu.Value.Node);
    }

    [Fact]
    public void Usage_is_capped_and_the_clock_is_read_from_the_3d_engine()
    {
        var gpu = GpuCounters.Busiest(
        [
            ($"pid_1_{Dgpu}_eng_2_engtype_Compute", 70),
            ($"pid_2_{Dgpu}_eng_2_engtype_Compute", 50),
            ($"pid_1_{Dgpu}_eng_5_engtype_3D", 10),
        ]);

        Assert.Equal(100, gpu!.Value.Usage);
        Assert.Equal(5u, gpu.Value.Node);
    }

    [Fact]
    public void Idle_gpus_fall_back_to_the_one_with_the_most_vram_in_use()
    {
        var gpu = GpuCounters.Busiest(
            [($"pid_1_{Igpu}_eng_0_engtype_3D", 0), ($"pid_1_{Dgpu}_eng_0_engtype_3D", 0)],
            [(Igpu, 300.0 * 1024 * 1024), (Dgpu, 1500.0 * 1024 * 1024)]);

        Assert.Equal(Dgpu, gpu!.Value.Key, ignoreCase: true);
        Assert.Equal(1500, GpuCounters.DedicatedMb([(Igpu, 1.0), (Dgpu, 1500.0 * 1024 * 1024)], gpu.Value.Key));
    }

    [Fact]
    public void No_gpu_counters_means_no_gpu()
    {
        Assert.Null(GpuCounters.Busiest([]));
        Assert.Null(GpuCounters.Busiest([("garbage", 50)]));
    }
}
