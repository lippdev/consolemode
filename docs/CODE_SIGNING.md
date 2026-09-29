# Code signing policy

> **Status:** application to the SignPath Foundation open source program is pending.
> Until it is approved, releases are not signed and this policy describes how they will be.

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## What gets signed

Only binaries built from this repository's source by the [Release workflow](../.github/workflows/release.yml)
on GitHub Actions, from a `v*` tag:

- `ConsoleMode-Setup-x64.exe` (installer)
- `ConsoleMode-Portable-x64.exe` (portable)

Nothing built on a developer machine is signed.

## Team roles

| Role | Members |
|------|---------|
| Committers and reviewers | [Filipe Moreira (@lippdev)](https://github.com/lippdev) |
| Approvers | [Filipe Moreira (@lippdev)](https://github.com/lippdev) |

Pull requests from people outside this list, and from automated agents, are reviewed by a committer
before merge, and every pull request must pass the CI build. Each signing request is approved
manually by an approver. All members use multi-factor authentication on GitHub and SignPath.

## Privacy

Console Mode does not collect or send telemetry. It connects to the internet only to:

- check for new versions through the public GitHub Releases API (`api.github.com`), without
  sending any personal data, and download an update when the user accepts it;
- open pages in the user's browser when the user asks for it (the feedback form on GitHub,
  the releases page, links to other apps).

Settings, the display backup and the log stay on the user's PC, in `%LOCALAPPDATA%\ConsoleMode`
(installer) or `ConsoleMode_Data` next to the executable (portable). The installer version can be
removed from Windows Settings → Apps; the portable version by deleting its file and folder.
