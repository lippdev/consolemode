; Console Mode — per-user installer (no admin). Built by build\Publish-ConsoleMode.ps1:
;   ISCC /DAppVersion=1.3.0 /DSourceDir=<publish folder> /DOutputDir=<dist> /DArch=x64 ConsoleMode.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir (the dotnet publish folder) is required
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

#define AppName "Console Mode"
#define AppExe "ConsoleMode.exe"

[Setup]
; Keep this AppId forever: it is how updates find the existing install.
AppId={{6C1C3E8B-4F0E-4E0B-9C62-3A7E1D0C5B21}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=lippdev
AppPublisherURL=https://github.com/lippdev/consolemode
AppSupportURL=https://github.com/lippdev/consolemode/issues
AppUpdatesURL=https://github.com/lippdev/consolemode/releases
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=ConsoleMode-Setup-{#Arch}
SetupIconFile=..\assets\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Always ask for the installer language, preselecting the Windows display language.
; Silent in-app updates skip the dialog and reuse the language of the previous install.
ShowLanguageDialog=yes
LanguageDetectionMethod=uilanguage
UsePreviousLanguage=yes
; Updates run silently from inside the app; close it through Restart Manager if it is still up.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
ptbr.OneClickShortcut=Atalho "Console Mode 1 Click" na Área de Trabalho (entra no modo console com 1 clique)
en.OneClickShortcut="Console Mode 1 Click" desktop shortcut (enters console mode in one click)
ptbr.StartWithWindows=Iniciar com o Windows (fica na bandeja)
en.StartWithWindows=Start with Windows (stays in the tray)
ptbr.LaunchApp=Abrir o Console Mode
en.LaunchApp=Open Console Mode

[Tasks]
Name: "oneclick"; Description: "{cm:OneClickShortcut}"; Flags: unchecked
Name: "startup"; Description: "{cm:StartWithWindows}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Tells the app it is installed: data goes to %LOCALAPPDATA%\ConsoleMode instead of next to the exe.
Source: "ConsoleMode.installed"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\Console Mode 1 Click"; Filename: "{app}\{#AppExe}"; Parameters: "--start"; \
    Comment: "Entra no modo console com 1 clique"; Tasks: oneclick

[Registry]
; Same value the app's "Iniciar com o Windows" toggle writes (StartupService).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
    ValueName: "ConsoleMode"; ValueData: """{app}\{#AppExe}"" --tray"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
; In-app updates install with /SILENT: bring the app back afterwards.
Filename: "{app}\{#AppExe}"; Flags: nowait; Check: WizardSilent

[UninstallDelete]
Type: files; Name: "{app}\ConsoleMode.installed"
; User data in %LOCALAPPDATA%\ConsoleMode (config, screen backup) is kept on purpose.
