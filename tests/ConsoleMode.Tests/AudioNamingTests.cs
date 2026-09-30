using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class AudioNamingTests
{
    [Theory]
    [InlineData("Realtek(R) Audio", "Speakers", @"Realtek(R) Audio\Device\Speakers\Render")]
    [InlineData("NVIDIA High Definition Audio", "LG TV SSCR2", @"NVIDIA High Definition Audio\Device\LG TV SSCR2\Render")]
    public void Friendly_id_matches_the_one_saved_by_1_5(string adapter, string endpoint, string expected) =>
        Assert.Equal(expected, AudioNaming.FriendlyId(adapter, endpoint));

    [Theory]
    [InlineData("Speakers", "Realtek(R) Audio", "Speakers (Realtek(R) Audio)")]
    [InlineData("Speakers", "", "Speakers")]
    [InlineData("", "Realtek(R) Audio", "Realtek(R) Audio")]
    [InlineData("LG TV", "LG TV", "LG TV")]
    public void Display_name_shows_the_endpoint_and_its_adapter(string endpoint, string adapter, string expected) =>
        Assert.Equal(expected, AudioNaming.DisplayName(endpoint, adapter));
}
