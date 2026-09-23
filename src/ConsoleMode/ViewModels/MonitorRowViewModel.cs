using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleMode.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace ConsoleMode.ViewModels;

public enum MonitorRole
{
    Focus = 0,
    Hide = 1,
    Keep = 2
}

public partial class MonitorRowViewModel : ObservableObject
{
    private readonly Action<MonitorRowViewModel> _onRoleChanged;

    public static IReadOnlyList<string> RoleOptions { get; } = ["Jogar aqui", "Desligar", "Manter ligada"];

    public MonitorInfo Monitor { get; }

    public MonitorRowViewModel(MonitorInfo monitor, MonitorRole role, DisplayModeOption selected, IReadOnlyList<DisplayModeOption> modes, Action<MonitorRowViewModel> onRoleChanged)
    {
        Monitor = monitor;
        _roleIndex = (int)role;
        Modes = modes;
        _selectedMode = selected;
        _onRoleChanged = onRoleChanged;
    }

    public string Title => Monitor.DisplayTitle;
    public string Name => Monitor.FriendlyName;
    public string Details => Monitor.IsActive
        ? Monitor.ResolutionText
        : string.IsNullOrEmpty(Monitor.ResolutionText) ? "desligada agora" : $"{Monitor.ResolutionText} · desligada agora";

    public IReadOnlyList<DisplayModeOption> Modes { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Role), nameof(IsFocus), nameof(IsHide), nameof(RoleLabel), nameof(RoleGlyph), nameof(TileBackground), nameof(TileBorder), nameof(TileOpacity), nameof(RoleBrush))]
    private int _roleIndex;

    [ObservableProperty]
    private DisplayModeOption _selectedMode;

    [ObservableProperty]
    private double _tileWidth = 160;

    [ObservableProperty]
    private double _tileHeight = 100;

    public MonitorRole Role
    {
        get => (MonitorRole)Math.Clamp(RoleIndex, 0, 2);
        set => RoleIndex = (int)value;
    }

    public bool IsFocus => Role == MonitorRole.Focus;
    public bool IsHide => Role == MonitorRole.Hide;

    public string RoleLabel => Role switch
    {
        MonitorRole.Focus => "Jogar aqui",
        MonitorRole.Hide => "Desligar",
        _ => "Fica ligada"
    };

    public string RoleGlyph => Role switch
    {
        MonitorRole.Focus => "",
        MonitorRole.Hide => "",
        _ => ""
    };

    public Brush TileBackground => Resource(IsFocus ? "AccentDarkBrush" : "CardBrush");
    public Brush TileBorder => Resource(IsFocus ? "AccentBrush" : "BorderBrush");
    public Brush RoleBrush => Resource(IsFocus ? "AccentBrush" : "MutedBrush");
    public double TileOpacity => IsHide ? 0.55 : 1.0;

    /// <summary>Tile click on the home screen.</summary>
    public void MakeFocus() => Role = MonitorRole.Focus;

    partial void OnRoleIndexChanged(int value) => _onRoleChanged(this);

    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];
}
