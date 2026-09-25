# Console Mode

Turn your Windows PC into a **game console** with one click: focus the TV, turn off the other screens, switch the audio and launch your game UI. Quit the game and your desk comes back on its own.

🇧🇷 [Leia em português](README.pt-BR.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=version&color=brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

<img src="assets/console-mode.gif" alt="Desk monitors turn off for a previously off HDMI TV; closing Big Picture restores the desk automatically." width="800">

## Download

Get **[1.5.0](https://github.com/lippdev/consolemode/releases/tag/v1.5.0)**, the latest stable version:

- **`ConsoleMode-Setup-x64.exe`** (recommended): per-user install, no admin, updates itself.
- **`ConsoleMode-Portable-x64.exe`**: a single exe that keeps its data next to it.

On 1.4.0? The app offers the update. If it fails with a timeout, download the installer once; your settings are kept.

Windows 10 or 11, plus [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) or the Xbox app. [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) is optional, for the FPS limit.

## How it works

1. Open the app and pick the screen you play on. The others turn off.
2. Press **Play now**, or use the one-click desktop shortcut. The first time on a new screen, the TV asks "Can you see this screen?" and reverts on its own if nobody answers.
3. Quit Big Picture or Playnite and everything goes back: screens, audio, resolution.

Prefer the couch? With the app in the tray, hold the controller's Home button and it starts from there.

| Controller shortcut | Does |
|---|---|
| Hold **Home** (Xbox / PS) | Enter console mode from the tray |
| Hold **Select + Y** (Create + △) | Menu over the game: volume, resolution, audio, FPS, HDR, exit |
| Hold **Start + Select** (Options + Create) | Back to the PC |

## Features

- Hide the other screens by **disconnecting** them, **black overlays** or **DDC/CI**
- Per-screen **resolution and refresh rate**, **HDR**, **VRR** and an **FPS limit** (RTSS) while you play
- Audio goes to the TV (or any output) and comes back afterwards
- Launches **Steam Big Picture**, **Playnite fullscreen** or **Xbox** mode
- Two interfaces: **Desktop** (mouse) and **Console** (full screen, Xbox and PlayStation pads)
- Automation with `consolemode://start` / `stop` / `menu` links (Stream Deck, scripts) and a [local control API](docs/GUIDE.md#local-control-api)
- English, Portuguese and Spanish

## What's new in 1.5.0

- **Console interface:** full-screen home and Settings made for the controller, with Xbox and PlayStation pads
- **Select + Y menu** over the game: volume, resolution, audio, FPS, HDR and the way out, in horizontal tiles
- **Couch shortcuts** that also work with DualSense and DualShock 4, no Steam Input needed
- **Automation:** `consolemode://` links, `--stop` and a local control API
- Controller test in Settings, feedback button, Spanish interface, and updates that install from the couch

All the details in the [release notes](https://github.com/lippdev/consolemode/releases/tag/v1.5.0) and the [changelog](CHANGELOG.en-US.md) · [notas em português](CHANGELOG.md).

## Roadmap

**Next:** FPS overlay ([#66](https://github.com/lippdev/consolemode/issues/66)), record the last 30 s ([#77](https://github.com/lippdev/consolemode/issues/77)), desktop icon positions ([#30](https://github.com/lippdev/consolemode/issues/30)) · **Later:** per-game profiles ([#69](https://github.com/lippdev/consolemode/issues/69)), winget ([#13](https://github.com/lippdev/consolemode/issues/13)), more languages ([#14](https://github.com/lippdev/consolemode/issues/14)) · **Exploring:** HDMI-CEC ([#75](https://github.com/lippdev/consolemode/issues/75)), Linux and macOS ([#74](https://github.com/lippdev/consolemode/issues/74)), [and more](https://github.com/lippdev/consolemode/issues?q=is%3Aopen+label%3Aroadmap).

## More

- [Guide](docs/GUIDE.md): modes and restore, control API, HDR/VRR/FPS, troubleshooting, building from source
- **Feedback:** the button next to Settings in the app, or [this form](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). A ⭐ helps other couch gamers find the project.
- Antivirus may flag the bundled helper tools; the source is all here.

MIT License · [Third-party notices](THIRD_PARTY_NOTICES.md) · [Code signing policy](docs/CODE_SIGNING.md)
