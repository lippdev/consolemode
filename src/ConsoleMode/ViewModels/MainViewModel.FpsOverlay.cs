using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The FPS counter drawn by RTSS over the game (RtssOverlay): picked in Settings or cycled from
// the session menu; size, card, color and the custom items are set in Settings. Changes during a
// session show at once.
public partial class MainViewModel
{
    public ObservableCollection<ComboOption> FpsOverlayOptions { get; } = [];
    public ObservableCollection<ComboOption> FpsOverlayColorOptions { get; } = [];
    public ObservableCollection<ComboOption> FpsOverlaySizeOptions { get; } = [];

    [ObservableProperty] private ComboOption? _selectedFpsOverlay;
    [ObservableProperty] private ComboOption? _selectedFpsOverlayColor;
    [ObservableProperty] private ComboOption? _selectedFpsOverlaySize;
    [ObservableProperty] private bool _overlayShowApi = true;
    [ObservableProperty] private bool _overlayShowFps = true;
    [ObservableProperty] private bool _overlayShowFrameTime = true;
    [ObservableProperty] private bool _overlayShowCpu;
    [ObservableProperty] private bool _overlayShowCpuClock;
    [ObservableProperty] private bool _overlayShowGpu;
    [ObservableProperty] private bool _overlayShowGpuClock;
    [ObservableProperty] private bool _overlayShowGpuTemp;
    [ObservableProperty] private bool _overlayShowVram;
    [ObservableProperty] private bool _overlayShowRam;
    [ObservableProperty] private bool _overlaySingleLine;
    [ObservableProperty] private bool _overlayCard = true;

    public bool IsRtssInstalled => Engine.Rtss.IsInstalled;
    public bool IsFpsOverlayCustom => FpsOverlayStyle == RtssOverlay.Custom;
    /// <summary>The style draws something of ours, so size, card and color mean something.</summary>
    public bool IsFpsOverlayDrawn => RtssOverlay.Text(FpsOverlayStyle).Length > 0;
    public string FpsOverlayStatusText => LocalizationService.Get(IsRtssInstalled ? "FpsOverlayDescription" : "FpsUnavailable");

    private string FpsOverlayStyle => RtssOverlay.Normalize(SelectedFpsOverlay?.Value);

    private string FpsOverlayShortName => SelectedFpsOverlay is null ? "" : LocalizationService.Get(StyleKey(FpsOverlayStyle));

    private FpsOverlayLayout ReadFpsOverlayLayout() => new()
    {
        ShowApi = OverlayShowApi,
        ShowFps = OverlayShowFps,
        ShowFrameTime = OverlayShowFrameTime,
        ShowCpu = OverlayShowCpu,
        ShowCpuClock = OverlayShowCpuClock,
        ShowGpu = OverlayShowGpu,
        ShowGpuClock = OverlayShowGpuClock,
        ShowGpuTemp = OverlayShowGpuTemp,
        ShowVram = OverlayShowVram,
        ShowRam = OverlayShowRam,
        SingleLine = OverlaySingleLine,
        Color = RtssOverlay.NormalizeColor(SelectedFpsOverlayColor?.Value),
        Size = RtssOverlay.NormalizeSize(SelectedFpsOverlaySize?.Value),
        Card = OverlayCard
    };

    private void BuildFpsOverlayOptions()
    {
        var style = FpsOverlayStyle;
        var color = RtssOverlay.NormalizeColor(SelectedFpsOverlayColor?.Value);
        var size = RtssOverlay.NormalizeSize(SelectedFpsOverlaySize?.Value);
        FpsOverlayOptions.Clear();
        // Settings says what Compact and Detailed show; the session menu tile keeps the short name.
        foreach (var s in RtssOverlay.Styles)
            FpsOverlayOptions.Add(new ComboOption
            {
                Text = s is RtssOverlay.Compact or RtssOverlay.Detailed
                    ? $"{LocalizationService.Get(StyleKey(s))} ({LocalizationService.Get(StyleKey(s) + "Hint")})"
                    : LocalizationService.Get(StyleKey(s)),
                Value = s
            });
        SelectedFpsOverlay = FpsOverlayOptions.First(o => o.Value == style);

        string[] names = ["White", "Blue", "Green", "Yellow", "Orange"];
        FpsOverlayColorOptions.Clear();
        for (var i = 0; i < RtssOverlay.Colors.Length; i++)
            FpsOverlayColorOptions.Add(new ComboOption { Text = LocalizationService.Get("FpsOverlayColor" + names[i]), Value = RtssOverlay.Colors[i] });
        SelectedFpsOverlayColor = FpsOverlayColorOptions.First(o => o.Value == color);

        string[] sizes = ["Small", "Medium", "Large"];
        FpsOverlaySizeOptions.Clear();
        for (var i = 0; i < RtssOverlay.Sizes.Length; i++)
            FpsOverlaySizeOptions.Add(new ComboOption { Text = LocalizationService.Get("FpsOverlaySize" + sizes[i]), Value = RtssOverlay.Sizes[i] });
        SelectedFpsOverlaySize = FpsOverlaySizeOptions.First(o => o.Value == size);
        OnPropertyChanged(nameof(FpsOverlayStatusText));
    }

