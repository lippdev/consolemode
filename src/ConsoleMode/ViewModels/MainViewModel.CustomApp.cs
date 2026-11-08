using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace ConsoleMode.ViewModels;

// "Custom app" launch mode: any .exe as the console launcher (issue #111), as many as you like, each a tile on Home
// with its exe's icon and name. The three properties below are the selected one's, edited in System.
public partial class MainViewModel
{
    [ObservableProperty] private string _customAppPath = "";
    [ObservableProperty] private string _customAppArguments = "";
    [ObservableProperty] private string _customAppWatchProcess = "";

    private List<CustomAppEntry> _customApps = [];
    private string _customAppId = "";
    /// <summary>Showing another app's values: they are not edits to save.</summary>
    private bool _showingCustomApp;

    public bool IsCustomApp => CustomApp.IsEntryMode(SelectedLaunch?.Value);

    private CustomAppEntry? SelectedCustomApp => _customApps.FirstOrDefault(a => a.Id == _customAppId);

    /// <summary>The exe's description or file name, shown in summaries.</summary>
    public string CustomAppName => CustomApp.Validate(CustomAppPath) == CustomApp.Problem.None
        ? LaunchService.GetCustomAppName(CustomAppPath)
        : LocalizationService.Get("LaunchCustomApp");

    public string CustomAppPathText => CustomApp.ProblemText(CustomAppPath) ?? CustomApp.CleanPath(CustomAppPath);

    /// <summary>The selected launcher as the summaries show it: a custom app by its own name.</summary>
    private string LaunchSummaryText => IsCustomApp
        ? CustomAppName
        : SelectedLaunch?.Text ?? LocalizationService.Get("LaunchSteam");

    /// <summary>The config's custom apps (an older config's one app becomes the first) and the one in use.</summary>
    private void LoadCustomApps(AppConfig config)
    {
        _customApps = CustomApp.Entries(config);
        _customAppId = CustomApp.Selected(_customApps, config.CustomAppId)?.Id ?? "";
        ShowSelectedCustomApp();
    }

    /// <summary>The launcher value a config's mode stands for: a custom app's own value for "customApp".</summary>
    private string LaunchValueOf(AppConfig config) =>
        config.FullscreenMode == CustomApp.Mode && _customAppId.Length > 0 ? CustomApp.EntryMode(_customAppId) : config.FullscreenMode;

    private void ShowSelectedCustomApp()
    {
        var app = SelectedCustomApp;
        _showingCustomApp = true;
        try
        {
            CustomAppPath = app?.Path ?? "";
            CustomAppArguments = app?.Arguments ?? "";
            CustomAppWatchProcess = app?.WatchProcess ?? "";
        }
        finally
        {
            _showingCustomApp = false;
        }
        OnPropertyChanged(nameof(CustomAppName));
        OnPropertyChanged(nameof(CustomAppPathText));
    }

    /// <summary>A custom app's tile was picked as the launcher: System shows and edits that one.</summary>
    private void OnCustomAppPicked(string? launchValue)
    {
        if (CustomApp.EntryId(launchValue) is not { } id || id == _customAppId) return;
        _customAppId = id;
        ShowSelectedCustomApp();
    }

    partial void OnCustomAppPathChanged(string value)
    {
        OnPropertyChanged(nameof(CustomAppName));
        OnPropertyChanged(nameof(CustomAppPathText));
        if (_showingCustomApp || SelectedCustomApp is not { } app) return;
        app.Path = CustomApp.CleanPath(value);
        // Its tile takes the new exe's name and icon.
        RebuildLaunchOptions();
        SettingChanged();
    }

    partial void OnCustomAppArgumentsChanged(string value)
    {
        if (_showingCustomApp || SelectedCustomApp is not { } app) return;
        app.Arguments = value;
        SaveQuietly();
    }

    partial void OnCustomAppWatchProcessChanged(string value)
    {
        if (_showingCustomApp || SelectedCustomApp is not { } app) return;
        app.WatchProcess = value;
        SaveQuietly();
    }

