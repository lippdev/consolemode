# Anleitung für Console Mode

Details, die nicht ins [README](../README.de-DE.md) passen.

[English](GUIDE.md) · [Português (Brasil)](GUIDE.pt-BR.md) · [Español](GUIDE.es-ES.md) · Deutsch · [Français](GUIDE.fr-FR.md) · [Русский](GUIDE.ru-RU.md)

## Modi und Wiederherstellung

| Modus | Beim Beenden |
|------|---------|
| **Steam Big Picture** | Automatische Wiederherstellung (die App bleibt im Infobereich) |
| **Playnite (Vollbild)** | Automatische Wiederherstellung (die App bleibt im Infobereich) |
| **Xbox-Modus** | Manuell: Verwende *Jetzt wiederherstellen*, das Menü im Infobereich oder öffne das Fenster erneut |
| **Eigene App** | Automatische Wiederherstellung, wenn die App (oder der gewählte überwachte Prozess) geschlossen wird |

Du kannst auch jederzeit über den Infobereich wiederherstellen (*Konfiguration wiederherstellen* / *Fenster anzeigen*). Bei schwarzen Bildschirmen blendest du die Abdeckungen mit **ESC** aus.

Das Sitzungsmenü bietet zwei Wege hinaus. **Zurück zum PC** beendet die Sitzung und setzt Bildschirme, Auflösung, HDR, Ton und die FPS-Begrenzung auf den vorherigen Stand zurück; die App bleibt im Infobereich. **Console Mode beenden** macht dasselbe und schließt danach die App, sodass die Controller-Tastenkombinationen nicht mehr reagieren, bis du sie wieder öffnest. Beim ersten Öffnen bietet das Menü eine Führung an, die jede Option erklärt. Um sie später anzusehen, drücke im Menü **Y** (△ bei PlayStation) oder **F1**, oder verwende **Einstellungen → Führung durch das Sitzungsmenü**.

## Einstellungen im Konsolenmodus

Unter **System → Controller** wählst du das Tastenlayout: **Automatisch**, **Xbox** oder **PlayStation**. Vorschau und Tastenbeschriftungen richten sich nach dieser Wahl, die beim Schließen der App gespeichert wird. Das ändert nur die angezeigten Symbole; die Eingaben des Controllers behalten ihre bisherige Belegung.

Die Registerkarte **System** bietet dieselben Einstellungen wie der Desktopmodus, darunter Controller-Tastenkombinationen, Playnite, Android TV, benutzerdefinierte FPS und den Datenordner. Beide Oberflächen teilen sich die gespeicherten Werte, die auch nach einem Neustart der App erhalten bleiben.

Öffne die Aktion, deren Tastenkombination du einrichten willst, wähle **Festlegen** oder **Ändern**, halte die gewünschte Kombination auf einem Controller gedrückt und lass die Tasten los. **Entfernen** deaktiviert diese Tastenkombination. Während der Aufnahme navigieren die Controller-Tasten nicht durch die Oberfläche; mit **Abbrechen** oder **Esc** beendest du die Aufnahme. Außerhalb der Aufnahme verlässt du den Editor mit **B**. Die Felder für Adresse und benutzerdefinierte FPS nehmen Tastatureingaben an.

Bei der Rückkehr aus Big Picture schließt **Steam beim Zurückkehren zum PC in den Infobereich minimieren** das Client-Fenster, während Steam weiterläuft. Ist die Option aus, bleibt der Desktop-Client geöffnet. Steam-Fenster werden nicht angetastet, wenn ein Spiel läuft oder sich sein Zustand nicht prüfen lässt.

Ein Wechsel des Hauptbildschirms kann Windows dazu bringen, die Desktopsymbole neu anzuordnen. **Desktopsymbole wieder an ihren Platz setzen** (standardmäßig an) speichert vor der Sitzung, wo sie liegen, und setzt sie nach der Wiederherstellung wieder dorthin. Ist auf dem Desktop **Symbole automatisch anordnen** aktiv, passiert nichts, weil Windows sie dann selbst anordnet.

