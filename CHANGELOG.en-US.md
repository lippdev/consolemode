# Changelog

English (US) release notes, mirroring CHANGELOG.md (Brazilian Portuguese). Before publishing a version, add a `## [VERSION]` section to **both** files: the workflow publishes the section matching the tag from each one and fails if either is missing.

## [Unreleased]
### What's new
- Settings → "Playnite folder": pick Playnite.FullscreenApp.exe by hand for portable installs, which auto-detection can't find.

### Fixes
- The language chosen in the installer now applies to the app. With no saved choice, the app follows the Windows language (Portuguese → pt-BR; anything else → English) instead of always opening in Portuguese.

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
