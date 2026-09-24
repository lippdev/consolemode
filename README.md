# Console Mode

Turn your Windows PC into a **game console** with one click: focus on your TV, hide extra monitors, switch audio, and launch your preferred fullscreen game UI.

🇧🇷 [Leia em português](README.pt-BR.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=version&color=brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

> [!NOTE]
> **More improvements are on the way.** I'm actively working on new features and fixes. The next ones are already in [1.5.0-beta.11](https://github.com/lippdev/consolemode/releases/tag/v1.5.0-beta.11). Your opinion shapes what comes next: use the **feedback button** in the app or [send feedback here](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). See [Feedback](#feedback).

<img src="assets/console-mode.gif" alt="Desk monitors turn off for a previously off HDMI TV; closing Big Picture restores the desk automatically." width="800">

Typical setup: two desk monitors and a distant HDMI TV that was off. Console Mode focuses the TV, turns the desk displays off, launches Steam Big Picture (or Xbox), and restores your desktop automatically when you quit Big Picture.

## Latest release: [1.5.0-beta.11](https://github.com/lippdev/consolemode/releases/tag/v1.5.0-beta.11)

Includes everything since 1.5.0-beta.6 (beta.7 to beta.11).

<img src="assets/whats-new.gif" alt="What's new in 1.5: the Select + Y session menu with horizontal tiles, the start-of-session toast, PlayStation pads, controller navigation in Settings, working updates, and the developer's other apps." width="800">

### What's new
- Session menu (Select + Y): horizontal tile layout, works with PlayStation pads, and a corner toast at the start of a session shows the combo. ([#64](https://github.com/lippdev/consolemode/pull/64))
- Settings → "More apps from the developer": WakeOn and Next Boost. ([#63](https://github.com/lippdev/consolemode/pull/63))
- Settings → "Test controller": shows live what Windows delivers from each pad (source, buttons by index, D-pad, sticks) and copies a diagnostic for the issue. The startup log lists the pads. For when a pad shows up but does nothing (DualSense with Steam running, for example).
- Local control API: while the app runs, the named pipe `\\.\pipe\ConsoleMode.Control` takes one JSON line (`status`, `start`, `stop`, `show`) and replies with the state (active, restoring, mode, version). Only the signed-in user and LocalSystem can connect. By @nextestudios. ([#24](https://github.com/lippdev/consolemode/pull/24))
- `ConsoleMode.exe --stop` restores the desk from the command line, like `consolemode://stop`. By @nextestudios. ([#23](https://github.com/lippdev/consolemode/pull/23))

### Fixes
- Select + Y (in-game menu) and Start + Back now work with PlayStation pads (DualSense, DualShock 4) without Steam Input, and with the Home button option off. ([#61](https://github.com/lippdev/consolemode/pull/61))
- Console Settings: controller navigation no longer skips "How to turn off the other screens" or draws the focus ring in the wrong place. ([#60](https://github.com/lippdev/consolemode/pull/60))
- Updates: the download no longer fails with "HttpClient.Timeout of 15 seconds elapsing" on slow connections; it is cancelled only after 30 s with no data. ([#58](https://github.com/lippdev/consolemode/pull/58))
- Desktop home screen: "Controller detected" is now a toast shown for a few seconds when a pad connects. ([#58](https://github.com/lippdev/consolemode/pull/58))
- Controller: a device that fails to read no longer silences the others; pads Windows exposes as a Gamepad with no XInput slot answering are now read (they used to be skipped); DualSense/DualShock over HID take the same path when XInput is empty.
- Console interface: the update notice now has a way to install, not just find. Before, the "Update now" button only existed on the Desktop screen; on Console, "Check now" would find the new version with no way to act on it. ([#49](https://github.com/lippdev/consolemode/pull/49))

Full history: [CHANGELOG.en-US.md](CHANGELOG.en-US.md) · 🇧🇷 Notas em português: [CHANGELOG.md](CHANGELOG.md)

## Features

- **One-click home screen**: pick the screen you play on, hit **Play now**
- **Desktop shortcut** (`ConsoleMode.exe --start`) that goes straight into console mode, with the app waiting in the tray
- Hide spare displays by **disconnect**, **black overlays**, or **DDC/CI**
- Optional **resolution & refresh rate** per monitor for console mode
- Launch **Steam Big Picture**, **Playnite fullscreen**, or **Xbox** (Win+F11)
- Optional **HDR** on the focus monitor and **VRR** (Windows setting)
- Optional global **FPS limit** via RivaTuner (RTSS)
- Audio routing, including “use output when connected” (e.g. TV HDMI)
- System tray icon to restore your desktop layout or reopen the app
- **Installer** (per-user, no admin) or a **portable single-file** `.exe` — both announce new versions from GitHub Releases
- Interface in **Brazilian Portuguese**, **English** or **Spanish**, following the Windows language by default and switchable in Settings without restarting. Adding a language is one JSON file ([#14](https://github.com/lippdev/consolemode/issues/14))
- Native **C# / WinUI 3** app (Windows App SDK), unpackaged and self-contained

## Why not just…?

- **Pick a monitor in Playnite / Big Picture settings?** That moves the game UI to the TV, but your desk monitors stay on, audio stays on the desk speakers, and you undo it by hand afterwards.
- **Use a display-switching add-on?** Those change the primary display. Console Mode also turns off or covers the other screens, routes audio to the TV, optionally sets resolution, HDR, VRR and an FPS cap, and puts everything back when you quit.
- **Win+P "Second screen only"?** Works for one fixed setup. Console Mode remembers which screen is the TV, launches Steam, Playnite or Xbox, and on a new screen asks "Can you see this screen?" and reverts on its own if nobody answers.

## Requirements

- Windows 10 or 11
- One of the launch modes you plan to use:
  - [Steam](https://store.steampowered.com/) (Big Picture — recommended)
  - [Playnite](https://playnite.link/) (fullscreen app)
  - Xbox / Game Bar on Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — optional, only if you want the FPS limit (usually via MSI Afterburner)

## How to use

1. Download from [Releases](https://github.com/lippdev/consolemode/releases):
   - **`ConsoleMode-Setup-x64.exe`** (recommended) — per-user install, no admin, Start menu entry, optional one-click desktop shortcut and start with Windows; data in `%LOCALAPPDATA%\ConsoleMode`
   - **`ConsoleMode-Portable-x64.exe`** — a single exe that keeps its data in `ConsoleMode_Data\` next to it
2. On first launch a short tour shows the screen map: pick the screen you play on (the others turn off)
3. From then on it is one click: **Play now**, or the **Console Mode 1 Click** desktop shortcut (Settings → Create shortcut). The first time with a new game screen, the TV asks "Can you see this screen?" and everything reverts on its own if nobody answers (mouse, keyboard or Xbox/PlayStation controller)
4. Or skip the PC altogether: with the app in the tray (turn on **Start with Windows**), **hold the Xbox button on the controller for 1 second** and console mode starts from the couch. **Hold Start + Select for 1 second** during a session to go back to the PC. **Hold Select + Y** during a session for a menu over the game: volume, resolution, audio output, FPS limit, HDR, back to the PC or exit (not over exclusive-fullscreen games). Xbox (XInput) controllers only for the Home button. In Settings you can switch to a **short press**: the app then turns off the controller shortcut for Game Bar in Windows (Win+G keeps working); if Steam is open, also turn off "Guide button focuses Steam" in Steam
5. Automation: `consolemode://start`, `consolemode://stop`, `consolemode://show` and `consolemode://menu` links work from Stream Deck, launchers, scripts or any shortcut. Tools that also need to know the state (a remote-control agent, a Stream Deck plugin) can use the local control API below
6. Two interfaces: the **Desktop** one (mouse, screen map) and the **Console** one (full screen, big cards, D-pad/stick + A/B, works with Xbox and PlayStation pads). By default the app picks Console whenever a controller is connected; change it in Settings → Interface or with the switch button on either screen
4. When you are done, exit Big Picture / Playnite (or restore manually in Xbox mode)
5. New versions are announced in the app from GitHub Releases (installed: updates silently; portable: swaps the exe)

> **Antivirus note:** some scanners may flag bundled helper tools. The source code is available in this repository for review.

## Build from source (Windows)

Requires [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows application development** workload, or the .NET 8 SDK plus the Windows App SDK.

```powershell
# Downloads MultiMonitorTool / SoundVolumeView / rtss-cli, then builds both packages
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Output: `dist\ConsoleMode-Portable-x64.exe` and `dist\ConsoleMode-Setup-x64.exe` (the installer needs [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Open `ConsoleMode.sln` to debug.

To release, push a tag like `v1.4.0` (or `v1.4.0-beta.2` for a pre-release): the `Release` workflow builds both files and publishes them, and the app picks them up as an update.

The previous PowerShell + WPF implementation (1.2 and earlier) lives on the [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) branch and is not used by the WinUI app.

## Modes and restore

| Mode | On exit |
|------|---------|
| **Steam Big Picture** | Automatic restore (app stays in the tray) |
| **Playnite fullscreen** | Automatic restore (app stays in the tray) |
| **Xbox mode** | Manual — use *Restore now*, the tray menu, or reopen the window |

You can also restore anytime from the tray (*Restore setup* / *Show window*). With black overlays, **ESC** dismisses the curtains.

## Local control API

`consolemode://` links fire and forget. Tools that need an answer — a remote-control agent running as a Windows service, a Stream Deck plugin showing whether console mode is on — can use the named pipe `\\.\pipe\ConsoleMode.Control` while the app is running: send one JSON line, get one back.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` and `show` do exactly what the matching `consolemode://` link does, then reply once the app has settled (`ok:false` with an `error` if console mode didn't start or the restore didn't finish). `status` only reads. Only the signed-in user and LocalSystem can connect; nothing is exposed to the network.

## Optional extras

### HDR

Enable HDR on the focus monitor while console mode is active. It is turned back off (or restored) when you exit.

### VRR

Console Mode can toggle the Windows VRR optimize setting. For best results, also enable VRR / G-SYNC / FreeSync in your **GPU control panel** (NVIDIA or AMD).

### FPS limit (RTSS)

Cap the global frame rate while console mode runs (helpful on a 60 Hz TV). Requires RTSS installed and running. The previous limit is restored when you exit.

## Limitations

- Multi-monitor layouts vary; on some setups restore may need a second try from the tray
- Xbox mode does not detect when fullscreen ends — restore manually
- Monitor and audio switching rely on bundled [NirSoft](https://www.nirsoft.net/) tools
- The FPS limit is global (RTSS limitation), not per display
- The WinUI 3 build currently requires Windows to compile (`net8.0-windows`)

## Troubleshooting

### Desktop layout did not restore

Open the tray menu and choose **Restore setup**. If the layout still looks wrong, choose **Restore setup** again after Windows finishes applying the monitor change. You can also reopen the window from the tray and restore manually.

### Audio stayed on the previous output

Check that the target output is connected and available in Windows before starting console mode. For HDMI/TV outputs, reconnecting the cable and starting the mode again may be necessary.

### The controller shows up but does nothing (DualSense / DualShock)

Open **Settings → Test controller**: it shows live what Windows delivers from each pad. If the reading stays empty while you press buttons, Steam is most likely capturing the pad (Steam running with PlayStation support in Steam Input turns it into keyboard/mouse on the desktop). Close Steam, or turn off PlayStation support in Steam Input, and test again. If the reading still shows nothing, use **Copy diagnostics** and paste it into the feedback form.

### HDR or VRR did not change

Confirm that the focus monitor supports the feature and that HDR is enabled in Windows. For VRR, also enable G-SYNC or FreeSync in the GPU control panel when applicable.

## Roadmap

<img src="assets/roadmap.gif" alt="Console Mode roadmap: Next, Later and Exploring columns." width="800">

Plans, not promises: the order can change with feedback. Vote with a 👍 on the issue, or [suggest something](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml).

**Next**
- 1.5.0 stable, wrapping up the betas
- FPS overlay, toggled from the session menu ([#66](https://github.com/lippdev/consolemode/issues/66))
- Record the last 30 seconds from the session menu
- Save and restore desktop icon positions ([#30](https://github.com/lippdev/consolemode/issues/30))

**Later**
- Per-game profiles: resolution, HDR and audio per game
- Controller battery in the session menu
- Publish to winget ([#13](https://github.com/lippdev/consolemode/issues/13))
- More interface languages ([#14](https://github.com/lippdev/consolemode/issues/14))

**Exploring**
- Windows Game Bar widget ([#11](https://github.com/lippdev/consolemode/issues/11))
- Voice and assistant triggers: Alexa, MCP ([#12](https://github.com/lippdev/consolemode/issues/12))
- WakeOn integration: wake the PC straight into console mode
- Phone remote over the local control API

## Feedback

Found a bug or have an idea? Feedback is what decides the next improvements, so every report counts.

- **From the app** (1.5.0-beta.1 and later): click the feedback button, next to **Settings** on the home screen. It opens a GitHub issue that already includes your app version, whether it's installed or portable, your Windows version and the interface language. Just describe what happened or what you'd like, attach screenshots if you want, and submit.
- **Without the app:** [open the feedback form](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml) directly.

You need a free GitHub account to submit. Nothing is sent automatically: no logs, no personal data, and you see the whole text before sending it. You can write in English or Portuguese.

If Console Mode saved you some monitor juggling, a ⭐ on the repo helps other couch gamers find it.

## License

Licensed under the [MIT License](LICENSE).

Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