**PC mit dem Controller aufwecken** erlaubt dem Xbox Wireless Adapter, Bluetooth oder einem Controller am Kabel, den PC mit der mittleren Taste des Controllers aus dem Energiesparmodus zu wecken. **Einschalten** fragt Windows nach Administratorrechten und aktiviert bei diesen Geräten *Gerät kann den Computer aus dem Ruhezustand aktivieren*. Ein ausgeschalteter PC oder einer im Ruhezustand wacht nicht auf, und manche PCs schalten Bluetooth im Energiesparmodus ab. Zusammen mit **Beim Verbinden eines Controllers starten** wacht der PC direkt im Konsolenmodus auf.

## Lokale Steuerungs-API

`consolemode://`-Links werden abgeschickt, ohne auf eine Antwort zu warten. Werkzeuge, die eine Antwort brauchen, etwa ein Fernsteuerungs-Agent, der als Windows-Dienst läuft, oder ein Stream-Deck-Plugin, das anzeigt, ob der Konsolenmodus aktiv ist, können die Named Pipe `\\.\pipe\ConsoleMode.Control` nutzen, solange die App läuft: Eine JSON-Zeile senden, eine zurückbekommen.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` und `show` tun genau das, was der passende `consolemode://`-Link tut, und antworten, sobald die App fertig ist (`ok:false` mit einem `error`, wenn der Konsolenmodus nicht gestartet ist oder die Wiederherstellung nicht abgeschlossen wurde). `status` liest nur. Verbinden können sich nur der angemeldete Benutzer und LocalSystem; nichts wird ins Netzwerk freigegeben.

## Fernsehersteuerung

Unter Einstellungen → **Fernseher** kann die App beim Start des Konsolenmodus den Fernseher einschalten und auf den HDMI-Eingang des PCs wechseln und ihn optional wieder in den Standby versetzen, nachdem der Schreibtisch wiederhergestellt ist. Ein Fernseher, der nicht antwortet, blockiert den Konsolenmodus nie: Die App protokolliert das und wartet wie gewohnt auf den Spielbildschirm.

Mit der Voreinstellung **Nicht steuern** wird nichts gesendet und Start und Wiederherstellung dauern nicht länger. Ist ein Weg ausgewählt, läuft der Fernseher-Schritt beim Start des Konsolenmodus zuerst und ist auf **30 Sekunden** begrenzt (der schlimmste Fall, wenn der Fernseher nicht antwortet und Wake-on-LAN versucht wird); der optionale Standby bei der Wiederherstellung ist auf **15 Sekunden** begrenzt. Ein Fernseher, der schon an ist, antwortet meist in ein bis zwei Sekunden.

Die meisten PC-Grafikkarten können kein HDMI-CEC senden, deshalb spricht die App stattdessen über das Netzwerk mit dem Fernseher:

### Google TV / Android TV

TCL, Sony, Hisense, Philips und andere Fernseher mit Google TV oder Android TV, über ADB (das Android-Debugging-Protokoll). Auf dem PC musst du nichts installieren.

1. Auf dem Fernseher: **Einstellungen → System → Info** (About), drücke 7-mal auf **Android TV OS-Build**, um die Entwickleroptionen freizuschalten.
2. **Einstellungen → System → Entwickleroptionen**: Schalte **USB-Debugging** ein (bei manchen Fernsehern **Netzwerk-Debugging** / **ADB über Netzwerk**).
3. Wähle in Console Mode *Google TV / Android TV (Netzwerk)*, gib die IP-Adresse des Fernsehers ein (Einstellungen → Netzwerk auf dem Fernseher; reserviere sie im Router) und den HDMI-Eingang, den der PC nutzt.
4. Drücke **Jetzt testen**. Der Fernseher fragt „USB-Debugging zulassen?“ (Allow debugging from this computer?): Setze das Häkchen bei **Von diesem Computer immer zulassen** (Always allow) und drücke **Zulassen** (Allow).

Der ADB-Schlüssel des PCs wird verschlüsselt für deinen Windows-Benutzer gespeichert (DPAPI), daher fragt der Fernseher nur einmal. Wird der Datenordner zu einem anderen Windows-Benutzer oder PC kopiert, lässt sich der Schlüssel dort nicht lesen: Die App erstellt einen neuen und der Fernseher fragt erneut „USB-Debugging zulassen?“.