    /// <summary>The "+" tile on Home and System → Add a custom app: pick an exe, it becomes a tile and the launcher.</summary>
    [RelayCommand]
    private async Task AddCustomAppAsync()
    {
        if (IsConsoleActive || await PickExeAsync() is not { } path) return;
        var app = new CustomAppEntry { Id = CustomApp.NextId(_customApps.Select(a => a.Id)), Path = path };
        _customApps.Add(app);
        _customAppId = app.Id;
        ShowSelectedCustomApp();
        RebuildLaunchOptions();
        SelectedLaunch = LaunchOptions.FirstOrDefault(o => o.Value == CustomApp.EntryMode(app.Id)) ?? SelectedLaunch;
        AppLog.Write($"Aplicativo personalizado: adicionado {path}");
        SettingChanged();
    }

    /// <summary>System → the custom app's exe: another exe for the selected app (or a new app when there is none).</summary>
    [RelayCommand]
    private async Task PickCustomAppPathAsync()
    {
        if (SelectedCustomApp is null) { await AddCustomAppAsync(); return; }
        if (await PickExeAsync() is { } path) CustomAppPath = path;
    }

    /// <summary>System → Remove this app: its tile goes, and Steam Big Picture opens instead.</summary>
    [RelayCommand]
    private void RemoveCustomApp()
    {
        if (SelectedCustomApp is { } app) RemoveCustomAppEntry(app);
    }

    /// <summary>
    /// Takes an app off Home (System, or its tile's menu). The launcher that opens stays, unless it was this app: then
    /// Steam Big Picture opens instead. System goes on showing another of the apps, if any is left.
    /// </summary>
    private void RemoveCustomAppEntry(CustomAppEntry app)
    {
        if (IsConsoleActive || !_customApps.Remove(app)) return;
        var wasLauncher = SelectedLaunch?.Value == CustomApp.EntryMode(app.Id);
        if (_customAppId == app.Id)
        {
            _customAppId = _customApps.FirstOrDefault()?.Id ?? "";
            ShowSelectedCustomApp();
        }
        RebuildLaunchOptions(wasLauncher ? "bigPicture" : null);
        AppLog.Write($"Aplicativo personalizado: removido {app.Path}");
        SettingChanged();
    }

    // ── A custom app's tile menu (Start / Options on it, or a right click): Remove from Home ──

    private const string TileMenuRemove = "remove";
    private const string TileMenuCancel = "cancel";
    /// <summary>The app whose tile menu is open (its id).</summary>
    private string _tileMenuAppId = "";

    /// <summary>Only the apps added with "+" have one: Steam, Playnite and Xbox mode always stay.</summary>
    public static bool HasTileMenu(LaunchTile? tile) => tile is { IsAdd: false } && CustomApp.IsEntryMode(tile.Option.Value);

    /// <summary>Opens the tile's menu in the option picker, titled with the app's name. False when it has none.</summary>
    public bool OpenTileMenu(LaunchTile? tile)
    {
        if (IsConsoleActive || !HasTileMenu(tile) || CustomApp.EntryId(tile!.Option.Value) is not { } id) return false;
        _tileMenuAppId = id;
        _pickerKey = "tile";
        PickerTitle = tile.Text;
        PickerOptions.Clear();
        PickerOptions.Add(new PickerItem(LocalizationService.Get("RemoveFromHome"), TileMenuRemove, false));
        PickerOptions.Add(new PickerItem(LocalizationService.Get("Cancel"), TileMenuCancel, false));
        IsRolePanelOpen = false;
        IsPickerOpen = true;
        AppLog.Write($"Início: menu do bloco \"{tile.Text}\"");
        return true;
    }

    private void PickTileMenu(string value)
    {
        if (value == TileMenuRemove && _customApps.FirstOrDefault(a => a.Id == _tileMenuAppId) is { } app) RemoveCustomAppEntry(app);
        _tileMenuAppId = "";
    }

    private async Task<string?> PickExeAsync()
    {
        try
        {
            var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            picker.FileTypeFilter.Add(".exe");
            if (App.MainWindowInstance is { } window)
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
        catch (Exception ex)
        {
            AppLog.Write($"Aplicativo personalizado: {ex}");
            SetStatus(LocalizationService.Get("PlaynitePathPickError", ex.Message), InfoBarSeverity.Error);
            return null;
        }
    }

    /// <summary>The custom apps as saved: copies, so later edits don't change a config already handed to the engine.</summary>
    private List<CustomAppEntry> CustomAppsToSave() =>
        _customApps.Select(a => new CustomAppEntry { Id = a.Id, Path = CustomApp.CleanPath(a.Path), Arguments = a.Arguments.Trim(), WatchProcess = a.WatchProcess.Trim() }).ToList();
}
