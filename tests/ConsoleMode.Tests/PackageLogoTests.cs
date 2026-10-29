using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public class PackageLogoTests
{
    // The Xbox app's assets folder, trimmed.
    private static readonly string[] XboxFiles =
    [
        "Xbox_AppList.scale-200.png",
        "Xbox_AppList.scale-200_contrast-black.png",
        "Xbox_AppList.targetsize-16.png",
        "Xbox_AppList.targetsize-48.png",
        "Xbox_AppList.targetsize-256.png",
        "Xbox_AppList.targetsize-256_altform-lightunplated.png",
        "Xbox_AppList.targetsize-256_contrast-white.png",
        "Xbox_MedTile.scale-200.png",
    ];

    [Fact]
    public void The_biggest_plain_logo_wins() =>
        Assert.Equal("Xbox_AppList.targetsize-256.png", PackageLogo.Pick(@"assets\Xbox_AppList.png", XboxFiles));

    [Fact]
    public void The_unplated_form_beats_the_plated_one_at_the_same_size() =>
        Assert.Equal("Logo.targetsize-256_altform-unplated.png",
            PackageLogo.Pick("Logo.png", ["Logo.targetsize-256.png", "Logo.targetsize-256_altform-unplated.png", "Logo.targetsize-48_altform-unplated.png"]));

    [Fact]
    public void Contrast_themes_are_never_picked() =>
        Assert.Null(PackageLogo.Pick("Logo.png", ["Logo.targetsize-256_contrast-black.png", "Logo.scale-400_contrast-white.png"]));

    [Fact]
    public void Scaled_logos_count_by_their_scale() =>
        Assert.Equal("Logo.scale-400.png", PackageLogo.Pick("Logo.png", ["Logo.scale-100.png", "Logo.scale-400.png", "Logo.scale-200.png"]));

    [Fact]
    public void A_logo_without_variants_is_used_as_is() =>
        Assert.Equal("StoreLogo.png", PackageLogo.Pick(@"Assets\StoreLogo.png", ["StoreLogo.png", "Other.png"]));

    [Fact]
    public void Other_logos_and_extensions_are_ignored() =>
        Assert.Null(PackageLogo.Pick("Logo.png", ["Logo.targetsize-256.jpg", "Logo2.png", "Splash.png"]));
}
