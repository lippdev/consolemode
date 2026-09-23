# Legacy PowerShell

This folder archives the original PowerShell + WPF app (Console Mode 1.2 and earlier). It is kept only as a reference; the current app lives in `src/ConsoleMode` (C# / WinUI 3). Do not add new features here.

| File | What it is |
|---|---|
| `ConsoleMode.ps1`, `lib\` | The old app |
| `IniciarConsoleMode.bat` | Launches `ConsoleMode.ps1` with the right execution policy |
| `Build-ConsoleMode.ps1` | Builds the old single `ConsoleMode.exe` with ps2exe into `legacy\dist\` (`-TestDev` only checks paths) |
| `SoundVolumeView.cfg` | SoundVolumeView window settings the old app left behind |

To run it, put `MultiMonitorTool.exe` and `SoundVolumeView.exe` in this folder (or let `Build-ConsoleMode.ps1` download them) and start `IniciarConsoleMode.bat`. Its data goes to `legacy\ConsoleMode_Data\`.