    private static string StyleKey(string style) => style switch
    {
        RtssOverlay.Off => "FpsOverlayOff",
        RtssOverlay.FpsOnly => "FpsOverlayFpsOnly",
        RtssOverlay.Compact => "FpsOverlayCompact",
        RtssOverlay.Detailed => "FpsOverlayDetailed",
        RtssOverlay.Custom => "FpsOverlayCustom",
        _ => "FpsOverlayExternal"
    };

    private void LoadFpsOverlay(AppConfig config)
    {
        var layout = config.FpsOverlayLayout ?? new FpsOverlayLayout();
        SelectedFpsOverlay = FpsOverlayOptions.FirstOrDefault(o => o.Value == RtssOverlay.Normalize(config.FpsOverlay)) ?? FpsOverlayOptions.FirstOrDefault();
        SelectedFpsOverlayColor = FpsOverlayColorOptions.FirstOrDefault(o => o.Value == RtssOverlay.NormalizeColor(layout.Color)) ?? FpsOverlayColorOptions.FirstOrDefault();
        SelectedFpsOverlaySize = FpsOverlaySizeOptions.FirstOrDefault(o => o.Value == RtssOverlay.NormalizeSize(layout.Size)) ?? FpsOverlaySizeOptions.FirstOrDefault();
        OverlayShowApi = layout.ShowApi;
        OverlayShowFps = layout.ShowFps;
        OverlayShowFrameTime = layout.ShowFrameTime;
        OverlayShowCpu = layout.ShowCpu;
        OverlayShowCpuClock = layout.ShowCpuClock;
        OverlayShowGpu = layout.ShowGpu;
        OverlayShowGpuClock = layout.ShowGpuClock;
        OverlayShowGpuTemp = layout.ShowGpuTemp;
        OverlayShowVram = layout.ShowVram;
        OverlayShowRam = layout.ShowRam;
        OverlaySingleLine = layout.SingleLine;
        OverlayCard = layout.Card;
    }

    partial void OnSelectedFpsOverlayChanged(ComboOption? value) => FpsOverlayChanged();
    partial void OnSelectedFpsOverlayColorChanged(ComboOption? value) => FpsOverlayChanged();
    partial void OnSelectedFpsOverlaySizeChanged(ComboOption? value) => FpsOverlayChanged();
    partial void OnOverlayShowApiChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowFpsChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowFrameTimeChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowCpuChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowCpuClockChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowGpuChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowGpuClockChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowGpuTempChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowVramChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowRamChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlaySingleLineChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayCardChanged(bool value) => FpsOverlayChanged();

    private void FpsOverlayChanged()
    {
        OnPropertyChanged(nameof(IsFpsOverlayCustom));
        OnPropertyChanged(nameof(IsFpsOverlayDrawn));
        SessionOverlayText = FpsOverlayShortName;
        if (_applying) return;
        SettingChanged();
        if (!IsConsoleActive || !IsRtssInstalled) return;
        var style = FpsOverlayStyle;
        var layout = ReadFpsOverlayLayout();
        _ = Task.Run(() =>
        {
            if (!Engine.Rtss.ApplyOverlay(style, layout))
                AppLog.Write("RTSS: o contador de FPS não pôde ser exibido");
        });
    }

    /// <summary>A on the session menu row: the next style, shown at once and kept for the next sessions.</summary>
    [RelayCommand]
    private void CycleFpsOverlay()
    {
        var next = RtssOverlay.Next(FpsOverlayStyle);
        SelectedFpsOverlay = FpsOverlayOptions.FirstOrDefault(o => o.Value == next) ?? SelectedFpsOverlay;
    }
}
