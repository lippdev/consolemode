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

Wird die App mitten in einer Sitzung geschlossen oder geht der PC aus, stellt Console Mode beim nächsten Öffnen (auch bei der Windows-Anmeldung, wenn es mit Windows startet) alles wieder her, was die Sitzung geändert hat: Bildschirme, Ton, RTSS, Desktopsymbole und Zeiger, und sagt Bescheid.

Du kannst auch jederzeit über den Infobereich wiederherstellen (*Konfiguration wiederherstellen* / *Fenster anzeigen*). Bei schwarzen Bildschirmen blendest du die Abdeckungen mit **ESC** aus.

Das Sitzungsmenü bietet zwei Wege hinaus. **Zurück zum PC** beendet die Sitzung und setzt Bildschirme, Auflösung, HDR, Ton und die FPS-Begrenzung auf den vorherigen Stand zurück; die App bleibt im Infobereich. **Console Mode beenden** macht dasselbe und schließt danach die App, sodass die Controller-Tastenkombinationen nicht mehr reagieren, bis du sie wieder öffnest. Beim ersten Öffnen bietet das Menü eine Führung an, die jede Option erklärt. Um sie später anzusehen, drücke im Menü **Y** (△ bei PlayStation) oder **F1**, oder verwende **System → App → Führung durch das Sitzungsmenü**.

Das Menü funktioniert auch **außerhalb des Konsolenmodus**, um den PC vom Sofa aus zu bedienen, ohne etwas zu starten: Solange Console Mode geöffnet ist (auch nur im Infobereich), öffnen es die Menü-Tastenkombination, das Symbol im Infobereich oder `consolemode://menu` auf dem Spielbildschirm, wenn er eingeschaltet ist, sonst auf dem Hauptbildschirm. Dort kannst du Fenster wechseln, Lautstärke, Auflösung, Audioausgabe und HDR ändern, ControlFS und JoyChromium öffnen, den Mauszeiger ausblenden und die Maus mit dem Controller steuern. Was du änderst, bleibt so, bis du es wieder änderst, denn es gibt keine Sitzung zum Rückgängigmachen; FPS-Begrenzung und FPS-Anzeige erscheinen nur in einer Sitzung. Stört die Tastenkombination in einem Spiel, schalte **System → Controller → Sitzungsmenü außerhalb des Konsolenmodus** aus (über den Infobereich öffnet sich das Menü weiterhin).

Die **Tastenkombination für „Aktive Anwendungen“** macht mit dem Controller, was Win + Tab macht: Sie öffnet die Windows-Ansicht mit allen offenen Fenstern, in einer Sitzung oder außerhalb. Solange sie offen ist, wählen Steuerkreuz oder linker Stick ein Fenster (Halten wiederholt), **A** öffnet es und **B** schließt die Ansicht; die Tastenkombination schließt sie ebenfalls. Sie ist von Anfang an auf **Select + X** gesetzt; ändere oder deaktiviere sie unter **System → Controller → Controller-Tastenkombination für „Aktive Anwendungen“** (auch die Xbox-Taste allein geht, gehalten, wenn sie nicht die Kombination zum Öffnen von Console Mode ist). Die ersten drei Male zeigt ein Hinweis die Tasten, bevor die Ansicht aufgeht, denn über ihr erscheint nichts. Mit einem Xbox-Controller bewegt Windows die Auswahl selbst; mit einem PlayStation-Controller, den Windows dort nicht versteht, übernimmt das Console Mode (schalte **„Aktive Anwendungen“ mit einem PlayStation-Controller bedienen** aus, wenn du DS4Windows oder Steam Input nutzt und sich Eingaben wiederholen).

## Die Oberfläche

