using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConsoleMode.Models;
using ConsoleMode.Services;
using ConsoleMode.Services.Tv;
using Microsoft.UI.Xaml.Controls;

namespace ConsoleMode.ViewModels;

// Settings → TV: turn the TV on and switch it to the PC's input (issue #75).
public partial class MainViewModel
{
    public ObservableCollection<ComboOption> TvProviders { get; } = [];
    public ObservableCollection<ComboOption> TvHdmiInputs { get; } = [];

    [ObservableProperty] private ComboOption? _selectedTvProvider;
    [ObservableProperty] private ComboOption? _selectedTvHdmiInput;
    [ObservableProperty] private string _tvHost = "";
    [ObservableProperty] private string _tvMacAddress = "";
    [ObservableProperty] private string _tvInputCommand = "";
    [ObservableProperty] private bool _tvTurnOffOnRestore;
    [ObservableProperty] private string _haUrl = "";
    [ObservableProperty] private string _haToken = "";
    [ObservableProperty] private string _haOnEntity = "";
    [ObservableProperty] private string _haOffEntity = "";
    [ObservableProperty] private bool _isTestingTv;

    private string TvProvider => SelectedTvProvider?.Value ?? TvControlConfig.None;

    public bool IsTvEnabled => TvProvider != TvControlConfig.None;
    public bool IsTvAndroid => TvProvider == TvControlConfig.AndroidTv;
    public bool IsTvHomeAssistant => TvProvider == TvControlConfig.HomeAssistant;
    public bool UsesTvHost => IsTvAndroid;
    public bool UsesTvHdmiInput => IsTvAndroid;

    public string TvProviderDescription => LocalizationService.Get(TvProvider switch
    {
        TvControlConfig.AndroidTv => "TvAndroidDescription",
        TvControlConfig.HomeAssistant => "TvHaDescription",
        _ => "TvNoneDescription"
    });

    partial void OnSelectedTvProviderChanged(ComboOption? value)
    {
        OnPropertyChanged(nameof(IsTvEnabled));
        OnPropertyChanged(nameof(IsTvAndroid));
        OnPropertyChanged(nameof(IsTvHomeAssistant));
        OnPropertyChanged(nameof(UsesTvHost));
        OnPropertyChanged(nameof(UsesTvHdmiInput));
        OnPropertyChanged(nameof(TvProviderDescription));
        SaveQuietly();
    }

    partial void OnSelectedTvHdmiInputChanged(ComboOption? value) => SaveQuietly();
    partial void OnTvHostChanged(string value) => SaveQuietly();
    partial void OnTvMacAddressChanged(string value) => SaveQuietly();
    partial void OnTvInputCommandChanged(string value) => SaveQuietly();
    partial void OnTvTurnOffOnRestoreChanged(bool value) => SaveQuietly();
    partial void OnHaUrlChanged(string value) => SaveQuietly();
    partial void OnHaTokenChanged(string value) => SaveQuietly();
    partial void OnHaOnEntityChanged(string value) => SaveQuietly();
    partial void OnHaOffEntityChanged(string value) => SaveQuietly();

    /// <summary>Part of <see cref="BuildLocalizedOptions"/>: the names follow the interface language.</summary>
    private void BuildTvOptions()
    {
        var provider = SelectedTvProvider?.Value ?? _loadedConfig.Tv?.Provider ?? TvControlConfig.None;
        var hdmi = SelectedTvHdmiInput?.Value ?? (_loadedConfig.Tv?.HdmiInput ?? 1).ToString();

        TvProviders.Clear();
        TvProviders.Add(new ComboOption { Text = LocalizationService.Get("TvProviderNone"), Value = TvControlConfig.None });
        TvProviders.Add(new ComboOption { Text = LocalizationService.Get("TvProviderAndroid"), Value = TvControlConfig.AndroidTv });
        TvProviders.Add(new ComboOption { Text = LocalizationService.Get("TvProviderHomeAssistant"), Value = TvControlConfig.HomeAssistant });
        SelectedTvProvider = TvProviders.FirstOrDefault(o => o.Value == provider) ?? TvProviders[0];

        TvHdmiInputs.Clear();
        for (var i = 1; i <= 4; i++)
            TvHdmiInputs.Add(new ComboOption { Text = $"HDMI {i}", Value = i.ToString() });
        SelectedTvHdmiInput = TvHdmiInputs.FirstOrDefault(o => o.Value == hdmi) ?? TvHdmiInputs[0];
    }

    private void ApplyTv(TvControlConfig? tv)
    {
        tv ??= new TvControlConfig();
        SelectedTvProvider = TvProviders.FirstOrDefault(o => o.Value == tv.Provider) ?? TvProviders.FirstOrDefault();
        SelectedTvHdmiInput = TvHdmiInputs.FirstOrDefault(o => o.Value == tv.HdmiInput.ToString()) ?? TvHdmiInputs.FirstOrDefault();
        TvHost = tv.Host ?? "";
        TvMacAddress = tv.MacAddress ?? "";
        TvInputCommand = tv.InputCommand ?? "";
        TvTurnOffOnRestore = tv.TurnOffOnRestore;
        HaUrl = tv.HomeAssistantUrl ?? "";
        HaToken = SecretProtector.Unprotect(tv.HomeAssistantToken ?? "");
        HaOnEntity = tv.HomeAssistantOnEntity ?? "";
        HaOffEntity = tv.HomeAssistantOffEntity ?? "";
    }

    private TvControlConfig BuildTvConfig() => new()
    {
        Provider = TvProvider,
        Host = TvHost.Trim(),
        MacAddress = TvMacAddress.Trim(),
        HdmiInput = int.TryParse(SelectedTvHdmiInput?.Value, out var hdmi) ? hdmi : 1,
        InputCommand = TvInputCommand.Trim(),
        TurnOffOnRestore = TvTurnOffOnRestore,
        HomeAssistantUrl = HaUrl.Trim(),
        HomeAssistantToken = SecretProtector.Protect(HaToken.Trim()),
        HomeAssistantOnEntity = HaOnEntity.Trim(),
        HomeAssistantOffEntity = HaOffEntity.Trim()
    };

    /// <summary>Turns the TV on and switches the input now; the first time, pairs with the TV.</summary>
    [RelayCommand]
    private async Task TestTvAsync()
    {
        if (IsTestingTv) return;
        IsTestingTv = true;
        SetStatus(LocalizationService.Get("TvTesting"), InfoBarSeverity.Informational);
        try
        {
            var config = BuildTvConfig();
            await Task.Run(() => Engine.Tv.TestAsync(config, CancellationToken.None));
            SetStatus(LocalizationService.Get("TvTestSuccess"), InfoBarSeverity.Success);
        }
        catch (TvControlException ex)
        {
            AppLog.Write($"TV: teste falhou: {ex.Message}");
            SetStatus(ex.Message, InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            AppLog.Write($"TV: teste falhou: {ex}");
            SetStatus(LocalizationService.Get("TvTestFailure", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsTestingTv = false;
        }
    }
}
