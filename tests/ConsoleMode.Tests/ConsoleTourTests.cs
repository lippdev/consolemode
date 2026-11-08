using System.Text.Json;
using System.Text.RegularExpressions;
using ConsoleMode.Models;
using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class ConsoleTourTests
{
    [Fact]
    public void A_stop_stays_long_enough_to_read_it_and_then_moves_on()
    {
        Assert.Equal(TimeSpan.FromSeconds(7), ConsoleTour.DwellTime("Hi", "Short."));
        Assert.Equal(TimeSpan.FromSeconds(16), ConsoleTour.DwellTime("Title", new string('a', 2000)));
        // A typical stop: a title and two sentences, about 13 s.
        var typical = ConsoleTour.DwellTime(new string('t', 20), new string('b', 140));
        Assert.Equal(13, typical.TotalSeconds, 1);
    }

    [Fact]
    public void The_tour_opens_with_a_welcome_and_closes_with_the_way_back_to_it()
    {
        Assert.Equal(ConsoleTourStop.Welcome, ConsoleTour.Stops[0]);
        Assert.Equal(ConsoleTourStop.Done, ConsoleTour.Stops[^1]);
        Assert.True(ConsoleTour.IsCentered(ConsoleTourStop.Welcome));
        Assert.True(ConsoleTour.IsCentered(ConsoleTourStop.Done));
        Assert.All(ConsoleTour.Stops.Skip(1).SkipLast(1), stop => Assert.False(ConsoleTour.IsCentered(stop)));
    }

    [Fact]
    public void It_walks_the_tabs_left_to_right_and_ends_back_on_home()
    {
        var tabs = ConsoleTour.Stops.SkipLast(1).Select(ConsoleTour.Tab).ToList();
        Assert.Equal(tabs.Order(), tabs);
        Assert.Equal([0, 1, 2], tabs.Distinct());
        Assert.Equal(0, ConsoleTour.Tab(ConsoleTourStop.Done));
    }

    [Fact]
    public void The_shortcuts_stop_opens_the_controller_category()
    {
        Assert.Equal(2, ConsoleTour.Tab(ConsoleTourStop.Shortcuts));
        Assert.Equal(2, ConsoleTour.SystemCategory(ConsoleTourStop.Shortcuts));
        Assert.Equal(0, ConsoleTour.SystemCategory(ConsoleTourStop.System));
        Assert.Null(ConsoleTour.SystemCategory(ConsoleTourStop.Play));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void It_is_offered_until_finished_and_never_over_a_session(bool tourDone, bool sessionActive, bool expected) =>
        Assert.Equal(expected, ConsoleTour.ShouldOffer(tourDone, sessionActive));

    [Fact]
    public void Next_and_back_stop_at_both_ends()
    {
        Assert.Equal(1, ConsoleTour.Step(0, 1));
        Assert.Null(ConsoleTour.Step(0, -1));
        Assert.Equal(ConsoleTour.Stops.Count - 2, ConsoleTour.Step(ConsoleTour.Stops.Count - 1, -1));
        Assert.Null(ConsoleTour.Step(ConsoleTour.Stops.Count - 1, 1));
    }

    [Fact]
    public void Every_stop_has_its_texts_in_every_catalog_with_the_hints_it_is_given()
    {
        // How many {n} the view model fills in for each stop (MainViewModel.ConsoleTourArgs).
        var arguments = new Dictionary<ConsoleTourStop, int>
        {
            [ConsoleTourStop.Tabs] = 2,
            [ConsoleTourStop.Launchers] = 1,
            [ConsoleTourStop.Play] = 2,
            [ConsoleTourStop.Screens] = 1,
            [ConsoleTourStop.System] = 1,
            [ConsoleTourStop.Done] = 2
        };
        try
        {
            foreach (var language in LocalizationService.SupportedLanguages)
            {
                LocalizationService.SetLanguage(language.Code);
                var keys = LocalizationService.GetKeys(language.Code).ToHashSet();
                foreach (var stop in ConsoleTour.Stops)
                {
                    Assert.Contains(ConsoleTour.TitleKey(stop), keys);
                    Assert.Contains(ConsoleTour.BodyKey(stop), keys);
                    var body = LocalizationService.Get(ConsoleTour.BodyKey(stop));
                    var placeholders = Regex.Matches(body, @"\{\d+\}").Select(m => m.Value).Distinct().Count();
                    Assert.True(placeholders == arguments.GetValueOrDefault(stop), $"{language.Code} {stop}: {placeholders} placeholders");
                }
                Assert.Contains("ConsoleTourFinish", keys);
            }
        }
        finally
        {
            LocalizationService.SetLanguage(LocalizationService.PortugueseBrazil);
        }
    }

    [Fact]
    public void Everyone_sees_the_new_tour_once_even_after_the_old_one()
    {
        var config = JsonSerializer.Deserialize<AppConfig>("""{"TourDone":true,"UiMode":"desktop"}""")!;
        Assert.False(config.ConsoleTourDone);
    }
}
