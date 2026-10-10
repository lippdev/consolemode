# Console Mode guide

Details that don't fit in the [README](../README.md). 🇧🇷 [Guia em português](GUIDE.pt-BR.md)

## Modes and restore

| Mode | On exit |
|------|---------|
| **Steam Big Picture** | Automatic restore (app stays in the tray) |
| **Playnite fullscreen** | Automatic restore (app stays in the tray) |
| **Xbox mode** | Manual — use *Restore now*, the tray menu, or reopen the window |

You can also restore anytime from the tray (*Restore setup* / *Show window*). With black overlays, **ESC** dismisses the curtains.

The session menu has two ways out. **Back to the PC** ends the session and puts screens, resolution, HDR, audio and the FPS limit back as they were; the app stays in the tray. **Exit Console Mode** does the same and then closes the app, so the controller shortcuts stop until you open it again. The first time the menu opens, it offers a tour that explains each option; to see it later, press **Y** (△ on PlayStation) or **F1** in the menu, or use **Settings → Session menu tour**.

## Settings in Console mode

In **System → Controller**, choose the button layout: **Automatic**, **Xbox** or **PlayStation**. The preview and shortcut labels follow the choice, which is saved when you close the app. This changes the displayed symbols; the controller's inputs keep their existing mapping.

The **System** tab provides the same settings as desktop mode, including controller shortcuts, Playnite, Android TV, custom FPS and the data folder. Both interfaces share the saved values, which remain after restarting the app.

Open the shortcut action you want to configure, choose **Set** or **Change**, hold the desired combination on one controller and release the buttons. **Remove** disables that shortcut. During capture, controller buttons do not navigate the interface; use **Cancel** or **Esc** to stop capture. Outside capture, **B** leaves the editor. Address and custom FPS fields accept keyboard input.

When returning from Big Picture, **Send Steam to the tray when going back to the PC** closes the client window while keeping Steam running. With the option off, the desktop client is left open. Steam windows are left alone when a game is running or its state cannot be checked.

## Local control API

`consolemode://` links fire and forget. Tools that need an answer — a remote-control agent running as a Windows service, a Stream Deck plugin showing whether console mode is on — can use the named pipe `\\.\pipe\ConsoleMode.Control` while the app is running: send one JSON line, get one back.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` and `show` do exactly what the matching `consolemode://` link does, then reply once the app has settled (`ok:false` with an `error` if console mode didn't start or the restore didn't finish). `status` only reads. Only the signed-in user and LocalSystem can connect; nothing is exposed to the network.

## TV control

Settings → **TV** can turn the TV on and switch it to the PC's HDMI input when console mode starts, and optionally put it back in standby after the desk is restored. A TV that doesn't answer never blocks console mode: the app logs it and waits for the game screen as usual.

With the default, **Don't control it**, nothing is sent and nothing is added to starting or restoring. With a route selected, the TV step runs first when console mode starts and is capped at **30 seconds** (the worst case, when the TV doesn't answer and Wake-on-LAN is tried); the optional standby on restore is capped at **15 seconds**. A TV that is already on usually answers in about a second or two.

Most PC graphics cards can't send HDMI-CEC, so the app talks to the TV over the network instead:

### Google TV / Android TV

TCL, Sony, Hisense, Philips and other TVs running Google TV or Android TV, through ADB (the Android debugging protocol). Nothing to install on the PC.

1. On the TV: **Settings → System → About**, press **Android TV OS build** 7 times to unlock Developer options.
2. **Settings → System → Developer options**: turn on **USB debugging** (on some TVs, **Network debugging** / **ADB over network**).
3. In Console Mode, pick *Google TV / Android TV*, enter the TV's IP (Settings → Network on the TV; reserve it in your router) and the HDMI input the PC uses.
4. Press **Test now**. The TV asks "Allow debugging from this computer?": tick **Always allow** and press **Allow**.

The PC's ADB key is stored encrypted for your Windows user (DPAPI), so the TV only asks once. If the data folder is copied to another Windows user or PC, the key can't be read there: the app creates a new one and the TV asks "Allow debugging?" again.

Waking uses the Android wake-up key, then the **HDMI 1-4** key. If your TV ignores that key, set **Input command** to any Android shell command that opens the PC's input. If the TV drops off the network in standby, fill in its **MAC address** so the app sends Wake-on-LAN first (the TV's "Wake on network" / "Wake on Wi-Fi" option must be on).

"Wireless debugging" with a pairing code (Android 11+ phones) is a different, TLS-wrapped protocol and isn't supported: use USB / network debugging.

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
# Downloads rtss-cli, then builds both packages
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Output: `dist\ConsoleMode-Portable-x64.exe` and `dist\ConsoleMode-Setup-x64.exe` (the installer needs [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Open `ConsoleMode.sln` to debug.

To release, merge the release PR into `main`, then run Actions → **Release** → Run workflow on `main` with the version (like `1.4.0`, or `1.4.0-beta.2` for a pre-release) and **publish** ticked: the workflow creates the tag and the GitHub release, builds both files and publishes them, and the app picks them up as an update. Without "publish" it only builds and uploads the files as an artifact. Pushing a `v*` tag still works and does the same.

The previous PowerShell + WPF implementation (1.2 and earlier) lives on the [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) branch and is not used by the WinUI app.

