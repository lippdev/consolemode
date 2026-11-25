# Fork notes

This is a fork of [lippdev/consolemode](https://github.com/lippdev/consolemode). The dojo's
`keeper` agent (see `1r0n51uv/ron1n-dojo`, FLOW-013) reads this file every time upstream publishes a
release, merges it here and opens a PR from `sync/vX`. Keep it current: it is what the agent knows
about what we changed.

## What we changed

- **Italian**: `src/ConsoleMode/Resources/Strings.it-IT.json` (a catalog with every key of
  `Strings.en-US.json`), its wiring (`ConsoleMode.csproj`, `Tests.csproj`, `LocalizationService.cs`,
  `LocalizationTests.cs`, `build/ConsoleMode.iss`), `README.it-IT.md`, the language line of the other
  READMEs, `docs/TESTING.md` and `docs/TRANSLATING.md`.
- **Updates and links point at the fork**: `UpdateService.Repository`, the changelog link in
  `release.yml`, the installer URLs in `build/ConsoleMode.iss`. Never take upstream's value here.
- **Fork automation**: `.github/workflows/fork-sync.yml`, `.github/scripts/fork-sync.sh`, this file.
  `winget.yml`/`release.yml`'s winget job stay off (no `WINGET_TOKEN`).

## Rules for a sync

1. Branch `sync/v<upstream version>` from `main`, then merge the upstream **tag** (`git merge v<X>`),
   never a squash: the history must keep upstream's commits.
2. On a conflict, keep upstream's code and re-apply our changes above on top.
3. Every key upstream added to `Strings.en-US.json` goes into `Strings.it-IT.json`, translated
   (the localization tests fail on a missing key, a wrong `{0}` or an untranslated sentence). Run
   `dotnet test tests/ConsoleMode.Tests/ConsoleMode.Tests.csproj` (in a `mcr.microsoft.com/dotnet/sdk:8.0`
   container when `dotnet` is missing).
4. **Version**: the fork's version is the higher of upstream's and ours; if ours is higher or equal,
   ours +1 patch. Set it in `src/ConsoleMode/ConsoleMode.csproj`: `Version`, `AssemblyVersion`,
   `FileVersion` and `InformationalVersion`.
5. **Changelog**: a `## [<version>]` section in **both** `CHANGELOG.md` (Portuguese) and
   `CHANGELOG.en-US.md`; the release fails without them. Keep upstream's sections, add ours on top.
6. Anything beyond the Italian catalog, `README.it-IT.md`, the changelogs and the version lines
   makes the PR "not clean": it waits for the owner (`needs-review`).
7. Never push to `main`, never merge: `fork-sync.yml` decides, with fixed rules, after the CI is green.
