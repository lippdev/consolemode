# Console Mode: notes for coding agents

Console Mode is a Windows desktop app (WinUI 3, .NET 8, C#) that turns a PC into a
console: it focuses the TV, hides the other displays, launches Big Picture / Playnite /
Xbox mode, and restores the desk setup afterwards.

## What you can and cannot verify

- **The app only builds on Windows.** `src/ConsoleMode` targets `net8.0-windows` with the
  Windows App SDK. On Linux (Pullfrog, most cloud agents) do not try to build it; the
  **CI** workflow builds it on Windows for every pull request. Say in the PR that the
  build was left to CI.
- **The unit tests run anywhere:**
  `dotnet test tests/ConsoleMode.Tests/ConsoleMode.Tests.csproj`.
  The test project targets plain `net8.0` and compiles a list of pure-logic files from
  `src/ConsoleMode` (see its `.csproj`). Run it before every push. When you add logic
  that doesn't touch Win32/WinUI, put it in such a file, link it there and test it.
- **Displays, TVs, audio, controllers, windows of other apps** can't be tested in CI.
  For changes there, add the manual checks to `docs/TESTING.md` (Portuguese, checklist
  style) instead of claiming they work.

## Layout

- `src/ConsoleMode/Services`: engine and services (`ConsoleEngine`, `MonitorService`,
  `LaunchService`, controllers, updates, localization).
- `src/ConsoleMode/Native`: P/Invoke (Win32, CCD display config).
- `src/ConsoleMode/Views`, `ViewModels`, `Controls`: UI (MVVM with CommunityToolkit.Mvvm).
- `build/`: publish script and Inno Setup installer. `.github/workflows/release.yml`
  publishes a release when a `v*` tag is pushed. **Never create or push tags.**

## Conventions

- **Every user-facing string** goes in all three catalogs:
  `src/ConsoleMode/Resources/Strings.pt-BR.json`, `Strings.en-US.json`, `Strings.es-ES.json`.
  The localization tests fail when keys don't match.
- **Changelog**: add user-visible changes under `## [Unreleased]` in both
  `CHANGELOG.md` (Portuguese) and `CHANGELOG.en-US.md`, ending the item with the PR number.
- **Docs come in pairs**: `README.md` / `README.pt-BR.md`, `docs/GUIDE.md` / `docs/GUIDE.pt-BR.md`.
- **Commits and PR titles**: Conventional Commits in English (`fix: …`, `feat: …`, `docs: …`),
  one topic per PR. `AppLog` messages are written in Portuguese, like the existing ones.
- Keep changes small and focused; don't reformat unrelated code.
