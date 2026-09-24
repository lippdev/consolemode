# Console Mode

Turn your Windows PC into a **game console** with one click: focus on your TV, hide extra monitors, switch audio, and launch your preferred fullscreen game UI.

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=version&color=brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

<img src="assets/console-mode.gif" alt="Desk monitors turn off for a previously off HDMI TV; closing Big Picture restores the desk automatically." width="800">

Typical setup: two desk monitors and a distant HDMI TV that was off. Console Mode focuses the TV, turns the desk displays off, launches Steam Big Picture (or Xbox), and restores your desktop automatically when you quit Big Picture.

## Features

- **One-click home screen**: pick the screen you play on, hit **Play now**
- **Desktop shortcut** (`ConsoleMode.exe --start`) that goes straight into console mode, with the app waiting in the tray; `ConsoleMode.exe --stop` restores the desktop (handy for remote tools and scripts)
- Hide spare displays by **disconnect**, **black overlays**, or **DDC/CI**
- Optional **resolution & refresh rate** per monitor for console mode
- Launch **Steam Big Picture**, **Playnite fullscreen**, or **Xbox** (Win+F11)
- Optional **HDR** on the focus monitor and **VRR** (Windows setting)
- Optional global **FPS limit** via RivaTuner (RTSS)
- Audio routing, including “use output when connected” (e.g. TV HDMI)
- System tray icon to restore your desktop layout or reopen the app
- **Installer** (per-user, no admin) or a **portable single-file** `.exe` — both announce new versions from GitHub Releases
- Interface in **Brazilian Portuguese** or **English**, switchable in Settings without restarting
- Native **C# / WinUI 3** app (Windows App SDK), unpackaged and self-contained

## Why not just…?

- **Pick a monitor in Playnite / Big Picture settings?** That moves the game UI to the TV, but your desk monitors stay on, audio stays on the desk speakers, and you undo it by hand afterwards.
- **Use a display-switching add-on?** Those change the primary display. Console Mode also turns off or covers the other screens, routes audio to the TV, optionally sets resolution, HDR, VRR and an FPS cap, and puts everything back when you quit.
- **Win+P "Second screen only"?** Works for one fixed setup. Console Mode remembers which screen is the TV, launches Steam, Playnite or Xbox, and on a new screen asks "Can you see this screen?" and reverts on its own if nobody answers.

## Requirements

