; Console Mode 2 Alpha: per-user installer (no admin). A separate product from Console Mode 1.x:
; its own AppId, folder, Start menu entry and uninstall entry. It never touches 1.x's install, its
; Run value, the consolemode:// link or %LOCALAPPDATA%\ConsoleMode. Built by build\Publish-ConsoleMode2.ps1:
;   ISCC /DAppVersion=2.0.0-alpha.1 /DNumericVersion=2.0.0.0 /DSourceDir=<dir with ConsoleMode2.exe> /DOutputDir=<dist\v2> /DArch=x64 ConsoleMode2.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef NumericVersion
  #define NumericVersion "0.0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir (the folder staged by Publish-ConsoleMode2.ps1) is required
#endif
#ifndef OutputDir
  #define OutputDir "..\dist\v2"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

#define AppName "Console Mode 2 Alpha"
#define AppExe "ConsoleMode2.exe"

[Setup]
; Keep this AppId forever: it is how updates find the existing 2.0 install. It is NOT the 1.x one.
AppId={{388223BE-BE29-42B4-AC17-4394D1A5F7FF}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=lippdev
AppPublisherURL=https://github.com/lippdev/consolemode
AppSupportURL=https://github.com/lippdev/consolemode/issues
AppUpdatesURL=https://github.com/lippdev/consolemode/releases
VersionInfoVersion={#NumericVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription={#AppName} Setup
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=ConsoleMode2Alpha-Setup-{#Arch}
SetupIconFile=..\crates\cm-app\assets\ConsoleMode2.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
LicenseFile=..\LICENSE
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
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
ptbr.LaunchApp=Abrir o Console Mode 2 Alpha
en.LaunchApp=Open Console Mode 2 Alpha
es.LaunchApp=Abrir Console Mode 2 Alpha
de.LaunchApp=Console Mode 2 Alpha öffnen
fr.LaunchApp=Ouvrir Console Mode 2 Alpha
ru.LaunchApp=Открыть Console Mode 2 Alpha
ptbr.RemoveHomeFirst=Remova primeiro o registro da Home do Modo Xbox em Ajustes do Console Mode 2 Alpha.
en.RemoveHomeFirst=Remove the Xbox Mode Home registration in Console Mode 2 Alpha Settings before uninstalling.
es.RemoveHomeFirst=Elimina el registro de la pantalla de inicio del modo Xbox en los ajustes de Console Mode 2 Alpha antes de desinstalar.
de.RemoveHomeFirst=Entferne vor der Deinstallation die Registrierung als Xbox-Modus-Start-App in den Einstellungen von Console Mode 2 Alpha.
fr.RemoveHomeFirst=Supprimez l’inscription de l’application d’accueil du mode Xbox dans les paramètres de Console Mode 2 Alpha avant de désinstaller.
ru.RemoveHomeFirst=Перед удалением отключите регистрацию домашнего приложения режима Xbox в настройках Console Mode 2 Alpha.

[Files]
Source: "{#SourceDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ConsoleMode2Home.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\AppxManifest.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ConsoleMode2Home.Dev.sccd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\FseAssets\*.png"; DestDir: "{app}\FseAssets"; Flags: ignoreversion
Source: "..\docs\v2\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; DestName: "THIRD_PARTY_NOTICES.md"; Flags: ignoreversion
Source: "..\crates\cm-ui\assets\fonts\SourceSans3-OFL.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
; Its own Start menu entry; 1.x keeps "Console Mode".
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"

; No [Registry] on purpose: no consolemode:// link, no Run entry, nothing under Software\ConsoleMode.

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
; In-app updates install with /SILENT: bring the app back afterwards.
Filename: "{app}\{#AppExe}"; Flags: nowait; Check: WizardSilent

; User data in %LOCALAPPDATA%\ConsoleMode2 (config, screen backup) is kept on uninstall on purpose.

[Code]
function InitializeUninstall(): Boolean;
var
  Exe: String;
  ExitCode: Integer;
begin
  Result := True;
  Exe := ExpandConstant('{app}\{#AppExe}');
  if FileExists(Exe) and Exec(Exe, '--fse-uninstall-check', ExpandConstant('{app}'),
    SW_HIDE, ewWaitUntilTerminated, ExitCode) and (ExitCode = 2) then
  begin
    SuppressibleMsgBox(CustomMessage('RemoveHomeFirst'), mbError, MB_OK, IDOK);
    Result := False;
  end;
end;
