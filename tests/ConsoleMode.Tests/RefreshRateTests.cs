using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class RefreshRateTests
{
    [Theory]
    [InlineData(120, 120)]
    [InlineData(119, 120)]   // Windows can list 119.88 Hz as 119
    [InlineData(60, 60)]
    [InlineData(59, 60)]
    [InlineData(30, 30)]
    [InlineData(29, 30)]
    [InlineData(24, 24)]
    [InlineData(23, 24)]
    [InlineData(48, 48)]
    [InlineData(47, 48)]
    [InlineData(240, 240)]
    [InlineData(239, 240)]
    [InlineData(144, 144)]
    [InlineData(165, 165)]
    [InlineData(0, 0)]
    public void A_listed_rate_stands_for_its_whole_number_base(int listed, int expected) =>
        Assert.Equal(expected, RefreshRate.NtscBase(listed));

    [Theory]
    [InlineData(120, true)]
    [InlineData(119, true)]
    [InlineData(60, true)]
    [InlineData(24, true)]
    [InlineData(144, false)]
    [InlineData(165, false)]
    [InlineData(100, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void Only_tv_rates_have_a_1000_over_1001_twin(int hertz, bool expected) =>
        Assert.Equal(expected, RefreshRate.HasNtscTwin(hertz));

    [Fact]
    public void The_twin_of_120_is_119_88_as_a_fraction()
    {
        Assert.True(RefreshRate.TryNtsc(120, out var numerator, out var denominator));
        Assert.Equal(120000u, numerator);
        Assert.Equal(1001u, denominator);
        Assert.Equal(119.88, RefreshRate.ToHertz(numerator, denominator), 2);

        // 119 is how Windows may list 119.88: it gives the same fraction, not 118.88.
        Assert.True(RefreshRate.TryNtsc(119, out var n2, out var d2));
        Assert.Equal((numerator, denominator), (n2, d2));

        Assert.True(RefreshRate.TryNtsc(60, out var n3, out var d3));
        Assert.Equal(59.94, RefreshRate.ToHertz(n3, d3), 2);
    }

    [Theory]
    [InlineData(144)]
    [InlineData(165)]
    [InlineData(0)]
    public void A_rate_without_a_twin_gives_no_fraction(int hertz)
    {
        Assert.False(RefreshRate.TryNtsc(hertz, out var numerator, out var denominator));
        Assert.Equal(0u, numerator);
        Assert.Equal(0u, denominator);
    }

    [Fact]
    public void Rates_match_within_a_few_hundredths_of_a_hertz()
    {
        Assert.True(RefreshRate.Matches(120000, 1001, 119880, 1000));
        Assert.True(RefreshRate.Matches(120000, 1001, 119879, 1000));
        Assert.False(RefreshRate.Matches(120000, 1001, 120, 1));      // 119.88 is not 120
        Assert.False(RefreshRate.Matches(120000, 1001, 144, 1));
        Assert.False(RefreshRate.Matches(120000, 0, 120000, 1001));   // an unreadable rate matches nothing
    }

    [Fact]
    public void Zero_denominator_reads_as_zero_hertz() =>
        Assert.Equal(0, RefreshRate.ToHertz(120000, 0));

    [Theory]
    [InlineData(119.88, "119.88")]
    [InlineData(120.0, "120")]
    [InlineData(59.94, "59.94")]
    [InlineData(60.001, "60")]
    [InlineData(23.976, "23.98")]
    public void The_label_uses_a_dot_and_drops_empty_decimals(double hertz, string expected)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
        try { Assert.Equal(expected, RefreshRate.Label(hertz)); }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }
}
