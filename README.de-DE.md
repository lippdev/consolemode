# Console Mode

Mach deinen Windows-PC mit einem Klick zur **Spielkonsole**: Der Fernseher wird zum Hauptbildschirm, die anderen Bildschirme gehen aus, der Ton wechselt und deine Spieleoberfläche startet. Beende das Spiel und dein Schreibtisch-Setup kommt von selbst zurück.

[English](README.md) · [Português (Brasil)](README.pt-BR.md) · [Español](README.es-ES.md) · Deutsch · [Français](README.fr-FR.md) · [Русский](README.ru-RU.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=Version&color=brightgreen)
![License](https://img.shields.io/github/license/lippdev/consolemode)

<img src="assets/console-mode-1.6-short.gif" alt="Rundgang durch Console Mode 1.6: Ein Klick bringt das Spiel auf den Fernseher, das Sitzungsmenü über dem Spiel mit den geöffneten Fenstern, der integrierte Dateimanager ControlFS, die Konsolenoberfläche mit den Registerkarten Startseite, Sitzung und System sowie selbst gewählte Controller-Tastenkombinationen." width="800">

## Download

Hol dir die neueste stabile Version unter **[Releases](https://github.com/lippdev/consolemode/releases/latest)**:

- **`ConsoleMode-Setup-x64.exe`** (empfohlen): Installation pro Benutzer, ohne Administratorrechte, aktualisiert sich selbst.
- **`ConsoleMode-Portable-x64.exe`**: eine einzelne EXE, die ihre Daten direkt daneben speichert.
- Oder mit winget: `winget install lippdev.ConsoleMode`

Du willst Neues vor allen anderen ausprobieren? Schalte unter Einstellungen „Testversionen erhalten (Alpha und Beta)“ ein.

Windows 10 oder 11, dazu [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) oder die Xbox-App. [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) ist optional, für die FPS-Begrenzung und die FPS-Anzeige.

## So funktioniert es

1. Öffne die App und wähle den Bildschirm, auf dem du spielst. Die anderen gehen aus.
2. Drücke **Jetzt spielen** oder nutze die Desktop-Verknüpfung mit einem Klick. Beim ersten Mal auf einem neuen Bildschirm fragt der Fernseher „Kannst du diesen Bildschirm sehen?“ und setzt alles von selbst zurück, wenn niemand antwortet.
3. Beende Big Picture oder Playnite und alles kehrt zurück: Bildschirme, Ton, Auflösung.

Lieber vom Sofa aus? Wenn die App im Infobereich läuft, halte deine Controller-Tastenkombination gedrückt und es geht von dort los.

Die Controller-Tastenkombinationen wählst du selbst: Anfangs ist keine aktiv, die App fragt dich beim ersten Öffnen danach (und du kannst sie in den Einstellungen ändern). Keine Taste kann zwei Aktionen auslösen.

| Controller-Tastenkombination | Funktion | Vorschlag |
|---|---|---|
| Deine Kombination zum **Öffnen** halten | Konsolenmodus aus dem Infobereich starten | **Home** (Xbox / PS) |
| Deine Kombination für das **Menü** halten | Menü über dem Spiel: geöffnete Fenster, Dateimanager, Lautstärke, Auflösung, Audio, FPS, HDR, Beenden | **Select + Y** (Create + △) |
| Deine Kombination für **Zurück zum PC** halten | Zurück zum PC | **Start + Select** (Options + Create) |

## Funktionen

- Andere Bildschirme ausblenden durch **Trennen**, **schwarze Bildschirme** oder **DDC/CI**, alles mit den Windows-eigenen APIs: Es werden keine Bildschirm- oder Audio-Tools von Drittanbietern mitgeliefert
- **Auflösung und Bildwiederholrate** pro Bildschirm, **HDR**, **VRR** und eine **FPS-Begrenzung** (RTSS) während du spielst
- **FPS-Anzeige** über dem Spiel (RTSS) als Kachel in drei Größen: Nur FPS, Kompakt (CPU-, GPU- und RAM-Auslastung), Detailliert (Taktraten, GPU-Temperatur und Speicher) oder dein eigenes Layout
- Der Ton geht an den Fernseher (oder einen beliebigen Ausgang) und kommt danach zurück
- [Schaltet den Fernseher ein und wechselt auf den Eingang des PCs](docs/GUIDE.de-DE.md#fernsehersteuerung) (Google TV / Android TV über das Netzwerk) und kann ihn danach in den Standby versetzen
- Startet **Steam Big Picture**, **Playnite im Vollbild**, den **Xbox-Modus** oder [JoyChromium](https://github.com/lippdev/joychromium) (das Web mit dem Controller) und minimiert Steam in den Infobereich, wenn du zum PC zurückkehrst
- **Sitzungsmenü** über dem Spiel: geöffnete Fenster wechseln oder schließen, Lautstärke, Auflösung, Audio, FPS und HDR ändern, den Mauszeiger ausblenden oder mit dem Controller steuern und deine Dateien mit [ControlFS](https://github.com/nextestudios/ControlFS) durchsuchen
- Zwei Oberflächen: **Desktop** (Maus) und **Konsole** (Vollbild, Xbox- und PlayStation-Controller, Registerkarten mit LB/RB, ein eigenes Hintergrundbild oder deine Steam-Cover als Hintergrund)
- Automatisierung mit den Links `consolemode://start` / `stop` / `menu` (Stream Deck, Skripte) und einer [lokalen Steuerungs-API](docs/GUIDE.de-DE.md#lokale-steuerungs-api)
- Englisch, Portugiesisch, Spanisch, Deutsch, Französisch und Russisch

Was in jeder Version neu ist, steht im [CHANGELOG.en-US.md](CHANGELOG.en-US.md) (auf Englisch).

## Mehr

- [Anleitung](docs/GUIDE.de-DE.md): Modi und Wiederherstellung, Steuerungs-API, HDR/VRR/FPS, Fehlerbehebung
- **Feedback:** die Schaltfläche „Feedback senden“ neben den Einstellungen in der App oder [dieses Formular](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Ein ⭐ hilft anderen Sofa-Spielern, das Projekt zu finden.
- Die Releases sind noch nicht code-signiert, daher warnen Windows SmartScreen oder ein Virenscanner eventuell beim Download; der Quellcode liegt komplett hier.
- **Beim Übersetzen helfen:** siehe [docs/TRANSLATING.md](docs/TRANSLATING.md).
- Die deutsche Oberfläche wurde von [u/Skinner99](https://www.reddit.com/user/Skinner99/) auf Reddit übersetzt.

[Nur GNU Affero General Public License v3.0](LICENSE) · [Hinweise zu Drittanbietern](THIRD_PARTY_NOTICES.md) · [Richtlinie zur Codesignierung](docs/CODE_SIGNING.md)

*Diese Seite ist eine Übersetzung des englischen README. Wenn etwas abweicht, gilt die englische Version als aktuell.*
