# Console Mode guide

Details that don't fit in the [README](../README.md).

English · [Português (Brasil)](GUIDE.pt-BR.md) · [Español](GUIDE.es-ES.md) · [Deutsch](GUIDE.de-DE.md) · [Français](GUIDE.fr-FR.md) · [Русский](GUIDE.ru-RU.md)

## Modes and restore

| Mode | On exit |
|------|---------|
| **Steam Big Picture** | Automatic restore (app stays in the tray) |
| **Playnite fullscreen** | Automatic restore (app stays in the tray) |
| **Xbox mode** | Manual — use *Restore now*, the tray menu, or reopen the window |
| **Custom app** | Automatic restore when the app (or the process you chose to watch) closes |

**Custom app** opens any `.exe` you pick (RetroArch, another launcher…) from its own folder, with optional arguments, and moves its window to the game screen. The session ends a few seconds after that process closes. If the exe only starts another program and exits (a launcher stub), type the real program's process name in **Process to watch**. If the watched process never shows up, the session stays until you end it (hold Start + Select, the session menu or the tray).

You can also restore anytime from the tray (*Restore setup* / *Show window*). With black overlays, **ESC** dismisses the curtains.

The session menu has two ways out. **Back to the PC** ends the session and puts screens, resolution, HDR, audio and the FPS limit back as they were; the app stays in the tray. **Exit Console Mode** does the same and then closes the app, so the controller shortcuts stop until you open it again. The first time the menu opens, it offers a tour that explains each option; to see it later, press **Y** (△ on PlayStation) or **F1** in the menu, or use **Settings → Session menu tour**.

## Settings in Console mode

**Home** puts your launchers in a row at the top (Steam Big Picture, Playnite, Xbox mode): **A** on one picks what opens, and **Play now** below starts it. The line under the name says where you will play and what changes (audio, resolution, FPS, HDR, VRR); the round button next to Play now opens **Session**, with the screens and quick settings.

The **System** tab lists its categories on the left (Gaming display, When entering console mode, Controller, TV, Optional extras, App, Shortcut, help, and data, More apps from the developer). Moving over a category shows its options on the right; **A** or right goes into them, and **B** or left goes back to the categories.

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

## Scripts, exact refresh rate and pointer

These three options live in the desktop **Settings**.

**Your own scripts.** *Settings → Your own scripts* takes one command that runs when a session starts and one that runs after the desk is back. A command is anything `cmd` can run: a `.bat` file, a program with arguments, or a PowerShell line such as `powershell -NoProfile -File "C:\Scripts\x.ps1"`. Put a path that has spaces in quotes. Both run hidden, and Console Mode waits up to 60 seconds for each; if one takes longer or fails, the session carries on and the log (`Script (início)` / `Script (fim)`) has the exit code. The start command runs once the session is registered, before the screens change. The stop command runs at the end of the restore, also when the start was rolled back or the restore had trouble, so what the start command took away (closing a browser, taking game mods out) can be given back. Nothing is shown on screen, so a script that waits for a key (`pause`) just runs into the 60-second limit.

**Exact refresh rate.** Many TVs run their 120 Hz mode at 119.88 Hz and their 60 Hz mode at 59.94 Hz. Windows' mode list only has whole numbers (and may list 119.88 as 119), so it can't be picked from the resolution list. With this option on, Console Mode sets the mode as usual and then asks for the same rate × 1000/1001 and reads it back. Only rates that have such a twin (24, 30, 48, 60, 120 and 240 Hz) are changed, and when the screen doesn't take it the whole-number rate stays and the log says so (`Taxa NTSC`).

**Move the mouse pointer out of the way.** Once the launcher is up, the pointer is moved to the bottom-right corner of the game screen, twice (Steam and others recenter it while their window opens). Xbox mode has no window to watch, so it goes by the time since the launch.

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