Console Mode hat eine einzige Oberfläche, gestaltet wie die einer Konsole: Sie füllt den Bildschirm und funktioniert mit einem Xbox- oder PlayStation-Controller, der Maus oder der Tastatur (klicke auf eine Registerkarte oder nutze Bild auf / Bild ab; Eingabe und Esc wirken wie A und B). Beim ersten Öffnen führt dich eine Tour durch die Registerkarten, die Launcher, die Spielen-Schaltfläche, die Bildschirme, die Schnelleinstellungen und die Controller-Tastenkombinationen; jeder Schritt geht von selbst weiter, sobald Zeit zum Lesen war (der Balken unter der Karte zeigt, wie lange noch), **A** geht sofort weiter, **B** überspringt sie. Um sie erneut zu sehen: **System → App → Tutorial**. Während die App lädt, zeigt sie ein Intro mit dem Logo und einem Startsound; jede Taste überspringt es, ab und zu kommt als Überraschung eines im Stil der Xbox 360 oder der PS4 mit dem Sound der Konsole; unter **System → App** wählst du einen Stil (damit enden die Überraschungen), siehst es sofort an und schaltest das Intro oder nur seinen Sound aus.

Unter **System → Controller** wählst du das Tastenlayout: **Automatisch**, **Xbox** oder **PlayStation**. Vorschau und Tastenbeschriftungen richten sich nach dieser Wahl, die beim Schließen der App gespeichert wird. Das ändert nur die angezeigten Symbole; die Eingaben des Controllers behalten ihre bisherige Belegung.

Die Registerkarte **System** enthält alle Einstellungen, darunter Controller-Tastenkombinationen, Playnite, Android TV, benutzerdefinierte FPS, das Layout des FPS-Zählers, die Argumente der eigenen App, den Diagnosebericht und den Datenordner. Die Werte bleiben auch nach einem Neustart der App erhalten.

Öffne die Aktion, deren Tastenkombination du einrichten willst, wähle **Festlegen** oder **Ändern**, halte die gewünschte Kombination auf einem Controller gedrückt und lass die Tasten los. **Entfernen** deaktiviert diese Tastenkombination. Während der Aufnahme navigieren die Controller-Tasten nicht durch die Oberfläche; mit **Abbrechen** oder **Esc** beendest du die Aufnahme. Außerhalb der Aufnahme verlässt du den Editor mit **B**. Die Felder für Adresse und benutzerdefinierte FPS nehmen Tastatureingaben an.

Bei der Rückkehr aus Big Picture schließt **Steam beim Zurückkehren zum PC in den Infobereich minimieren** das Client-Fenster, während Steam weiterläuft. Ist die Option aus, bleibt der Desktop-Client geöffnet. Steam-Fenster werden nicht angetastet, wenn ein Spiel läuft oder sich sein Zustand nicht prüfen lässt.

Ein Wechsel des Hauptbildschirms kann Windows dazu bringen, die Desktopsymbole neu anzuordnen. **Desktopsymbole wieder an ihren Platz setzen** (standardmäßig an) speichert vor der Sitzung, wo sie liegen, und setzt sie nach der Wiederherstellung wieder dorthin. Ist auf dem Desktop **Symbole automatisch anordnen** aktiv, passiert nichts, weil Windows sie dann selbst anordnet.

**Mauszeiger aus dem Weg räumen** (standardmäßig an): Sobald der Launcher auf dem Fernseher erscheint, wandert der Zeiger in die untere rechte Ecke des Spielbildschirms, und einige Sekunden später noch einmal, weil Steam ihn beim Öffnen von Big Picture wieder zentriert. Beim Zurückkehren zum PC kommt er an seine alte Stelle zurück. Den Zeiger ganz auszublenden bleibt eine Option im Sitzungsmenü.

**PC mit dem Controller aufwecken** erlaubt dem Xbox Wireless Adapter, Bluetooth oder einem Controller am Kabel, den PC mit der mittleren Taste des Controllers aus dem Energiesparmodus zu wecken. **Einschalten** fragt Windows nach Administratorrechten und aktiviert bei diesen Geräten *Gerät kann den Computer aus dem Ruhezustand aktivieren*. Ein ausgeschalteter PC oder einer im Ruhezustand wacht nicht auf, und manche PCs schalten Bluetooth im Energiesparmodus ab. Zusammen mit **Beim Verbinden eines Controllers starten** wacht der PC direkt im Konsolenmodus auf.

