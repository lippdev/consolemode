# Console Mode guide

Details that don't fit in the [README](../README.md). 🇧🇷 [Guia em português](GUIDE.pt-BR.md)

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

## Build from source (Windows)

Requires [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows application development** workload, or the .NET 8 SDK plus the Windows App SDK.

```powershell
# Downloads MultiMonitorTool / SoundVolumeView / rtss-cli, then builds both packages
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Output: `dist\ConsoleMode-Portable-x64.exe` and `dist\ConsoleMode-Setup-x64.exe` (the installer needs [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Open `ConsoleMode.sln` to debug.

To release, push a tag like `v1.4.0` (or `v1.4.0-beta.2` for a pre-release): the `Release` workflow builds both files and publishes them, and the app picks them up as an update.

The previous PowerShell + WPF implementation (1.2 and earlier) lives on the [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) branch and is not used by the WinUI app.

