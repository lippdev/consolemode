using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class ReleaseNotesTests
{
    private const string Bilingual = """
        ## Português (Brasil)

        ### Novidades
        - **Escolha** do idioma no instalador.

        ### Arquivos para baixar
        - Instalador

        ---

        ## English (US)

        ### What's new
        - **Language** choice in the installer.

        ### Downloads
        - Installer
        """;

    private const string LegacyPortuguese = """
        ### Novidades
        - Interface em inglês (Estados Unidos).

        ## Arquivos para baixar
        - Instalador
        """;

    [Fact]
    public void FirstLine_PicksPortugueseSection()
    {
        Assert.Equal("Escolha do idioma no instalador.",
            ReleaseNotes.FirstLine(Bilingual, LocalizationService.PortugueseBrazil));
    }

    [Fact]
    public void FirstLine_PicksEnglishSection()
    {
        Assert.Equal("Language choice in the installer.",
            ReleaseNotes.FirstLine(Bilingual, LocalizationService.EnglishUnitedStates));
    }

    [Fact]
    public void FirstLine_HandlesCrlf()
    {
        Assert.Equal("Language choice in the installer.",
            ReleaseNotes.FirstLine(Bilingual.Replace("\n", "\r\n"), LocalizationService.EnglishUnitedStates));
    }

    [Fact]
    public void FirstLine_LegacyBodyIsPortugueseOnly()
    {
        Assert.Equal("Interface em inglês (Estados Unidos).",
            ReleaseNotes.FirstLine(LegacyPortuguese, LocalizationService.PortugueseBrazil));
        Assert.Null(ReleaseNotes.FirstLine(LegacyPortuguese, LocalizationService.EnglishUnitedStates));
    }

    [Fact]
    public void FirstLine_EmptyBodyIsNull()
    {
        Assert.Null(ReleaseNotes.FirstLine("", LocalizationService.PortugueseBrazil));
        Assert.Null(ReleaseNotes.FirstLine(null, LocalizationService.EnglishUnitedStates));
    }

    /// <summary>
    /// 1.4.0 skips "#" lines and shows the first remaining one, so the bilingual body must
    /// open with the pt-BR section and keep its first item ahead of anything in English.
    /// </summary>
    [Fact]
    public void BilingualBody_StaysReadableByTheOneFourZeroParser()
    {
        string? legacy = null;
        foreach (var raw in Bilingual.Split('\n'))
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            legacy = trimmed.TrimStart('-', '*', ' ').Replace("**", "").Trim();
            break;
        }
        Assert.Equal("Escolha do idioma no instalador.", legacy);
    }
}
