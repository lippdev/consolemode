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

### USB-CEC adapter (Pulse-Eight)

Works with **any TV with HDMI-CEC** (Samsung Anynet+, Sony Bravia Sync, LG SimpLink…), with no network setup, through a [Pulse-Eight USB-CEC adapter](https://www.pulse-eight.com/p/104/usb-hdmi-cec-adapter) placed on the HDMI cable between the PC and the TV.

1. Install **libCEC** from Pulse-Eight; it brings `cec-client.exe`. This route is **optional** and Console Mode does **not** ship `cec-client`: the app looks for it, in this order, at the path set in Settings (a folder or the `.exe`), next to `ConsoleMode.exe`, in `Program Files (x86)\Pulse-Eight\USB-CEC Adapter` / `Program Files`, then on the PATH. Settings → TV shows which one it found, or says "cec-client.exe not found" and where to install libCEC; starting console mode with no `cec-client` only logs the error and carries on.
2. Turn CEC on in the TV's settings.
3. In Console Mode, pick *USB-CEC adapter* and the TV's HDMI input the PC is on. Press **Test now**.

On start, the app runs `cec-client -s -t p -p <input>` with `on 0` (power on) and then `as` (Active Source, so the TV switches to that input); on restore, `standby 0`. Each command takes a few seconds while the adapter opens and is capped at 15 seconds: a `cec-client` that hangs is killed, so no stray process is left holding the adapter. No text from your settings is put on its command line (only fixed flags and the HDMI port, 1-4).

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

To release, push a tag like `v1.4.0` (or `v1.4.0-beta.2` for a pre-release): the `Release` workflow builds both files and publishes them, and the app picks them up as an update.

The previous PowerShell + WPF implementation (1.2 and earlier) lives on the [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) branch and is not used by the WinUI app.

