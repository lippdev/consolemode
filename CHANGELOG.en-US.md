# Changelog

English (US) release notes, mirroring CHANGELOG.md (Brazilian Portuguese). Before publishing a version, add a `## [VERSION]` section to **both** files: the workflow publishes the section matching the tag from each one and fails if either is missing.

## [1.5.0-beta.8]
### Fixes
- Desktop home screen: the "press A for console mode" hint is capped in width with an ellipsis and shows the full text on hover; long translations no longer stretch the header. (#56)

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept.

## [1.5.0-beta.7]
### What's new
- Settings → "Test controller": shows live what Windows delivers from each pad (source, buttons by index, D-pad, sticks) and copies a diagnostic for the issue. The startup log lists the pads. For when a pad shows up but does nothing (DualSense with Steam running, for example).
- Local control API: while the app runs, the named pipe `\\.\pipe\ConsoleMode.Control` takes one JSON line (`status`, `start`, `stop`, `show`) and replies with the state (active, restoring, mode, version). Only the signed-in user and LocalSystem can connect. By @nextestudios. (#24)
- `ConsoleMode.exe --stop` restores the desk from the command line, like `consolemode://stop`. By @nextestudios. (#23)

### Fixes
- Controller: a device that fails to read no longer silences the others; pads Windows exposes as a Gamepad with no XInput slot answering are now read (they used to be skipped); DualSense/DualShock over HID take the same path when XInput is empty.
- Console interface: the update notice now has a way to install, not just find. Before, the "Update now" button only existed on the Desktop screen; on Console, "Check now" would find the new version with no way to act on it. (#49)

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept.

## [1.5.0-beta.6]
### What's new
- Session menu: while the game is open, hold Select + Y for a menu over the game with volume, resolution, audio output, FPS limit, HDR, "Back to the PC" and "Exit Console Mode". Also via `consolemode://menu`. (#46)
- Desktop interface navigable with the controller: D-pad/stick move, A activates (lists, switches, cards), B closes a list or leaves Settings, Start toggles Settings, Y goes to the console interface; in the tour, A advances and B skips. Based on PR #17 by @nextestudios. (#47)

### Fixes
- The controller went dead for good if a pad disconnected mid-read; the read now just skips that sample. Also from PR #17. (#47)
- Controller navigation: the first press only reveals the focus ring; in scrolled lists focus falls back to tab order when the spatial search finds nothing; D-pad diagonals count as vertical; reads only count while the window is in the foreground (checked per window, not per event). (#47)

### Beta release
- Pre-release: people on any 1.5.0 beta get the notice in the app. People on 1.4.0 don't; to try it, download the files below.

## [1.5.0-beta.5]
### What's new
- Console interface: list settings (launcher, audio, resolution, FPS, hide strategy, language, interface) open a picker with every option, focused on the current one; A picks, B goes back. Before, you had to keep pressing A to cycle. (#42)
- In the desktop interface, with a controller detected, a "press A for console mode" hint appears; A or Start switch the interface. (#41)
- Spanish interface. With no saved choice, the app follows the Windows language (Portuguese, Spanish or English). Adding a language is now just a `Strings.<code>.json` file. (#40)

### Fixes
- Automatic mode missed controllers that Windows lists late (PlayStation, Bluetooth) and opened in Desktop. The app now re-detects when a controller appears and also 1.5 s and 4 s after opening.
- In the console interface the controller sometimes didn't respond until you changed screens: the listener didn't start the first time.

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept.

## [1.5.0-beta.4]
### What's new
- "Enter when a controller connects" option (off by default): with the app in the tray, turning on or plugging in a controller enters console mode. Ignores the first 15 s after opening and 30 s after restoring.

### Beta release
- Pre-release: people on any 1.5.0 beta get the notice in the app. People on 1.4.0 don't; to try it, download the files below.

## [1.5.0-beta.3]
### What's new
- Settings → "Playnite folder": pick Playnite.FullscreenApp.exe by hand for portable installs, which auto-detection can't find.

### Fixes
- The language chosen in the installer now applies to the app. With no saved choice, the app follows the Windows language (Portuguese → pt-BR; anything else → English) instead of always opening in Portuguese.

### Beta release
- Pre-release: people on 1.5.0-beta.1 or beta.2 get the notice in the app. People on 1.4.0 don't; to try it, download the files below.

## [1.5.0-beta.2]
### What's new
- Holding the controller's Xbox button for 1 second while the app is in the tray enters console mode without touching the PC. During a session, holding Start + Select for 1 second goes back to the PC. Xbox (XInput) controllers only; it can be turned off in Settings.
- "Short press of the Xbox button" option: the app turns off the controller shortcut for Game Bar and warns when Steam is also using the button.
- Console interface: big full-screen cards driven by the controller (D-pad or stick, A, B, Start, Y) or the keyboard, with quick settings that cycle on A. Settings → Interface picks Automatic (Console whenever a controller is connected), Desktop or Console; an on-screen button switches right away.
- `consolemode://start`, `consolemode://stop` and `consolemode://show` links for automation (Stream Deck, launchers, scripts).

### Beta release
- Pre-release: people on 1.5.0-beta.1 get the notice in the app. People on 1.4.0 don't; to try it, download the files below.

## [1.5.0-beta.1]
### What's new
- Feedback button on the home screen: it opens a GitHub issue prefilled with the app and Windows version, to send suggestions and report bugs.
- The new-version notice shows the release notes in the interface language.
- The installer asks for the language, preselecting the Windows one. Updates made from the app keep the previous choice.

### Fixes
- The tray icon menu items work again.
- The app waits for the game screen to turn on before turning the others off.
- The regular Steam window is no longer mistaken for Big Picture.
- The summary details on the home screen wrap instead of being clipped.
- The one-click shortcut is now named "Console Mode 1 Click".

### Beta release
- This beta is the main download. People on 1.4.0 get the notice in the app: just click **Update now**. Your settings are kept.
- After updating, the app also notifies you about the next betas, in addition to the final 1.5.0.

## [1.4.0]
### What's new
- English (United States) interface, in addition to Brazilian Portuguese. Pick the language in Settings; it switches right away, without restarting the app.

### Fixes
- Resolution names ("Don't change", "(cached)", "(estimated)") and the On/Off buttons now follow the chosen language.
- Cached resolutions no longer lose their "(cached)" label when listed alongside estimated ones.
- The new-version notice shows the first item of the release notes instead of the "What's new" heading.

### How to update
- If you use 1.3.0, you get the notice inside the app: just click **Update now**. The installed version updates itself, and the portable one replaces its own file and reopens.
- Your settings are kept. The language stays Portuguese until you pick **English** in Settings.

## [1.3.0]
### What's new
- Native interface to choose which display to play on and what to do with the others.
- Displays are arranged by their actual position on the desktop.
- Controller navigation, a first-run tutorial, and a confirmation step to help avoid ending up with no picture.
- Settings for resolution, refresh rate, audio, HDR, VRR, and frame rate limit.
- Per-user installer that doesn't require administrator rights, plus a portable version.
- New-version notices from GitHub, with updates for both the installed and portable versions.
