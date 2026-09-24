# Changelog

English (US) release notes, mirroring CHANGELOG.md (Brazilian Portuguese). Before publishing a version, add a `## [VERSION]` section to **both** files: the workflow publishes the section matching the tag from each one and fails if either is missing.

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
- This is a test release. People on 1.4.0 don't get a notice for it: to try it, download the installer or the portable version below. From then on, the app notifies you about the next betas and about the final 1.5.0.

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
