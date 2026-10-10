# Changelog

English (US) release notes, mirroring CHANGELOG.md (Brazilian Portuguese). Before publishing a version, add a `## [VERSION]` section to **both** files: the workflow publishes the section matching the tag from each one and fails if either is missing.

## [Unreleased]

### What's new
- The mouse pointer stays out of the way: during a session it disappears after 3 seconds without moving and comes back as soon as the mouse moves (handy for emulators that leave it in the middle of the TV); turn it off in Settings → "Hide the mouse pointer while playing". And for games and emulators whose menus ignore the controller, the session menu's "Control the mouse with the controller" row makes the left stick move the pointer, the right stick scroll, A click, A twice right-click and A held drag. It lasts until the session ends.

## [1.6.0]
### What's new
- Redesigned Console interface, made to feel like a console: a top bar with three tabs (Home, Session and System) you switch with LB/RB (L1/R1 on PlayStation), screen tiles that show what will happen to each monitor, quick settings with icons and a focus that grows as you move over it. Home brings the Play now banner and a summary of the current setup (game screen, audio, launcher and HDR/VRR). (#117)
- Session menu redesigned like Steam's overlay (Shift + Tab): it covers the whole screen, the game dims and blurs behind it, a side panel gathers the options (volume with a gauge, resolution, audio, FPS, HDR) and the **open windows** sit in the middle, an in-menu Alt + Tab: the D-pad picks, A brings the window to the front, X closes it (after a confirmation) and B goes back to the game. The cards show each program's icon. Outside a session it opens as a preview. (#120, #127, #133, #135)
- File explorer in the session menu: the "File explorer (ControlFS)" row opens ControlFS, the team's open-source, controller-first file manager, full screen on the session screen. If it isn't installed, A downloads and installs it right there, with the installer checked (SHA-256) before it runs. (#122, #136, #137)
- FPS counter over the game, through RivaTuner (RTSS): pick it in Settings or cycle it with A on the "FPS counter" row of the session menu. Styles: Afterburner (default; changes nothing), Off, Compact (FPS only), Detailed (API, FPS and frame time) and Custom, where you pick the items, whether they share one line and the number color. Except on "Afterburner", MSI Afterburner's OSD is hidden during the session and comes back by itself when you go back to the PC. (#138, #139)
- Controller shortcuts of your choice: the button that opens Console Mode, the session menu's and the one that goes back to the PC are set in Settings (hold the buttons you want and let go). None is on by default: the first time you open the app it shows the setup, with a suggestion for each action (Home, Select + Y and Start + Select). One button can't serve two actions. (#116)
- Settings → TV: console mode turns a Google TV / Android TV on over the network and switches to the PC's HDMI input; it can also put the TV in standby when you go back to the PC. (#126)
- Console interface background: by default, a collage of the covers of your installed Steam games under a dark gradient (the app only reads images Steam already downloaded). You can switch to just the gradient or to a picture of your own, in System → Background. (#117)
- Sounds in the Console interface when you move the focus, pick and go back, made by the app itself. Turn them off in Settings → Interface sounds. (#117)
- Console interface: the System tab reaches the desktop settings (Playnite, TV, FPS counter, Steam, the tutorial and the data folder), with grouped controller options, a button layout choice (Automatic, Xbox or PlayStation) with a preview, and a shortcut editor with cards and larger buttons. (#128, #131)

### Changes
- Displays and audio are now controlled through Windows' own APIs. The app no longer bundles NirSoft's MultiMonitorTool or SoundVolumeView: the package holds only the project's code and rtss-cli (MIT), which paves the way for a signed executable and fewer antivirus false positives. Saved screens, the chosen audio output and the desk backup keep working. (#91, #97, #98, #99, #110, #119)
- Steam: when you go back to the PC, Big Picture is closed and Steam's window goes to the tray, with the client still running. The request only goes to Steam's own windows, so games and other windows are left alone, and nothing happens while a game is running. Turn it off in Settings → "Send Steam to the tray when going back to the PC". (#121, #131, #140)
- Publishing a stable release also opens the update PR on winget (`winget install lippdev.ConsoleMode`). (#106)

### Fixes
- Displays: going back to the PC restores the exact layout from before (the second monitor no longer slides aside) and keeps the TV off. The result is checked against the backup and redone if needed. (#134)
- Displays: a monitor turned to portrait comes back in portrait when the desk is restored; before, the rotation was not saved. (#108)
- Displays: the primary screen comes from Windows' own flag, so cloned screens are no longer ambiguous. (#110)
- Playnite: a slow start no longer sends you back to the PC after a few seconds; the desk is only restored once Playnite actually closes. (#83)
- Audio: if switching the output fails, the session goes on instead of being rolled back after the screens were already switched. (#109)
- Controllers: a DualSense (PS5) over Bluetooth and a DualShock are read directly over HID by the shortcuts, the Console interface and the session menu. (#130, #132)
- Controllers: a controller that sends impossible readings (many buttons down at once, a stuck D-pad) is ignored. (#115)
- Console interface: controller navigation no longer gets stuck; up and down work across columns and, when you switch tabs, the focus lands on the first item of the new one. (#117)
- Session menu: the notice that shows the shortcut when a session starts no longer gets stuck on screen. (#114)

### Updating from 1.5.1
- The app shows the notice: click **Update now**. Your settings, saved screens and audio output are kept.
- Controller shortcuts have to be chosen again: if you used the Home button or Start + Select, confirm (or change) them in the setup the app shows the first time it opens.
- The old MultiMonitorTool and SoundVolumeView files stay in the `tools` folder until you delete them by hand; the app no longer uses them.

## [1.6.0-beta.8]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- FPS counter: the style saved in Settings now applies as soon as the session starts. Before, the counter only showed up after cycling the "FPS counter" row in the session menu. (#139)
- Back to the PC: the request to close Big Picture now only goes to Steam's own windows. Before, a full-screen game (SDL ones, like Valve's and many indies) could get it and close. (#140)

## [1.6.0-beta.7]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### What's new
- FPS counter over the game, through RivaTuner (RTSS), in Console Mode's style: pick it in Settings or cycle it with A on the new "FPS counter" row of the session menu. Styles: Afterburner (default; changes nothing), Off (nothing on screen), Compact (FPS only), Detailed (API, FPS and frame time) and Custom, where you pick the items, whether they share one line and the number color. Except on "Afterburner", MSI Afterburner's OSD is hidden during the session and comes back by itself when you go back to the PC; Afterburner's settings are not changed. (#138)

## [1.6.0-beta.6]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- Session menu: after installing ControlFS from the "File explorer" row, the menu stays in front and the row says "Installed · press to open"; ControlFS opens on the next A. Before, it opened by itself behind the game and the menu went away. (#137)

## [1.6.0-beta.5]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### What's new
- Session menu: without ControlFS installed, pressing A on "File explorer (ControlFS)" now downloads and installs ControlFS right there, without leaving the controller: the row shows the progress, the installer is checked (SHA-256) before it runs and, when done, ControlFS opens full screen. If it can't (no internet, for example), the row says so and the next A opens the download page. (#136)

## [1.6.0-beta.4]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- Session menu: the window cards are all the same size and show each program's right icon (Microsoft Store apps too), sharp on the TV; the close-window confirmation is now its own card with the window's icon and title, and clear "Cancel" and "Close window" buttons. (#135)

## [1.6.0-beta.3]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- Returning from Big Picture now closes only Steam's client window to the tray, keeping Steam running; games and other windows are preserved. (#131)
- Console interface settings: grouped controller options, a saved Automatic/Xbox/PlayStation button layout with a preview, and shortcut editing with cards and larger buttons. (#131)

## [1.6.0-beta.2]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha/beta. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- Session menu: ← and → move across the open-window cards by position without failing; nothing moves past the last card of a row. (#133)
- Controllers: a DualSense (PS5) over Bluetooth is read again by the shortcuts and the session menu; the log records the first report format of each Sony pad. (#132)

## [1.6.0-beta.1]
### Heads-up: beta version, not the stable one
- This is a test version of 1.6. The stable version is still 1.5.1, and people on it don't get this update automatically: only those who turned on Settings → "Receive test versions (alpha and beta)" or are already on an alpha. If something breaks, go back to 1.5.1 and report it with the feedback button (attach `consolemode.log`).

### Fixes
- Displays: going back to the PC restores the exact layout from before (the second monitor no longer slides aside) and keeps the TV off. Only the screens that were on come back, the TV is detached before the positions, and the result is checked against the backup and redone if needed. (#134)

## [1.6.0-alpha.5]

### Fixes
- Console screen: read DualShock input directly over HID; overlay: navigate from the sidebar to windows, remove the cards' X button and display the ControlFS logo. (#130)


## [1.6.0-alpha.4]
### What's new
- Settings → TV: console mode turns a Google TV / Android TV on over the network and switches to the PC's HDMI input; it can also put the TV in standby on restore. (#126)

### Fixes
- Console interface: the System tab now supports custom controller shortcuts and desktop settings, including Playnite, Android TV, custom FPS, closing Steam, the tutorial and the data folder. (#128)
- Session menu: closing a window through the internal Alt+Tab now asks for confirmation; Cancel keeps the window open and restores focus to its card. (#127)

## [1.6.0-alpha.3]
### What's new
- Session menu: a pinned panel at the top of the side column with the **File explorer (ControlFS)**, the team's open-source, controller-first file manager. One press opens ControlFS on top, always full screen on the session screen (or brings it to the front if already open); if it isn't installed, its download page opens. Console Mode only calls it through its own `--start` flag, never closes it, and never sends F11 to another window if Windows denies focus. (#122)
- Session menu (the controller shortcut, Select + Y in the suggestion) redesigned like Steam's overlay (Shift + Tab): it covers the whole screen, the game dims and blurs behind it, a side panel gathers the options (volume with a gauge, resolution, audio, FPS, HDR) and the **open windows** are always visible in the middle, an in-menu Alt + Tab: the D-pad picks, A brings the window to the front, X closes it and B goes back to the game (switching windows does not end the session). The frosted glass only shows when Windows' transparency effects are on (otherwise the panel is dark and solid, and it changes live if you flip the setting), and in high contrast it uses the system colours. It has a focus that grows, sounds and animations (the panel slides in, the rows follow one after another and closing fades it out). In this alpha it also opens **outside a session**, as a preview with a badge on top, so it can be tried without turning on console mode (in it the FPS limit is hidden and "Back to the PC" becomes "Close menu"). (#120)
- Redesigned Console interface, made to feel like a console: a top bar with three tabs (Home, Session and System) you switch with LB/RB (L1/R1 on PlayStation) with a slide animation, screen tiles that show what will happen to each monitor, quick settings with icons and a focus that grows as you move over it. Home brings the Play now banner and a summary of the current setup (game screen, audio, launcher and HDR/VRR), each opening its choices. (#117)
- Console background: by default, a collage of the covers of your installed Steam games under a dark gradient (the app only reads images Steam already downloaded; nothing is bundled). You can switch to just the gradient or to a picture of your own, in System → Background. (#117)
- Sounds in the Console interface when you move the focus, pick and go back (made by the app itself, at a low volume). Turn them off in Settings → Interface sounds. (#117)
- Controller shortcuts of your choice: the button that opens Console Mode, the session menu's and the one that goes back to the PC are now set in Settings (hold the buttons you want and let go on the same controller). Capture never combines buttons from different controllers. None is on by default: the first time you open this version the app shows the setup, with a suggestion for each action (Home, Select + Y and Start + Select). One button can't serve two actions. If you used the Home button or Start + Select before, choose them again. (#116)
- Settings → "Receive test versions (alpha and beta)": anyone can join the tests of upcoming versions from the app. Off by default; turning it on shows a warning that test versions can have bugs.

### Fixes
- Session menu: if Windows cannot activate the selected window, the menu stays open so you can try again. (#120)
- Steam: when you go back to the PC (button, menu, shortcut, `stop` link or leaving Big Picture) Big Picture is always closed and Steam is asked to quit the normal way, its own Exit, without killing the process. Before, it stayed open and was often left glitchy. It does not close Steam if a game is running, and you can turn it off in Settings → "Close Steam when going back to the PC". (#121)
- Session menu: the notice that shows the shortcut when a session starts no longer gets stuck on screen; it goes away by itself after a few seconds. (#114)
- Controllers: a controller that sends impossible readings through the generic HID driver (many buttons down at once, a stuck D-pad) is now ignored even when Windows also exposes it through Windows.Gaming.Input. The log says so, and reopening the app tries again. (#115)
- Console interface: controller navigation no longer gets stuck. Up (and down) also works when the target card is not in the same column, the focus no longer lands on invisible containers and, when you switch tabs, it lands on the first item of the new one. (#117)
- Displays: a monitor turned to portrait (rotated) keeps its rotation in the layout backup, so restoring the desk puts it back in portrait. Before, the rotation was never saved or restored. (#108)
- Playnite: a slow start no longer sends you back to the PC after a few seconds. When Playnite swaps its loading window for the main one, Console Mode now follows the new window and only restores the desk once Playnite actually closes. (#83)
- Audio: if switching the output fails, the session goes on instead of being rolled back after the screens were already switched. (#109)
- Displays: the primary screen comes from Windows' own flag, so cloned screens are no longer ambiguous. (#110)

### Behind the scenes
- Publishing a stable release now also opens the update PR on winget (`winget install lippdev.ConsoleMode`). Test versions are skipped. (#106)
- The code no longer mentions MultiMonitorTool or SoundVolumeView, and the cleanup of their old files in the `tools` folder is gone: anyone updating straight from 1.5 keeps those two files until they delete them by hand. (#119)
- The package embeds only rtss-cli; leftover MultiMonitorTool/SoundVolumeView files no longer end up in the executable. (#110)

## [1.6.0-alpha.2]
### Changes
- 1.6 is all the project's own code: the option to go back to MultiMonitorTool / SoundVolumeView is gone too, and their old files in the `tools` folder are deleted when the app opens. (#91)
- Displays and audio are controlled through Windows' own APIs since 1.6.0-alpha.1; see its notes below. (#97, #98, #99)

### Heads-up: alpha release
- Not verified on many monitor and TV setups yet. If something fails, go back to 1.5.0 and tell us with the feedback button (attach `consolemode.log`). People on the stable 1.5.0 don't get this release automatically.

## [1.6.0-alpha.1]
### What's new
- Displays and audio are now controlled through Windows' own APIs. The app no longer bundles NirSoft's MultiMonitorTool or SoundVolumeView: the package holds only the project's code and rtss-cli (MIT), which paves the way for a signed executable and fewer antivirus false positives. (#91, #97, #98, #99)
- Saved screens, the chosen audio output and the desk backup keep working: the identifiers are the same as before.

### Heads-up: alpha release
- This release is not verified on many monitor and TV setups yet. If turning the TV on, turning the other screens off or restoring the desk fails, go back to 1.5.0 and tell us what happened with the feedback button (attach `consolemode.log`).
- People on the stable 1.5.0 don't get this release automatically.

## [1.5.1]
### What's new
- Settings → "Receive test versions (alpha and beta)": join the 1.6 tests from the app. 1.6 controls displays and audio with its own code, without the NirSoft tools. Off by default; turning it on warns that test versions can have bugs.

## [1.5.0]
### What's new
- Console interface: a full-screen, controller-first home and Settings, driven by the D-pad/stick and A/B with Xbox and PlayStation pads. Automatic mode picks it whenever a controller is connected. (#41, #42, #47)
- Session menu over the game: hold Select + Y (Create + △ on PlayStation) for volume, resolution, audio output, FPS limit, HDR, "Back to the PC" and "Exit Console Mode", in a horizontal tile layout. A corner toast reminds you of the combo when the game starts. (#46, #64)
- Couch shortcuts: hold the Home button with the app in the tray to enter console mode, and Start + Select during a session to go back to the PC. They also work with DualSense and DualShock 4 over HID, without Steam Input. (#61)
- Automation: `consolemode://start`, `stop`, `show` and `menu` links, `ConsoleMode.exe --stop`, and a local control API over the named pipe `\.\pipe\ConsoleMode.Control`. By @nextestudios. (#23, #24)
- Settings → "Test controller" shows live what Windows reads from each pad and copies a diagnostic; "Enter when a controller connects"; a manual Playnite folder for portable installs; "More apps from the developer" (WakeOn, Next Boost). (#63)
- Feedback button that opens a prefilled GitHub issue, release notes in the interface language, and a Spanish interface. The installer asks for the language.

### Fixes
- Updates: the download no longer times out on slow connections, and the console interface can install an update, not just find it. (#49, #58)
- Controller navigation in the console Settings no longer skips rows or draws the focus ring in the wrong place, and the update banner buttons have a visible (black) focus ring. (#60, #73)
- Controllers: one failing pad no longer silences the others, and pads Windows lists late (PlayStation, Bluetooth) are picked up.
- Tray menu items work again, the app waits for the game screen before turning the others off, and the regular Steam window is no longer mistaken for Big Picture.

### Updating from 1.4.0
- The app shows the notice: click **Update now**. Your settings are kept.
- If the update from 1.4.0 fails with "HttpClient.Timeout of 15 seconds elapsing" (slow connection), download the installer below once. That bug is in 1.4.0's updater and is fixed from 1.5.0 on.

## [1.5.0-beta.11]
### What's new
- Session menu (Select + Y) has a horizontal layout: tiles side by side, with "Back to the PC" / "Exit Console Mode" below. On volume, A enters adjust mode (◀ ▶ changes it, □/X mutes, A or B leaves). (#64)
- A toast in the corner of the game screen a few seconds in says which combo opens the menu (Select + Y, or Create + △ on PlayStation). It never takes the focus from the game. (#64)
- Settings → "More apps from the developer": WakeOn (Wake-on-LAN automation) and Next Boost (Windows optimization). (#63)

### Fixes
- Session menu: with a PlayStation pad, the menu opened but the controller did nothing in it. The menu now reads the pad directly over HID, even while the game has the foreground. (#64)

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept. On beta.8 and seeing a timeout when updating? Download the installer below once.

## [1.5.0-beta.10]
### Fixes
- Select + Y (in-game menu) and Start + Back (back to the desk) now work with PlayStation pads (DualSense and DualShock 4, USB or Bluetooth) without Steam Input: the app reads the pad directly over HID. On a Sony pad: Create/Share + Triangle opens the menu; Options + Share goes back to the desk; the PS button counts as Home. (#61)
- These shortcuts also stopped working when "Open with the controller's Home button" was off. (#61)
- Settings in the console interface: moving with the controller skipped "How to turn off the other screens" and drew the focus ring in the wrong place. (#60)

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept. On beta.8 and seeing a timeout when updating? Download the installer below once.

## [1.5.0-beta.9]
### Fixes
- Updates: the download no longer fails with "HttpClient.Timeout of 15 seconds elapsing" on slow connections. It is now cancelled only after 30 s with no data. If you are on beta.8 and hit this error, download the installer below once; later updates from the app work. (#58)
- Desktop home screen: "Controller detected" is now a toast shown for a few seconds when a pad connects, instead of a permanent chip in the header. (#58)

### Beta release
- This beta is the main download. People on 1.4.0 or an earlier beta get the notice in the app: just click **Update now**. Your settings are kept.

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
