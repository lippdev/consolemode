# Console Mode

Turn your Windows PC into a **game console** with one click: focus on your TV, hide extra monitors, switch audio, and launch your preferred fullscreen game UI.

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/badge/version-1.2-brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

<img src="assets/console-mode.gif" alt="Desk monitors turn off for a previously off HDMI TV; closing Big Picture restores the desk automatically." width="800">

Typical setup: two desk monitors and a distant HDMI TV that was off. Console Mode focuses the TV, turns the desk displays off, launches Steam Big Picture (or Xbox), and restores your desktop automatically when you quit Big Picture.

## Features

- **One-click home screen**: pick the screen you play on, hit **Enter console mode**
- **Desktop shortcut** (`ConsoleMode.exe --start`) that goes straight to console mode from the tray
- Hide spare displays by **disconnect**, **black overlays**, or **DDC/CI**
- Optional **resolution & refresh rate** per monitor for console mode
- Launch **Steam Big Picture**, **Playnite fullscreen**, or **Xbox** (Win+F11)
- Optional **HDR** on the focus monitor and **VRR** (Windows setting)
- Optional global **FPS limit** via RivaTuner (RTSS)
- Audio routing, including “use output when connected” (e.g. TV HDMI)
- System tray icon to restore your desktop layout or reopen the app
- Portable **single-file** executable — copy one `.exe`, no installer
- Native **C# / WinUI 3** app (Windows App SDK), unpackaged and self-contained

## Requirements

- Windows 10 or 11
- One of the launch modes you plan to use:
  - [Steam](https://store.steampowered.com/) (Big Picture — recommended)
  - [Playnite](https://playnite.link/) (fullscreen app)
  - Xbox / Game Bar on Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — optional, only if you want the FPS limit (usually via MSI Afterburner)

## How to use

1. Build `ConsoleMode.exe` on Windows (`.\build\Publish-ConsoleMode.ps1`) or grab the WinUI beta when it is published
2. Run the single `ConsoleMode.exe` — on first launch it creates `ConsoleMode_Data/` next to it (config, backups, and extracted helper tools)
3. On first launch, choose the screen you play on (the others are turned off) and click **Done**
4. From then on it is one click: **Enter console mode** on the home screen, or the **Modo Console** desktop shortcut created in Settings
5. When you are done, exit Big Picture / Playnite (or restore manually in Xbox mode)

> **Antivirus note:** some scanners may flag bundled helper tools. The source code is available in this repository for review.

## Build from source (Windows)

Requires [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows application development** workload, or the .NET 8 SDK plus the Windows App SDK.

```powershell
# Download MultiMonitorTool / SoundVolumeView / rtss-cli, then publish
.\build\Publish-ConsoleMode.ps1
```

Output: `dist\ConsoleMode.exe` (one portable file). Open `ConsoleMode.sln` to debug.

The previous PowerShell + WPF implementation is archived under `legacy/` and is not used by the WinUI app.

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

## License

Licensed under the [MIT License](LICENSE).

Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## Português (BR)

Transforme seu PC Windows em um **console de jogos** com um clique: foque na TV, esconda monitores extras, ajuste o áudio e abra a interface de jogos em tela cheia que você preferir.

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/badge/versão-1.2-brightgreen)
![License](https://img.shields.io/badge/licença-MIT-green)

<img src="assets/console-mode.gif" alt="Os monitores da mesa desligam para a TV no HDMI; ao sair do Big Picture, a mesa volta sozinha." width="800">

### Funcionalidades

- **Tela inicial de 1 clique**: escolha a tela onde você joga e clique em **Entrar no modo console**
- **Atalho na Área de Trabalho** (`ConsoleMode.exe --start`) que entra direto no modo console, pela bandeja
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- **Resolução e Hz** opcionais por monitor no modo console
- Abrir **Steam Big Picture**, **Playnite em tela cheia** ou **Modo Xbox** (Win+F11)
- **HDR** opcional no monitor de foco e **VRR** (ajuste do Windows)
- **Limite de FPS** opcional via RivaTuner (RTSS)
- Roteamento de áudio, inclusive “usar ao conectar” (ex.: HDMI da TV)
- Ícone na bandeja para restaurar o layout ou reabrir o app
- Executável portátil em **um único `.exe`** — sem instalador
- App nativo **C# / WinUI 3** (Windows App SDK), sem MSIX e self-contained

### Requisitos

- Windows 10 ou 11
- Um dos modos que você for usar:
  - [Steam](https://store.steampowered.com/) (Big Picture — recomendado)
  - [Playnite](https://playnite.link/) (app em tela cheia)
  - Xbox / Game Bar no Windows 11 (experimental; Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) — opcional, só se quiser o limite de FPS (em geral via MSI Afterburner)

### Como usar

1. Compile `ConsoleMode.exe` no Windows (`.\build\Publish-ConsoleMode.ps1`) ou use o beta WinUI quando for publicado
2. Execute o `ConsoleMode.exe` — na primeira vez cria `ConsoleMode_Data/` ao lado (config, backup e ferramentas extraídas)
3. Na primeira vez, escolha a tela onde você joga (as outras são desligadas) e clique em **Concluir**
4. Depois é 1 clique: **Entrar no modo console** na tela inicial, ou o atalho **Modo Console** criado em Ajustes
5. Ao terminar, saia do Big Picture / Playnite (ou restaure manualmente no Modo Xbox)

> **Antivírus:** alguns scanners podem sinalizar as ferramentas auxiliares. O código-fonte está neste repositório para auditoria.

### Compilar no Windows

Precisa do [Visual Studio 2022](https://visualstudio.microsoft.com/) com a workload **Desenvolvimento de aplicativos da Windows**, ou do SDK do .NET 8 + Windows App SDK.

```powershell
.\build\Publish-ConsoleMode.ps1
```

Saída: `dist\ConsoleMode.exe` (um arquivo só). Abra `ConsoleMode.sln` para depurar.

A implementação antiga em PowerShell + WPF ficou em `legacy/` e não é usada pelo app WinUI.

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

### Licença

Licença [MIT](LICENSE).

Avisos de terceiros: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