**Profile pro Spiel** geben jedem Steam-Spiel eine eigene Auflösung, HDR, ein FPS-Limit und eine Audioausgabe. Öffne bei laufendem Spiel das Sitzungsmenü und wähle **Spielprofil → Aktuelle Einstellungen für … speichern**: Gespeichert wird, was der Spielbildschirm in diesem Moment nutzt. Wenn das Spiel das nächste Mal während einer Sitzung startet, wendet Console Mode das Profil an, und beim Schließen kehrt der Bildschirm in den vorherigen Zustand zurück. Das Spiel wird über Steam erkannt, egal ob es aus Big Picture, Playnite oder der Xbox-App gestartet wurde. Die gespeicherten Profile stehen unter System → Beim Starten des Konsolenmodus → **Profile pro Spiel** und lassen sich dort löschen.

## Lokale Steuerungs-API

`consolemode://`-Links werden abgeschickt, ohne auf eine Antwort zu warten. Werkzeuge, die eine Antwort brauchen, etwa ein Fernsteuerungs-Agent, der als Windows-Dienst läuft, oder ein Stream-Deck-Plugin, das anzeigt, ob der Konsolenmodus aktiv ist, können die Named Pipe `\\.\pipe\ConsoleMode.Control` nutzen, solange die App läuft: Eine JSON-Zeile senden, eine zurückbekommen.

```
→ {"cmd":"status"}          // or "start", "stop", "show"
← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.5.0"}
```

`start`, `stop` und `show` tun genau das, was der passende `consolemode://`-Link tut, und antworten, sobald die App fertig ist (`ok:false` mit einem `error`, wenn der Konsolenmodus nicht gestartet ist oder die Wiederherstellung nicht abgeschlossen wurde). `status` liest nur. Verbinden können sich nur der angemeldete Benutzer und LocalSystem; nichts wird ins Netzwerk freigegeben.

## Fernsehersteuerung

Unter System → **Fernseher** kann die App beim Start des Konsolenmodus den Fernseher einschalten und auf den HDMI-Eingang des PCs wechseln und ihn optional wieder in den Standby versetzen, nachdem der Schreibtisch wiederhergestellt ist. Ein Fernseher, der nicht antwortet, blockiert den Konsolenmodus nie: Die App protokolliert das und wartet wie gewohnt auf den Spielbildschirm.

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

Öffne **System → Tastenkombination, Hilfe und Daten → Controller testen**: Dort siehst du live, was Windows von jedem Controller empfängt. Bleibt die Anzeige leer, während du Tasten drückst, fängt höchstwahrscheinlich Steam den Controller ab (läuft Steam mit PlayStation-Unterstützung in Steam Input, macht es ihn auf dem Desktop zu Tastatur/Maus). Schließe Steam oder schalte die PlayStation-Unterstützung in Steam Input aus und teste erneut. Zeigt die Anzeige immer noch nichts, verwende **Diagnosedaten kopieren** und füge sie in das Feedbackformular ein.

### HDR oder VRR hat sich nicht geändert

Prüfe, ob der Gaming-Bildschirm die Funktion unterstützt und ob HDR in Windows aktiviert ist. Für VRR aktivierst du gegebenenfalls zusätzlich G-SYNC oder FreeSync in der Systemsteuerung der Grafikkarte.

## Aus dem Quellcode bauen (Windows)

Siehe den Abschnitt in der englischen Anleitung: [GUIDE.md#build-from-source-windows](GUIDE.md#build-from-source-windows).

*Diese Seite ist eine Übersetzung der englischen Anleitung. Wenn etwas abweicht, gilt die englische Version als aktuell.*
