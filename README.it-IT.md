# Console Mode

Trasforma il tuo PC Windows in una **console** con un clic: mette a fuoco la TV, spegne gli altri schermi, cambia l'audio e apre l'interfaccia dei tuoi giochi. Quando esci dal gioco, la tua scrivania torna com'era.

[English](README.md) · [Português (Brasil)](README.pt-BR.md) · [Español](README.es-ES.md) · [Deutsch](README.de-DE.md) · [Français](README.fr-FR.md) · Italiano · [Русский](README.ru-RU.md)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Versione](https://img.shields.io/github/v/release/1r0n51uv/consolemode?label=versione&color=brightgreen)
![Licenza](https://img.shields.io/github/license/1r0n51uv/consolemode)

https://github.com/user-attachments/assets/9f244c06-76e5-4ccb-ab66-e637eae900ec

## Download

Scarica l'ultima versione stabile dalla **[pagina delle release](https://github.com/1r0n51uv/consolemode/releases/latest)**:

- **`ConsoleMode-Setup-x64.exe`** (consigliato): installazione per utente, senza permessi di amministratore, si aggiorna da sola.
- **`ConsoleMode-Portable-x64.exe`**: un solo exe che salva i suoi dati accanto a sé.

Vuoi provare per primo ciò che sta per arrivare? Attiva Sistema → App → "Ricevi le versioni di prova (beta)".

Windows 10 o 11, più [Steam](https://store.steampowered.com/) (Big Picture), [Playnite](https://playnite.link/) o l'app Xbox. [RTSS](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) è facoltativo, per il limite di FPS e il contatore di FPS.

## Come funziona

1. Apri l'app e scegli lo schermo su cui giochi. Gli altri si spengono.
2. Premi **Gioca ora**, oppure usa il collegamento con un clic sul desktop. La prima volta su uno schermo nuovo, la TV chiede "Riesci a vedere questa schermata?" e, se nessuno risponde, tutto torna indietro da solo.
3. Esci da Big Picture o da Playnite e ogni cosa torna al suo posto: schermi, audio e risoluzione.

Preferisci il divano? Con l'app nell'area di notifica, tieni premuta la scorciatoia del controller e parti da lì.

Le scorciatoie del controller le scegli tu: all'inizio non ce n'è nessuna attiva, il giro della prima volta ti mostra dove sceglierle (Sistema → Controller). Uno stesso pulsante non può servire per due azioni.

| Scorciatoia del controller | Cosa fa | Suggerita |
|---|---|---|
| Tieni premuta la scorciatoia per **aprire** | Entra in modalità console dall'area di notifica | **Home** (Xbox / PS) |
| Tieni premuta la scorciatoia del **menu** | Menu sopra il gioco: finestre aperte, gestione file, volume, risoluzione, audio, FPS, HDR, uscita | **Select + Y** (Create + △) |
| Tieni premuta la scorciatoia per **tornare al PC** | Torna al PC | **Start + Select** (Options + Create) |

## Funzioni

- Nasconde gli altri schermi **scollegandoli**, con **schermate nere** o tramite **DDC/CI**, tutto con le API di Windows: non include nessuno strumento di terze parti per schermo o audio
- **Risoluzione e frequenza di aggiornamento** per schermo, **HDR**, **VRR** e **limite di FPS** (RTSS) mentre giochi
- **Contatore di FPS** sopra il gioco (RTSS) in una scheda con tre dimensioni: solo FPS, compatto (uso di CPU, GPU e RAM), dettagliato (frequenze, temperatura e memoria della GPU) o una disposizione tutta tua
- L'audio va alla TV (o a qualsiasi uscita) e poi torna com'era
- [Accende la TV e la porta sull'ingresso del PC](docs/GUIDE.md#tv-control) (Google TV / Android TV via rete) e può eseguire un comando prima di metterla in standby quando torni al PC
- Nasconde le **icone del desktop** durante la sessione, se attivi l'opzione, e ripristina la visibilità precedente quando torni al PC
- Apre **Steam Big Picture**, **Playnite a schermo intero** o la **modalità Xbox**, e manda Steam nell'area di notifica quando torni al PC
- **Menu della sessione** sopra il gioco: cambia o chiudi le finestre aperte, regola volume, risoluzione, audio, FPS e HDR, nascondi il puntatore del mouse o muovilo con il controller, ed esplora i tuoi file con [ControlFS](https://github.com/nextestudios/ControlFS) e il web con [JoyChromium](https://github.com/lippdev/joychromium)
- Un'interfaccia in stile console, pensata per la TV, che funziona anche con mouse e tastiera (schermo intero, controller Xbox e PlayStation, schede con LB/RB, uno sfondo tuo o le copertine di Steam come sfondo), con un giro guidato la prima volta che la apri
- Automazione con i collegamenti `consolemode://start` / `stop` / `menu` (Stream Deck, script) e una [API di controllo locale](docs/GUIDE.md#local-control-api)
- Inglese, portoghese, spagnolo, tedesco, francese, italiano e russo

Le novità di ogni versione sono nel [CHANGELOG.en-US.md](CHANGELOG.en-US.md) (in inglese).

## Altro

- [Guida](docs/GUIDE.md) (in inglese): modalità e ripristino, API di controllo, HDR/VRR/FPS, risoluzione dei problemi
- **Feedback:** il pulsante accanto a Impostazioni nell'app, oppure [questo modulo](https://github.com/1r0n51uv/consolemode/issues/new?template=feedback.yml). Una ⭐ aiuta altri giocatori da divano a trovare il progetto.
- Le versioni non sono ancora firmate digitalmente, quindi Windows SmartScreen o un antivirus possono segnalare il download; tutto il codice sorgente è qui.
- Vuoi aiutare con le traduzioni? Vedi [docs/TRANSLATING.md](docs/TRANSLATING.md).

[GNU Affero General Public License v3.0 only](LICENSE) · [Avvisi di terze parti](THIRD_PARTY_NOTICES.md) · [Politica di firma del codice](docs/CODE_SIGNING.md)

*Questa pagina è una traduzione del README in inglese. In caso di differenze, fa fede la versione inglese.*
