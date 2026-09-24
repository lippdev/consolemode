# Console Mode

Turn your Windows PC into a **game console** with one click: focus on your TV, hide extra monitors, switch audio, and launch your preferred fullscreen game UI.

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Version](https://img.shields.io/github/v/release/lippdev/consolemode?label=version&color=brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

> [!NOTE]
> **More improvements are on the way.** I'm actively working on new features and fixes. The next ones are already in the [1.5.0 beta](https://github.com/lippdev/consolemode/releases). Your opinion shapes what comes next: use the **feedback button** in the app or [send feedback here](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). See [Feedback](#feedback).

<img src="assets/console-mode.gif" alt="Desk monitors turn off for a previously off HDMI TV; closing Big Picture restores the desk automatically." width="800">

Typical setup: two desk monitors and a distant HDMI TV that was off. Console Mode focuses the TV, turns the desk displays off, launches Steam Big Picture (or Xbox), and restores your desktop automatically when you quit Big Picture.

## Features

- **One-click home screen**: pick the screen you play on, hit **Play now**
- **Desktop shortcut** (`ConsoleMode.exe --start`) that goes straight into console mode, with the app waiting in the tray
- Hide spare displays by **disconnect**, **black overlays**, or **DDC/CI**
- Optional **resolution & refresh rate** per monitor for console mode
- Launch **Steam Big Picture**, **Playnite fullscreen**, or **Xbox** (Win+F11)
- Optional **HDR** on the focus monitor and **VRR** (Windows setting)
- Optional global **FPS limit** via RivaTuner (RTSS)
- Audio routing, including “use output when connected” (e.g. TV HDMI)
- System tray icon to restore your desktop layout or reopen the app
- **Installer** (per-user, no admin) or a **portable single-file** `.exe` — both announce new versions from GitHub Releases
- Interface in **Brazilian Portuguese**, **English** or **Spanish**, following the Windows language by default and switchable in Settings without restarting. Adding a language is one JSON file ([#14](https://github.com/lippdev/consolemode/issues/14))
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
3. From then on it is one click: **Play now**, or the **Console Mode 1 Click** desktop shortcut (Settings → Create shortcut). The first time with a new game screen, the TV asks "Can you see this screen?" and everything reverts on its own if nobody answers (mouse, keyboard or Xbox/PlayStation controller)
4. Or skip the PC altogether: with the app in the tray (turn on **Start with Windows**), **hold the Xbox button on the controller for 1 second** and console mode starts from the couch. **Hold Start + Select for 1 second** during a session to go back to the PC. Xbox (XInput) controllers only. In Settings you can switch to a **short press**: the app then turns off the controller shortcut for Game Bar in Windows (Win+G keeps working); if Steam is open, also turn off "Guide button focuses Steam" in Steam
5. Automation: `consolemode://start`, `consolemode://stop` and `consolemode://show` links work from Stream Deck, launchers, scripts or any shortcut (`stop`, like Start + Select, also closes Big Picture / Playnite before restoring the desk)
6. Two interfaces: the **Desktop** one (mouse, screen map) and the **Console** one (full screen, big cards, D-pad/stick + A/B, works with Xbox and PlayStation pads). By default the app picks Console whenever a controller is connected; change it in Settings → Interface or with the switch button on either screen
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

## Feedback

Found a bug or have an idea? Feedback is what decides the next improvements, so every report counts.

- **From the app** (1.5.0-beta.1 and later): click the feedback button, next to **Settings** on the home screen. It opens a GitHub issue that already includes your app version, whether it's installed or portable, your Windows version and the interface language. Just describe what happened or what you'd like, attach screenshots if you want, and submit.
- **Without the app:** [open the feedback form](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml) directly.

You need a free GitHub account to submit. Nothing is sent automatically: no logs, no personal data, and you see the whole text before sending it. You can write in English or Portuguese.

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

> [!NOTE]
> **Vem mais melhoria por aí.** Estou trabalhando ativamente em novos recursos e correções. As próximas já estão na [beta da 1.5.0](https://github.com/lippdev/consolemode/releases). Sua opinião define o que vem depois: use o **botão de feedback** no aplicativo ou [envie seu feedback aqui](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml). Veja [Feedback](#feedback-1).

<img src="assets/console-mode.gif" alt="Os monitores da mesa desligam para a TV no HDMI; ao sair do Big Picture, a mesa volta sozinha." width="800">

### Funcionalidades

- **Tela inicial de 1 clique**: escolha a tela onde você joga e clique em **Jogar agora**
- **Atalho na Área de Trabalho** (`ConsoleMode.exe --start`) que entra direto no modo console, com o app aguardando na bandeja
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- **Resolução e Hz** opcionais por monitor no modo console
- Abrir **Steam Big Picture**, **Playnite em tela cheia** ou **Modo Xbox** (Win+F11)
- **HDR** opcional no monitor de foco e **VRR** (ajuste do Windows)
- **Limite de FPS** opcional via RivaTuner (RTSS)
- Roteamento de áudio, inclusive “usar ao conectar” (ex.: HDMI da TV)
- Ícone na bandeja para restaurar o layout ou reabrir o app
- **Instalador** (por usuário, sem admin) ou **executável portátil em um único `.exe`** — os dois avisam das versões novas pelo GitHub
- Interface em **português do Brasil**, **inglês** ou **espanhol**, seguindo o idioma do Windows por padrão e trocável em Ajustes sem reiniciar. Adicionar um idioma é um arquivo JSON ([#14](https://github.com/lippdev/consolemode/issues/14))
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
3. Depois é 1 clique: **Jogar agora**, ou o atalho **Console Mode 1 Click** na Área de Trabalho (Ajustes → Criar atalho). Na primeira vez com uma tela de jogo nova, a TV pergunta "Está vendo esta tela?" e tudo volta sozinho se ninguém responder (mouse, teclado ou controle de Xbox/PlayStation)
4. Ou nem encoste no PC: com o app na bandeja (ligue **Iniciar com o Windows**), **segure o botão Xbox do controle por 1 segundo** e o modo console entra direto do sofá. **Segure Start + Select por 1 segundo** durante a sessão para voltar ao PC. Só controles Xbox (XInput). Em Ajustes dá para trocar por um **toque curto**: aí o app desliga o atalho do controle para a Game Bar no Windows (Win+G continua funcionando); se a Steam estiver aberta, desligue também "Botão Guide foca a Steam" na Steam
5. Automação: os links `consolemode://start`, `consolemode://stop` e `consolemode://show` funcionam em Stream Deck, launchers, scripts ou qualquer atalho (`stop`, assim como Start + Select, também fecha o Big Picture / Playnite antes de restaurar a mesa)
6. Duas interfaces: a **Desktop** (mouse, mapa de telas) e a **Console** (tela cheia, cartões grandes, D-pad/analógico + A/B, funciona com controles Xbox e PlayStation). Por padrão o app escolhe Console sempre que há um controle conectado; mude em Ajustes → Interface ou pelo botão de troca em qualquer uma das telas
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

### Feedback

Encontrou um bug ou tem uma ideia? É o feedback que define as próximas melhorias, então todo relato conta.

- **Pelo aplicativo** (1.5.0-beta.1 em diante): clique no botão de feedback, ao lado de **Ajustes** na tela inicial. Ele abre uma issue no GitHub que já traz a versão do aplicativo, se é instalado ou portátil, a versão do Windows e o idioma da interface. É só contar o que aconteceu ou o que você gostaria, anexar prints se quiser e enviar.
- **Sem o aplicativo:** [abra o formulário de feedback](https://github.com/lippdev/consolemode/issues/new?template=feedback.yml) direto.

Para enviar, é preciso uma conta gratuita no GitHub. Nada é enviado automaticamente: nem logs, nem dados pessoais, e você vê o texto inteiro antes de enviar. Pode escrever em português ou em inglês.

Se o Console Mode te poupou da dança dos monitores, uma ⭐ no repositório ajuda outras pessoas a encontrá-lo.

### Licença

Licença [MIT](LICENSE).

Avisos de terceiros: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
