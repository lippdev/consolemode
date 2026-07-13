# Console Mode

Turn your Windows PC into a **game console** with one click: focus on your TV, hide extra monitors, switch audio, and launch your preferred fullscreen game UI.

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/badge/version-1.2-brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- Guided setup wizard (monitors → mode → audio → launch)
- Hide spare displays by **disconnect**, **black overlays**, or **DDC/CI**
- Optional **resolution & refresh rate** per monitor for console mode
- Launch **Steam Big Picture**, **Playnite fullscreen**, or **Xbox** (Win+F11)
- Optional **HDR** on the focus monitor and **VRR** (Windows setting)
- Optional global **FPS limit** via RivaTuner (RTSS)
- Audio routing, including “use output when connected” (e.g. TV HDMI)
- System tray icon to restore your desktop layout or reopen the app
- Portable executable — no installer required

## Requirements

- Windows 10 or 11
- One of the launch modes you plan to use:
  - [Steam](https://store.steampowered.com/) (Big Picture — recommended)
  - [Playnite](https://playnite.link/) (fullscreen app)
  - Xbox / Game Bar on Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — optional, only if you want the FPS limit (usually via MSI Afterburner)

## How to use

1. Download `ConsoleMode.exe` from the [latest release](https://github.com/lippdev/consolemode/releases)
2. Run it — on first launch it creates a `ConsoleMode_Data/` folder next to the executable
3. Follow the wizard and click **Start console mode**
4. When you are done, exit Big Picture / Playnite (or restore manually in Xbox mode)

> **Antivirus note:** some scanners may flag the packaged executable. The source code is available in this repository for review.

## Modes and restore

| Mode | On exit |
|------|---------|
| **Steam Big Picture** | Automatic restore (app stays in the tray) |
| **Playnite fullscreen** | Automatic restore (app stays in the tray) |
| **Xbox mode** | Manual — use *Restore now*, the tray menu, or reopen the window |

You can also restore anytime from the tray (*Restore setup* / *Show window*). With black overlays, **ESC** dismisses the curtains.

## Optional extras

### HDR

Enable HDR on the focus monitor while console mode is active. It is turned back off (or restored) when you exit.

### VRR

Console Mode can toggle the Windows VRR optimize setting. For best results, also enable VRR / G-SYNC / FreeSync in your **GPU control panel** (NVIDIA or AMD).

### FPS limit (RTSS)

Cap the global frame rate while console mode runs (helpful on a 60 Hz TV). Requires RTSS installed and running. The previous limit is restored when you exit.

## Limitations

- Multi-monitor layouts vary; on some setups restore may need a second try from the tray
- Xbox mode does not detect when fullscreen ends — restore manually
- Monitor and audio switching rely on bundled [NirSoft](https://www.nirsoft.net/) tools
- The FPS limit is global (RTSS limitation), not per display
- Packaged builds may be flagged by antivirus software

## License

Licensed under the [MIT License](LICENSE).

Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## Português (BR)

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, esconda monitores extras, ajuste o áudio e abra a interface de jogos em tela cheia que você preferir.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/badge/versão-1.2-brightgreen)
![License](https://img.shields.io/badge/licença-MIT-green)

### Funcionalidades

- Assistente guiado (monitores → modo → áudio → iniciar)
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- **Resolução e Hz** opcionais por monitor no modo console
- Abrir **Steam Big Picture**, **Playnite em tela cheia** ou **Modo Xbox** (Win+F11)
- **HDR** opcional no monitor de foco e **VRR** (ajuste do Windows)
- **Limite de FPS** opcional via RivaTuner (RTSS)
- Roteamento de áudio, inclusive “usar ao conectar” (ex.: HDMI da TV)
- Ícone na bandeja para restaurar o layout ou reabrir o app
- Executável portátil — sem instalador

### Requisitos

- Windows 10 ou 11
- Um dos modos que você for usar:
  - [Steam](https://store.steampowered.com/) (Big Picture — recomendado)
  - [Playnite](https://playnite.link/) (app em tela cheia)
  - Xbox / Game Bar no Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — opcional, só se quiser o limite de FPS (em geral via MSI Afterburner)

### Como usar

1. Baixe `ConsoleMode.exe` na [última release](https://github.com/lippdev/consolemode/releases)
2. Execute — na primeira vez cria a pasta `ConsoleMode_Data/` ao lado do executável
3. Siga o assistente e clique em **Iniciar modo console**
4. Ao terminar, saia do Big Picture / Playnite (ou restaure manualmente no Modo Xbox)

> **Antivírus:** alguns scanners podem sinalizar o executável empacotado. O código-fonte está neste repositório para auditoria.

### Modos e restauração

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Restauração automática (app na bandeja) |
| **Playnite tela cheia** | Restauração automática (app na bandeja) |
| **Modo Xbox** | Manual — *Restaurar agora*, menu da bandeja ou reabrir a janela |

Também dá para restaurar a qualquer momento pela bandeja (*Restaurar setup* / *Mostrar janela*). Com cortinas pretas, **ESC** remove o overlay.

### Extras opcionais

#### HDR

Ativa HDR no monitor de foco enquanto o modo console estiver ligado. Ao sair, o estado anterior é restaurado.

#### VRR

O Console Mode pode alterar a opção de VRR do Windows. Para melhor resultado, ligue também VRR / G-SYNC / FreeSync no **painel do driver da GPU** (NVIDIA ou AMD).

#### Limite de FPS (RTSS)

Limita a taxa de quadros global durante o modo console (útil em TV 60 Hz). Exige RTSS instalado e em execução. O limite anterior volta ao sair.

### Limitações

- Layouts multi-monitor variam; em alguns setups a restauração pode precisar de uma nova tentativa pela bandeja
- O Modo Xbox não detecta o fim do fullscreen — restaure manualmente
- Monitores e áudio dependem das ferramentas [NirSoft](https://www.nirsoft.net/) incluídas no pacote
- O limite de FPS é global (limitação do RTSS), não por tela
- Builds empacotados podem ser sinalizados por antivírus

### Licença

Licença [MIT](LICENSE).

Avisos de terceiros: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