Zum Aufwecken sendet die App die Android-Aufwecktaste und danach die Taste **HDMI 1-4**. Wenn dein Fernseher diese Taste ignoriert, trage unter **Eingangsbefehl** einen beliebigen Android-Shell-Befehl ein, der den Eingang des PCs öffnet. Wenn der Fernseher im Standby aus dem Netzwerk verschwindet, trage seine **MAC-Adresse** ein, damit die App zuerst Wake-on-LAN sendet (die Option „Über Netzwerk einschalten“ / „Über WLAN einschalten“ (Wake on network / Wake on Wi-Fi) des Fernsehers muss an sein).

„Kabelloses Debugging“ (Wireless debugging) mit Kopplungscode (Android-11-Smartphones und neuer) ist ein anderes, per TLS geschütztes Protokoll und wird nicht unterstützt: Verwende USB- oder Netzwerk-Debugging.

## Optionale Extras

### HDR

Aktiviert HDR auf dem Gaming-Bildschirm, solange der Konsolenmodus aktiv ist. Beim Beenden wird es wieder ausgeschaltet (oder auf den vorherigen Zustand zurückgesetzt).

### VRR

Console Mode kann die Windows-Einstellung zur VRR-Optimierung umschalten. Für beste Ergebnisse aktivierst du VRR / G-SYNC / FreeSync zusätzlich in der **Systemsteuerung deiner Grafikkarte** (NVIDIA oder AMD).

### FPS-Begrenzung (RTSS)

Begrenzt die Bildrate global, solange der Konsolenmodus läuft (praktisch bei einem 60-Hz-Fernseher). Dafür muss RTSS installiert sein und laufen. Beim Beenden wird die vorherige Begrenzung wiederhergestellt.

## Einschränkungen

- Mehrbildschirm-Anordnungen sind sehr unterschiedlich; bei manchen Setups braucht die Wiederherstellung einen zweiten Versuch über den Infobereich
- Der Xbox-Modus erkennt nicht, wann das Vollbild endet: Stelle manuell wieder her
- Die FPS-Begrenzung gilt global (Einschränkung von RTSS), nicht pro Bildschirm
- Der WinUI-3-Build lässt sich derzeit nur unter Windows kompilieren (`net8.0-windows`)

## Fehlerbehebung

### Die Desktop-Anordnung wurde nicht wiederhergestellt

Öffne das Menü im Infobereich und wähle **Konfiguration wiederherstellen**. Sieht die Anordnung immer noch falsch aus, wähle **Konfiguration wiederherstellen** noch einmal, nachdem Windows die Bildschirmänderung fertig angewendet hat. Du kannst das Fenster auch über den Infobereich wieder öffnen und manuell wiederherstellen.

### Der Ton blieb auf dem vorherigen Ausgang

Prüfe vor dem Start des Konsolenmodus, ob der Zielausgang angeschlossen und in Windows verfügbar ist. Bei HDMI-/Fernseher-Ausgängen kann es nötig sein, das Kabel neu einzustecken und den Modus erneut zu starten.

### Der Controller wird erkannt, tut aber nichts (DualSense / DualShock)

Öffne **Einstellungen → Controller testen**: Dort siehst du live, was Windows von jedem Controller empfängt. Bleibt die Anzeige leer, während du Tasten drückst, fängt höchstwahrscheinlich Steam den Controller ab (läuft Steam mit PlayStation-Unterstützung in Steam Input, macht es ihn auf dem Desktop zu Tastatur/Maus). Schließe Steam oder schalte die PlayStation-Unterstützung in Steam Input aus und teste erneut. Zeigt die Anzeige immer noch nichts, verwende **Diagnosedaten kopieren** und füge sie in das Feedbackformular ein.

### HDR oder VRR hat sich nicht geändert

Prüfe, ob der Gaming-Bildschirm die Funktion unterstützt und ob HDR in Windows aktiviert ist. Für VRR aktivierst du gegebenenfalls zusätzlich G-SYNC oder FreeSync in der Systemsteuerung der Grafikkarte.

## Aus dem Quellcode bauen (Windows)

Siehe den Abschnitt in der englischen Anleitung: [GUIDE.md#build-from-source-windows](GUIDE.md#build-from-source-windows).

*Diese Seite ist eine Übersetzung der englischen Anleitung. Wenn etwas abweicht, gilt die englische Version als aktuell.*
