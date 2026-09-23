using System.Text.Json;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void Embedded_catalogs_support_en_us_and_pt_br_with_matching_keys()
    {
        LocalizationService.SetLanguage(LocalizationService.PortugueseBrazil);
        Assert.Equal("Onde você vai jogar?", LocalizationService.Get("HomeHeading"));

        var portugueseKeys = LocalizationService.GetKeys(LocalizationService.PortugueseBrazil);
        var englishKeys = LocalizationService.GetKeys(LocalizationService.EnglishUnitedStates);
        Assert.Equal(portugueseKeys.Order(), englishKeys.Order());

        var changedProperties = new List<string?>();
        void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => changedProperties.Add(e.PropertyName);
        LocalizationService.Texts.PropertyChanged += OnChanged;
        try
        {
            LocalizationService.SetLanguage(LocalizationService.EnglishUnitedStates);
            Assert.Equal("Where do you want to play?", LocalizationService.Get("HomeHeading"));
            Assert.Equal("Returning to normal in 15 sec", LocalizationService.Get("Countdown", 15));
            Assert.Contains(nameof(LocalizedStrings.HomeHeading), changedProperties);
        }
        finally
        {
            LocalizationService.Texts.PropertyChanged -= OnChanged;
            LocalizationService.SetLanguage(LocalizationService.PortugueseBrazil);
        }

        LocalizationService.SetLanguage("fr-FR");
        Assert.Equal(LocalizationService.PortugueseBrazil, LocalizationService.Language);
        Assert.Equal("Onde você vai jogar?", LocalizationService.Get("HomeHeading"));
    }

    [Fact]
    public void Every_xaml_text_property_has_a_key_in_both_catalogs()
    {
        var properties = typeof(LocalizedStrings)
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
        foreach (var language in new[] { LocalizationService.PortugueseBrazil, LocalizationService.EnglishUnitedStates })
        {
            var keys = LocalizationService.GetKeys(language).ToHashSet();
            Assert.Empty(properties.Where(p => !keys.Contains(p)));
        }
    }

    [Fact]
    public void Display_mode_labels_follow_the_language_switch()
    {
        LocalizationService.SetLanguage(LocalizationService.PortugueseBrazil);
        try
        {
            var noChange = new DisplayModeOption { Key = "current", UseCurrent = true, TextKey = "DisplayNoChange" }.RefreshText();
            var cached = new DisplayModeOption { Width = 3840, Height = 2160, Frequency = 60, TextKey = "CachedModeSuffix" }.RefreshText();
            var portugueseNoChange = noChange.Text;
            var portugueseCached = cached.Text;
            Assert.StartsWith("3840 x 2160 @ 60 Hz", portugueseCached);

            var changed = new List<string?>();
            cached.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            LocalizationService.SetLanguage(LocalizationService.EnglishUnitedStates);
            noChange.RefreshText();
            cached.RefreshText();

            Assert.Equal("Don't change", noChange.Text);
            Assert.NotEqual(portugueseNoChange, noChange.Text);
            Assert.Equal("3840 x 2160 @ 60 Hz (cached)", cached.Text);
            Assert.NotEqual(portugueseCached, cached.Text);
            Assert.Contains(nameof(DisplayModeOption.Text), changed);
        }
        finally
        {
            LocalizationService.SetLanguage(LocalizationService.PortugueseBrazil);
        }
    }

    [Fact]
    public void App_language_config_defaults_for_existing_users_and_roundtrips()
    {
        var existingConfig = JsonSerializer.Deserialize<AppConfig>("{}")!;
        Assert.Equal(LocalizationService.PortugueseBrazil, existingConfig.AppLanguage);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var englishConfig = new AppConfig { AppLanguage = LocalizationService.EnglishUnitedStates };
        var saved = JsonSerializer.Serialize(englishConfig, options);
        Assert.Contains("\"appLanguage\":\"en-US\"", saved);
        var loaded = JsonSerializer.Deserialize<AppConfig>(saved, options)!;
        Assert.Equal(LocalizationService.EnglishUnitedStates, loaded.AppLanguage);
    }
}
