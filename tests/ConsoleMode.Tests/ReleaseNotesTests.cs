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
        // A body without language headings is the English-only format (beta.6+): everyone reads it.
        Assert.Equal("Interface em inglês (Estados Unidos).",
            ReleaseNotes.FirstLine(LegacyPortuguese, LocalizationService.EnglishUnitedStates));
    }

    [Fact]
    public void FirstLine_EnglishOnlyBodyIsSharedByEveryLanguage()
    {
        const string englishOnly = """
            ### What's new
            - **Session menu** over the game. (#46)

            ### Downloads
            - Installer

            ---
            Notas em português: CHANGELOG.md
            """;
        Assert.Equal("Session menu over the game. (#46)", ReleaseNotes.FirstLine(englishOnly, LocalizationService.PortugueseBrazil));
        Assert.Equal("Session menu over the game. (#46)", ReleaseNotes.FirstLine(englishOnly, LocalizationService.SpanishSpain));
    }

    private const string WithSummaries = """
        ### What's new
        - **Minimize to the system tray:** a new option in Settings. (#150)

        ---

        🇧🇷 Notas em português: [CHANGELOG.md](https://example.com/CHANGELOG.md)

        <!-- summary pt-BR: **Minimizar para a bandeja:** traduzida por [u/Skinner99](https://www.reddit.com/user/Skinner99/). (#150) -->
        """;

    [Fact]
    public void FirstLine_HiddenSummaryWinsForItsLanguage()
    {
        Assert.Equal("Minimizar para a bandeja: traduzida por u/Skinner99. (#150)",
            ReleaseNotes.FirstLine(WithSummaries, LocalizationService.PortugueseBrazil));
        Assert.Equal("Minimizar para a bandeja: traduzida por u/Skinner99. (#150)",
            ReleaseNotes.FirstLine(WithSummaries.Replace("\n", "\r\n"), LocalizationService.PortugueseBrazil));
    }

    [Fact]
    public void FirstLine_LanguagesWithoutASummaryReadTheEnglishNotes()
    {
        Assert.Equal("Minimize to the system tray: a new option in Settings. (#150)",
            ReleaseNotes.FirstLine(WithSummaries, LocalizationService.EnglishUnitedStates));
        Assert.Equal("Minimize to the system tray: a new option in Settings. (#150)",
            ReleaseNotes.FirstLine(WithSummaries, LocalizationService.GermanGermany));
    }

    [Fact]
    public void FirstLine_EmptyOrUnclosedSummaryFallsBackToTheNotes()
    {
        Assert.Equal("Item.", ReleaseNotes.FirstLine("- Item.\n<!-- summary pt-BR:   -->", LocalizationService.PortugueseBrazil));
        Assert.Equal("Item.", ReleaseNotes.FirstLine("- Item.\n<!-- summary pt-BR: sem fim", LocalizationService.PortugueseBrazil));
    }

    /// <summary>Clients up to 1.6.x show the first line they find; the summaries sit at the end so they keep showing the English item.</summary>
    [Fact]
    public void Summaries_StayAfterTheNotesForOlderClients()
    {
        var firstLine = WithSummaries.Split('\n').Select(l => l.Trim())
            .First(l => l.Length > 0 && !l.StartsWith('#') && l != "---");
        Assert.StartsWith("- **Minimize", firstLine);
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
