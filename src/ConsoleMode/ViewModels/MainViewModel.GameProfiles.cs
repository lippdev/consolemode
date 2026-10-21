using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.ViewModels;

/// <summary>A saved per-game profile, as the settings list shows it.</summary>
public sealed record GameProfileItem(int AppId, string Name, string Summary, string DeleteText);

// Per-game profiles (issue #69): saved from the session menu while a Steam game runs (its
// current resolution, HDR, FPS limit and audio), listed and deleted in the settings.
public partial class MainViewModel
{
    private List<GameProfile> _gameProfiles = [];

    public ObservableCollection<GameProfileItem> GameProfileItems { get; } = [];

    [ObservableProperty] private string _gameProfilesCountText = "";
    [ObservableProperty] private bool _hasGameProfiles;

    /// <summary>The session menu row: the running game and whether it has a profile.</summary>
    [ObservableProperty] private string _sessionProfileText = "";
    [ObservableProperty] private bool _isSessionGameRunning;
    private int _sessionGameId;
    private string _sessionGameName = "";

    private void LoadGameProfiles(IEnumerable<GameProfile>? profiles)
    {
        _gameProfiles = [.. profiles ?? []];
        RefreshGameProfileTexts();
    }

    private void RefreshGameProfileTexts()
    {
        GameProfileItems.Clear();
        foreach (var profile in _gameProfiles)
            GameProfileItems.Add(new GameProfileItem(profile.SteamAppId, profile.Name, GameProfileSummary(profile), LocalizationService.Get("GameProfileDelete")));
        HasGameProfiles = _gameProfiles.Count > 0;
        GameProfilesCountText = HasGameProfiles
            ? LocalizationService.Get("GameProfilesCount", _gameProfiles.Count)
            : LocalizationService.Get("GameProfilesEmpty");
        RefreshSessionProfileText();
    }

    private static string GameProfileSummary(GameProfile profile) => GameProfiles.Summary(profile,
        LocalizationService.Get("GameProfileHdrOn"), LocalizationService.Get("GameProfileHdrOff"),
        LocalizationService.Get("FpsNoLimit"), LocalizationService.Get("GameProfileNothing"));

    /// <summary>Reads the running game (off the UI thread: its name may come from Steam's files).</summary>
    private async Task LoadSessionGameAsync()
    {
        var id = Engine.State.RunningGameId;
        _sessionGameId = id;
        _sessionGameName = id > 0 ? await Task.Run(() => SteamSession.GameName(id)) : "";
        RefreshSessionProfileText();
    }

    private void RefreshSessionProfileText()
    {
        IsSessionGameRunning = _sessionGameId > 0;
        SessionProfileText = _sessionGameId <= 0
            ? LocalizationService.Get("GameProfileNoGame")
            : LocalizationService.Get(GameProfiles.Find(_gameProfiles, _sessionGameId) is null ? "GameProfileNone" : "GameProfileActive", _sessionGameName);
    }

    private IEnumerable<(string Text, string Value, bool Selected)> SessionProfileOptions()
    {
        yield return (LocalizationService.Get("GameProfileSave", _sessionGameName), "save", false);
        if (GameProfiles.Find(_gameProfiles, _sessionGameId) is not null)
            yield return (LocalizationService.Get("GameProfileRemove", _sessionGameName), "remove", false);
    }

    /// <summary>The session menu's profile picker: save what the game screen uses now, or drop the profile.</summary>
    private async Task PickSessionProfileAsync(string action)
    {
        var id = _sessionGameId;
        if (id <= 0) return;
        if (action == "remove")
        {
            SaveGameProfiles(GameProfiles.Remove(_gameProfiles, id));
            AppLog.Write($"Perfil por jogo: perfil de {_sessionGameName} ({id}) apagado");
            return;
        }

        var now = await Task.Run(Engine.ReadGameBaseline);
        var audioName = await Task.Run(() => Engine.Audio.GetDevices().FirstOrDefault(d => d.FriendlyId == now.AudioDeviceId)?.Name);
        var profile = new GameProfile
        {
            SteamAppId = id,
            Name = _sessionGameName,
            Mode = now.Mode,
            Hdr = now.Hdr,
            FpsLimit = Engine.Rtss.IsReady ? now.FpsLimit : null,
            AudioDeviceId = now.AudioDeviceId,
            AudioDeviceName = audioName
        };
        SaveGameProfiles(GameProfiles.Upsert(_gameProfiles, profile));
        AppLog.Write($"Perfil por jogo: perfil de {profile.Name} ({id}) salvo: {GameProfileSummary(profile)}");
    }

    private void SaveGameProfiles(List<GameProfile> profiles)
    {
        _gameProfiles = profiles;
        if (Engine.State.IsActive) Engine.OnGameProfileSaved(profiles);
        RefreshGameProfileTexts();
        SaveQuietly();
    }

    [RelayCommand]
    private void RemoveGameProfile(GameProfileItem? item)
    {
        if (item is null) return;
        SaveGameProfiles(GameProfiles.Remove(_gameProfiles, item.AppId));
        AppLog.Write($"Perfil por jogo: perfil de {item.Name} ({item.AppId}) apagado");
    }
}
