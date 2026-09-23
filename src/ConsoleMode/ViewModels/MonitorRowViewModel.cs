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
    private readonly Action<MonitorRowViewModel> _onSelected;
    private int _roleIndex;
    private DisplayModeOption _selectedMode;

    public static IReadOnlyList<string> RoleOptions { get; } = ["Jogar aqui", "Desligar", "Manter ligada"];

    public MonitorInfo Monitor { get; }

    public MonitorRowViewModel(MonitorInfo monitor, MonitorRole role, DisplayModeOption selected, IReadOnlyList<DisplayModeOption> modes,
        Action<MonitorRowViewModel> onRoleChanged, Action<MonitorRowViewModel> onSelected)
    {
        Monitor = monitor;
        _roleIndex = (int)role;
        Modes = modes;
        _selectedMode = selected;
        _onRoleChanged = onRoleChanged;
        _onSelected = onSelected;
    }

    public string Title => Monitor.DisplayTitle;
    public string Name => Monitor.FriendlyName;
    public string Details => Monitor.IsActive
        ? Monitor.ResolutionText
        : string.IsNullOrEmpty(Monitor.ResolutionText) ? "desligada agora" : $"{Monitor.ResolutionText} · desligada agora";

    public IReadOnlyList<DisplayModeOption> Modes { get; }

    // The settings ComboBoxes are rebound when another screen is selected and briefly push
    // -1 / null back; ignoring those keeps each screen's role and mode intact.
    public int RoleIndex
    {
        get => _roleIndex;
        set
        {
            if (value is < 0 or > 2 || !SetProperty(ref _roleIndex, value)) return;
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(IsFocus));
            OnPropertyChanged(nameof(IsHide));
            OnPropertyChanged(nameof(RoleLabel));
            OnPropertyChanged(nameof(RoleGlyph));
            OnPropertyChanged(nameof(RoleBrush));
            OnPropertyChanged(nameof(TileBackground));
            OnPropertyChanged(nameof(TileBorder));
            OnPropertyChanged(nameof(TileOpacity));
            _onRoleChanged(this);
        }
    }

    public DisplayModeOption SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (value is null) return;
            SetProperty(ref _selectedMode, value);
        }
    }

    [ObservableProperty]
    private double _layoutX;

    [ObservableProperty]
    private double _layoutY;

    [ObservableProperty]
    private double _tileWidth = 160;

    [ObservableProperty]
    private double _tileHeight = 100;

    [ObservableProperty]
    private bool _isSelected;

    public MonitorRole Role
    {
        get => (MonitorRole)_roleIndex;
        set => RoleIndex = (int)value;
    }

    public bool IsFocus => Role == MonitorRole.Focus;
    public bool IsHide => Role == MonitorRole.Hide;

    /// <summary>Off in Windows right now; drawn dashed next to the live desktop.</summary>
    public bool IsOff => !Monitor.IsActive;

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
    public double TileOpacity => IsHide ? 0.6 : 1.0;

    /// <summary>Tile click on the home screen.</summary>
    public void MakeFocus() => Role = MonitorRole.Focus;

    /// <summary>Tile click in settings: pick the screen to edit.</summary>
    public void Select() => _onSelected(this);

    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];
}
