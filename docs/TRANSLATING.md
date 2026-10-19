# Translating Console Mode

Console Mode speaks English, Português (Brasil), Español, Deutsch, Français and Русский. Thanks for helping it speak yours, or for fixing a line that reads wrong in your language.

You don't need Windows or a build environment: every text the app shows lives in one JSON file per language, and the tests that check it run on any OS.

## Fix or improve an existing translation

1. Open `src/ConsoleMode/Resources/Strings.<code>.json` for your language (for example `Strings.de-DE.json`).
2. Change the **values**, never the keys. To find a text, search for it in `Strings.en-US.json`, then look up the same key in your file.
3. Open a pull request. If you'd rather not touch Git, [open a feedback issue](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml) with the key (or a screenshot), the current text and your suggestion.

## Add a new language

Copy `src/ConsoleMode/Resources/Strings.en-US.json` to `Strings.<code>.json` (a culture code like `it-IT`, `pl-PL`, `zh-CN`) and translate every value. That file is the real work; the rest is wiring, and a maintainer can do it for you if you only send the JSON.

The wiring, in the same pull request:

1. `src/ConsoleMode/ConsoleMode.csproj` and `tests/ConsoleMode.Tests/ConsoleMode.Tests.csproj`: add an `<EmbeddedResource>` line for the new file, next to the existing ones.
2. `src/ConsoleMode/Services/LocalizationService.cs`: add a constant for the code and list it in `SupportedLanguages` with the name the language uses for itself (`"Italiano"`, `"Polski"`, `"简体中文"`). The language picker reads that list, and a Windows set to any region of the language (`it-CH`, `zh-SG`) opens the app in it.
3. `tests/ConsoleMode.Tests/LocalizationTests.cs`: add an `[InlineData]` row to `Initial_language_follows_saved_choice_then_installer_then_windows` that maps another region of your language to your code. If your language is the one the test uses as "unknown" (`ja-JP` today), switch that case to another language the app doesn't have.
4. `build/ConsoleMode.iss` (the installer): a `[Languages]` entry with Inno Setup's messages file for your language, your translation of the three `[CustomMessages]` lines (`OneClickShortcut`, `StartWithWindows`, `LaunchApp`), and a `[Registry]` line that saves your code. Keep the file saved as UTF-8 with BOM, or non-Latin text shows up broken in the installer.
5. `README.md` / `README.pt-BR.md`: add the language to the features list. Add the changelog entry under `## [Unreleased]` in both `CHANGELOG.md` and `CHANGELOG.en-US.md`.
6. `docs/TESTING.md`: a checklist line to walk through the app and the installer in the new language on Windows, looking for cut or overlapping text.

A translated `README.<code>.md` and `docs/GUIDE.<code>.md` are welcome too, in a separate pull request. Follow the existing ones (`README.es-ES.md`, `docs/GUIDE.es-ES.md`): they leave out the version-specific parts so they don't go stale on every release.

## Writing good strings

- **Keep `{0}`, `{1}`…** exactly as they are; they are replaced by numbers, names or times. You can move them inside the sentence.
- **Keep it short.** Much of the app is read from the couch on a TV and laid out in cards and menu rows; a text much longer than the English may be cut. If your language runs long, prefer the shorter wording.
- **Talk to the player** the way games and consoles in your language do (in most languages, informally). Keep one tone across the whole file.
- **Use the official names** of things outside the app as they appear in your language: Windows settings, Steam, Big Picture, Xbox, PlayStation buttons (✕ ○ □ △), HDR, VRR.
- **Don't translate** product names: Console Mode, Steam Big Picture, Playnite, RTSS, ControlFS, the `Console Mode 1 Click` shortcut.

## Checking your work

```sh
dotnet test tests/ConsoleMode.Tests/ConsoleMode.Tests.csproj
```

With the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) this runs on Windows, Linux or macOS. The tests fail when your file is missing a key or has an extra one, when a `{0}` doesn't match the English, or when a sentence of three words or more is still identical to the English. Single words that are the same in your language (`Volume`, `Desktop`) are fine.

If you can, also run the app on Windows in your language and look at the Home screen, Settings, the session menu and the tutorial, in both the Desktop and the Console interface. If you can't, say so in the pull request and someone else will check it.