- Windows 10 or 11
- One of the launch modes you plan to use:
  - [Steam](https://store.steampowered.com/) (Big Picture — recommended)
  - [Playnite](https://playnite.link/) (fullscreen app)
  - Xbox / Game Bar on Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — optional, only if you want the FPS limit (usually via MSI Afterburner)

## How to use

1. Download from [Releases](https://github.com/lippdev/consolemode/releases):
   - **`ConsoleMode-Setup-x64.exe`** (recommended) — per-user install, no admin, Start menu entry, optional one-click desktop shortcut and start with Windows; data in `%LOCALAPPDATA%\ConsoleMode`
   - **`ConsoleMode-Portable-x64.exe`** — a single exe that keeps its data in `ConsoleMode_Data\` next to it
2. On first launch a short tour shows the screen map: pick the screen you play on (the others turn off)
3. From then on it is one click: **Play now**, or the **Console Mode** desktop shortcut (Settings → Create shortcut). The first time with a new game screen, the TV asks "Can you see this screen?" and everything reverts on its own if nobody answers (mouse, keyboard or Xbox/PlayStation controller)
4. When you are done, exit Big Picture / Playnite (or restore manually in Xbox mode)
5. New versions are announced in the app from GitHub Releases (installed: updates silently; portable: swaps the exe)

> **Antivirus note:** some scanners may flag bundled helper tools. The source code is available in this repository for review.

## Build from source (Windows)

Requires [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows application development** workload, or the .NET 8 SDK plus the Windows App SDK.

```powershell
# Downloads MultiMonitorTool / SoundVolumeView / rtss-cli, then builds both packages
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Output: `dist\ConsoleMode-Portable-x64.exe` and `dist\ConsoleMode-Setup-x64.exe` (the installer needs [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Open `ConsoleMode.sln` to debug.

To release, push a tag like `v1.4.0` (or `v1.4.0-beta.2` for a pre-release): the `Release` workflow builds both files and publishes them, and the app picks them up as an update.

The previous PowerShell + WPF implementation (1.2 and earlier) lives on the [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) branch and is not used by the WinUI app.

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
- The WinUI 3 build currently requires Windows to compile (`net8.0-windows`)

## Troubleshooting

### Desktop layout did not restore

Open the tray menu and choose **Restore setup**. If the layout still looks wrong, choose **Restore setup** again after Windows finishes applying the monitor change. You can also reopen the window from the tray and restore manually.

### Audio stayed on the previous output

Check that the target output is connected and available in Windows before starting console mode. For HDMI/TV outputs, reconnecting the cable and starting the mode again may be necessary.

### HDR or VRR did not change

Confirm that the focus monitor supports the feature and that HDR is enabled in Windows. For VRR, also enable G-SYNC or FreeSync in the GPU control panel when applicable.

If Console Mode saved you some monitor juggling, a ⭐ on the repo helps other couch gamers find it.

## License

Licensed under the [MIT License](LICENSE).

Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## Português (BR)

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, esconda monitores extras, ajuste o áudio e abra a interface de jogos em tela cheia que você preferir.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=vers%C3%A3o&color=brightgreen)
![License](https://img.shields.io/badge/licença-MIT-green)

<img src="assets/console-mode.gif" alt="Os monitores da mesa desligam para a TV no HDMI; ao sair do Big Picture, a mesa volta sozinha." width="800">

### Funcionalidades

- **Tela inicial de 1 clique**: escolha a tela onde você joga e clique em **Jogar agora**
- **Atalho na Área de Trabalho** (`ConsoleMode.exe --start`) que entra direto no modo console, com o app aguardando na bandeja; `ConsoleMode.exe --stop` restaura a área de trabalho (útil para ferramentas remotas e scripts)
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- **Resolução e Hz** opcionais por monitor no modo console
- Abrir **Steam Big Picture**, **Playnite em tela cheia** ou **Modo Xbox** (Win+F11)
- **HDR** opcional no monitor de foco e **VRR** (ajuste do Windows)
- **Limite de FPS** opcional via RivaTuner (RTSS)
- Roteamento de áudio, inclusive “usar ao conectar” (ex.: HDMI da TV)
- Ícone na bandeja para restaurar o layout ou reabrir o app
- **Instalador** (por usuário, sem admin) ou **executável portátil em um único `.exe`** — os dois avisam das versões novas pelo GitHub
- Interface em **português do Brasil** ou **inglês**, trocável em Ajustes sem reiniciar
- App nativo **C# / WinUI 3** (Windows App SDK), sem MSIX e self-contained

### Por que não só…?

- **Escolher o monitor nos ajustes do Playnite / Big Picture?** Isso leva a interface para a TV, mas os monitores da mesa continuam ligados, o áudio fica nas caixas da mesa e depois você desfaz tudo na mão.
- **Usar um add-on de trocar tela?** Eles mudam o monitor principal. O Console Mode também desliga ou cobre as outras telas, manda o áudio para a TV, ajusta resolução, HDR, VRR e limite de FPS se você quiser, e devolve tudo ao sair.
- **Win+P "Somente segunda tela"?** Funciona para um setup fixo. O Console Mode lembra qual tela é a TV, abre Steam, Playnite ou Xbox e, numa tela nova, pergunta "Está vendo esta tela?" e desfaz tudo sozinho se ninguém responder.

### Requisitos

- Windows 10 ou 11
- Um dos modos que você for usar:
  - [Steam](https://store.steampowered.com/) (Big Picture — recomendado)
  - [Playnite](https://playnite.link/) (app em tela cheia)
  - Xbox / Game Bar no Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — opcional, só se quiser o limite de FPS (em geral via MSI Afterburner)

### Como usar

1. Baixe em [Releases](https://github.com/lippdev/consolemode/releases):
   - **`ConsoleMode-Setup-x64.exe`** (recomendado) — instala por usuário, sem admin, com menu Iniciar e, se quiser, atalho de 1 clique e iniciar com o Windows; dados em `%LOCALAPPDATA%\ConsoleMode`
   - **`ConsoleMode-Portable-x64.exe`** — um único exe que guarda os dados em `ConsoleMode_Data\` ao lado dele
2. Na primeira vez, um tour curto mostra o mapa das telas: escolha a tela onde você joga (as outras desligam)
3. Depois é 1 clique: **Jogar agora**, ou o atalho **Modo Console** na Área de Trabalho (Ajustes → Criar atalho). Na primeira vez com uma tela de jogo nova, a TV pergunta "Está vendo esta tela?" e tudo volta sozinho se ninguém responder (mouse, teclado ou controle de Xbox/PlayStation)
4. Ao terminar, saia do Big Picture / Playnite (ou restaure manualmente no Modo Xbox)
5. O app avisa das versões novas pelas releases do GitHub (instalado: atualiza sozinho; portátil: troca o exe)

> **Antivírus:** alguns scanners podem sinalizar as ferramentas auxiliares. O código-fonte está neste repositório para auditoria.

### Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
.\build\Publish-ConsoleMode.ps1 -Version 1.4.0
```

Saída: `dist\ConsoleMode-Portable-x64.exe` e `dist\ConsoleMode-Setup-x64.exe` (o instalador precisa do [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`). Abra `ConsoleMode.sln` para depurar.

Para lançar uma versão, faça push de uma tag como `v1.4.0` (ou `v1.4.0-beta.2` para pré-release): o workflow `Release` gera e publica os dois arquivos, e o app oferece a atualização.

A implementação antiga em PowerShell + WPF (1.2 e anteriores) fica na branch [`legacy`](https://github.com/lippdev/consolemode/tree/legacy) e não é usada pelo app WinUI.

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
- O build WinUI 3 precisa ser compilado no Windows (`net8.0-windows`)

Se o Console Mode te poupou da dança dos monitores, uma ⭐ no repositório ajuda outras pessoas a encontrá-lo.

### Licença

Licença [MIT](LICENSE).

Avisos de terceiros: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
