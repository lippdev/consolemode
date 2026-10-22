namespace ConsoleMode.Services;

/// <summary>
/// The "NTSC" twin of a whole-number refresh rate: many TVs run their 120 Hz mode at 119.88 Hz (120 × 1000/1001),
/// 60 Hz at 59.94 Hz, and so on. Windows' mode list is whole numbers (and often shows 119.88 as 119), so the exact
/// rate is asked for as a fraction. Pure, so it's tested.
/// </summary>
public static class RefreshRate
{
    /// <summary>The rates that have a 1000/1001 twin on TVs.</summary>
    private static readonly int[] NtscBases = [24, 30, 48, 60, 120, 240];

    /// <summary>
    /// The whole-number rate a listed rate stands for. Windows can list 119.88 as 119 (and 59.94 as 59), so one
    /// below a base counts as that base; anything else is itself.
    /// </summary>
    public static int NtscBase(int hertz)
    {
        foreach (var rate in NtscBases)
            if (hertz == rate || hertz == rate - 1) return rate;
        return hertz;
    }

    public static bool HasNtscTwin(int hertz) => hertz > 0 && Array.IndexOf(NtscBases, NtscBase(hertz)) >= 0;

    /// <summary>The twin as a fraction (120 → 120000/1001); false when the rate has none (144, 165…).</summary>
    public static bool TryNtsc(int hertz, out uint numerator, out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (!HasNtscTwin(hertz)) return false;
        numerator = (uint)NtscBase(hertz) * 1000;
        denominator = 1001;
        return true;
    }

    public static double ToHertz(uint numerator, uint denominator) =>
        denominator == 0 ? 0 : (double)numerator / denominator;

    /// <summary>True when two fractions are the same rate for any practical purpose (within 0.02 Hz).</summary>
    public static bool Matches(uint numeratorA, uint denominatorA, uint numeratorB, uint denominatorB) =>
        denominatorA != 0 && denominatorB != 0 &&
        Math.Abs(ToHertz(numeratorA, denominatorA) - ToHertz(numeratorB, denominatorB)) < 0.02;

    /// <summary>"119.88" for a fractional rate, "120" for a whole one; always a dot, whatever the language.</summary>
    public static string Label(double hertz)
    {
        var rounded = Math.Round(hertz);
        return Math.Abs(hertz - rounded) < 0.005
            ? rounded.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : hertz.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
