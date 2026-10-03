using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

// The FPS counter drawn by RTSS over the game (RtssOverlay): picked in Settings or cycled from
// the session menu; the custom layout is set in Settings. Changes during a session show at once.
public partial class MainViewModel
{
    public ObservableCollection<ComboOption> FpsOverlayOptions { get; } = [];
    public ObservableCollection<ComboOption> FpsOverlayColorOptions { get; } = [];

    [ObservableProperty] private ComboOption? _selectedFpsOverlay;
    [ObservableProperty] private ComboOption? _selectedFpsOverlayColor;
    [ObservableProperty] private bool _overlayShowApi = true;
    [ObservableProperty] private bool _overlayShowFps = true;
    [ObservableProperty] private bool _overlayShowFrameTime = true;
    [ObservableProperty] private bool _overlaySingleLine;

    public bool IsRtssInstalled => Engine.Rtss.IsInstalled;
    public bool IsFpsOverlayCustom => FpsOverlayStyle == RtssOverlay.Custom;
    public string FpsOverlayStatusText => LocalizationService.Get(IsRtssInstalled ? "FpsOverlayDescription" : "FpsUnavailable");

    private string FpsOverlayStyle => RtssOverlay.Normalize(SelectedFpsOverlay?.Value);

    private FpsOverlayLayout ReadFpsOverlayLayout() => new()
    {
        ShowApi = OverlayShowApi,
        ShowFps = OverlayShowFps,
        ShowFrameTime = OverlayShowFrameTime,
        SingleLine = OverlaySingleLine,
        Color = RtssOverlay.NormalizeColor(SelectedFpsOverlayColor?.Value)
    };

    private void BuildFpsOverlayOptions()
    {
        var style = FpsOverlayStyle;
        var color = RtssOverlay.NormalizeColor(SelectedFpsOverlayColor?.Value);
        FpsOverlayOptions.Clear();
        foreach (var s in RtssOverlay.Styles)
            FpsOverlayOptions.Add(new ComboOption { Text = LocalizationService.Get(StyleKey(s)), Value = s });
        SelectedFpsOverlay = FpsOverlayOptions.First(o => o.Value == style);

        string[] names = ["White", "Blue", "Green", "Yellow", "Orange"];
        FpsOverlayColorOptions.Clear();
        for (var i = 0; i < RtssOverlay.Colors.Length; i++)
            FpsOverlayColorOptions.Add(new ComboOption { Text = LocalizationService.Get("FpsOverlayColor" + names[i]), Value = RtssOverlay.Colors[i] });
        SelectedFpsOverlayColor = FpsOverlayColorOptions.First(o => o.Value == color);
        OnPropertyChanged(nameof(FpsOverlayStatusText));
    }

    private static string StyleKey(string style) => style switch
    {
        RtssOverlay.Off => "FpsOverlayOff",
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
        OverlayShowApi = layout.ShowApi;
        OverlayShowFps = layout.ShowFps;
        OverlayShowFrameTime = layout.ShowFrameTime;
        OverlaySingleLine = layout.SingleLine;
    }

    partial void OnSelectedFpsOverlayChanged(ComboOption? value) => FpsOverlayChanged();
    partial void OnSelectedFpsOverlayColorChanged(ComboOption? value) => FpsOverlayChanged();
    partial void OnOverlayShowApiChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowFpsChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlayShowFrameTimeChanged(bool value) => FpsOverlayChanged();
    partial void OnOverlaySingleLineChanged(bool value) => FpsOverlayChanged();

    private void FpsOverlayChanged()
    {
        OnPropertyChanged(nameof(IsFpsOverlayCustom));
        SessionOverlayText = SelectedFpsOverlay?.Text ?? "";
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
