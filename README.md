# Console Mode

Turn your Windows PC into a **game console** with one click: hide extra monitors, focus on your TV, switch audio, and launch **Steam Big Picture** (recommended) or **Xbox Game Bar fullscreen** (Win+F11, experimental).

[English](#console-mode) · [Português (BR)](#português-br)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue)
![Status](https://img.shields.io/badge/status-alpha-orange)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)
![License](https://img.shields.io/badge/license-MIT-green)

> **Alpha software** — Console Mode is a personal side project under active development. Expect rough edges, setup-specific behavior, and breaking changes between releases. Feedback and issues are welcome.

## Features

- 4-step setup wizard (monitors → mode → audio → launch)
- Hide monitors via **disconnect**, **black overlays**, or **DDC/CI**
- Audio option to **use output when connected** (e.g. TV HDMI when it powers on)
- **Steam Big Picture (recommended)** — automatic restore when you exit (Windows window event hook)
- **Xbox mode (Alpha)** — sends Win+F11; **manual restore only** (tray or *Restore now*)
- **Optional FPS limit (RTSS)** — global frame cap via RivaTuner during console mode (anti-tearing)
- **Per-monitor resolution & refresh rate** — optional display mode per screen in step 1 (restored from backup on exit)
- Portable executable (`ConsoleMode.exe`) or PowerShell dev mode
- System tray icon to restore or reopen the app

## Requirements

- Windows 10 or 11
- [Steam](https://store.steampowered.com/) (Big Picture mode — recommended path)
- Xbox Game Bar on Windows 11 (optional, experimental; Win+F11 shortcut)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) (optional; FPS limit feature — install via MSI Afterburner)
- PowerShell 5.1+ **for development/build only** — the `.exe` does not require PowerShell

## Quick start (executable)

1. Build or download `dist/ConsoleMode.exe` (see [Build](#build))
2. Run it — on first launch it creates `ConsoleMode_Data/` next to the executable
3. Follow the wizard and click **Start console mode**
4. Prefer **Steam Big Picture** unless you specifically want to try Xbox mode

> **Antivirus note:** executables built with [PS2EXE](https://github.com/MScholtes/PS2EXE) may trigger false positives. Source code is available here for review.

## Development

```powershell
# 1. Clone the repository
git clone https://github.com/lippdev/consolemode.git
cd consolemode

# 2. Download NirSoft tools (automated)
powershell -ExecutionPolicy Bypass -File .\build\Get-NirSoftTools.ps1

# 2b. Download rtss-cli for FPS limit (optional)
powershell -ExecutionPolicy Bypass -File .\build\Get-RtssCli.ps1

# 3. Run
.\IniciarConsoleMode.bat
# or
powershell -ExecutionPolicy Bypass -File .\ConsoleMode.ps1
```

In dev mode, config and backups are stored in `ConsoleMode_Data/`.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Build-ConsoleMode.ps1
```

Output: `dist/ConsoleMode.exe` (NirSoft tools, `rtss-cli`, and icon embedded when available).

## FPS limit (RTSS)

Optional anti-tearing helper in **step 1 (Monitors)** of the wizard:

1. Install **RivaTuner Statistics Server** (ships with [MSI Afterburner](https://www.msi.com/Landing/afterburner/graphics-cards))
2. Keep RTSS running (Console Mode can try to start it)
3. Choose a global FPS cap (e.g. 60 for a 60 Hz TV)

The limit applies to **all GPU apps** while console mode is active and is **restored automatically** when you exit or click *Restore now*. This is not per-monitor (RTSS limitation) and does not replace VSync.

## Display mode (resolution & Hz)

In **step 1 (Monitors)**, each connected display has a **Resolution / Hz** dropdown:

1. Default **(Keep current)** leaves that monitor unchanged
2. Pick a mode from the list (Windows `EnumDisplaySettings` — modes your GPU/driver report)
3. On **Start console mode**, selected modes are applied before hiding other monitors
4. On exit or **Restore now**, the previous layout is restored from the MMT backup saved at start

Typical use: set your TV to **3840×2160 @ 60 Hz** while keeping a high-refresh desktop monitor unchanged until you exit.

Validate without compiling:

```powershell
.\build\Build-ConsoleMode.ps1 -TestDev
.\build\Build-ConsoleMode.ps1 -TestExe
```

## Project structure

```
consolemode/
├── ConsoleMode.ps1          # Entry point
├── IniciarConsoleMode.bat   # Dev launcher
├── assets/icon.ico          # App icon
├── lib/
│   ├── Encoding.ps1         # UTF-8
│   ├── Paths.ps1            # Portable paths (dev / exe)
│   ├── Rtss.ps1             # RTSS / FPS limit
│   ├── Engine.ps1           # Monitors, audio, restore
│   └── Gui.ps1              # WinForms wizard
└── build/
    ├── Build-ConsoleMode.ps1
    ├── Get-NirSoftTools.ps1
    └── Get-RtssCli.ps1
```

## Restoring your setup

| Mode | Restore on exit |
|------|-----------------|
| **Steam Big Picture** | Automatic (app stays in the tray) |
| **Xbox mode (Alpha)** | Manual — *Restore now*, tray menu, or reopen the window |

Other options:

- **ESC** — dismiss black overlays (overlay hide strategy)
- **Tray** — *Restore setup* or *Show window* at any time

## Known limitations (Alpha)

- Multi-monitor layouts vary widely; restore may need a retry on some setups
- Xbox mode does not detect when fullscreen closes — you must restore manually
- Monitor/audio switching relies on [NirSoft](https://www.nirsoft.net/) tools bundled at build time
- FPS limit requires user-installed RTSS; cap is global, not per display
- PS2EXE builds may be flagged by antivirus software

## License

This project is licensed under the [MIT License](LICENSE).

Third-party dependencies: see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

## Português (BR)

Transforme seu PC Windows em um **console de jogos** com um clique: esconda monitores extras, foque na TV, ajuste o áudio e abra o **Steam Big Picture** (recomendado) ou o **Modo Xbox** (Win+F11, experimental).

> **Software em Alpha** — projeto pessoal em desenvolvimento ativo. Pode haver arestas, comportamento dependente do seu setup e mudanças entre versões. Issues e feedback são bem-vindos.

### Funcionalidades

- Assistente em 4 passos (monitores → modo → áudio → iniciar)
- Esconder monitores por **desconexão**, **cortinas pretas** ou **DDC/CI**
- Áudio com opção **usar ao conectar** (HDMI da TV quando ligar)
- **Steam Big Picture (recomendado)** — restauração automática ao sair
- **Modo Xbox (Alpha)** — envia Win+F11; **restauração manual** (bandeja ou *Restaurar agora*)
- **Limite de FPS opcional (RTSS)** — cap global via RivaTuner durante o modo console (anti-tearing)
- **Resolução e Hz por monitor** — modo de exibição opcional no passo 1 (restaurado do backup ao sair)
- Executável portátil (`ConsoleMode.exe`) ou modo desenvolvimento via PowerShell
- Ícone na bandeja para restaurar ou reabrir o app

### Requisitos

- Windows 10 ou 11
- [Steam](https://store.steampowered.com/) (modo Big Picture — caminho recomendado)
- Modo Xbox no Windows 11 (opcional, experimental; atalho Win+F11)
- [RivaTuner Statistics Server](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html) (opcional; limite de FPS — via MSI Afterburner)
- PowerShell 5.1+ **apenas para desenvolvimento/build** — o `.exe` não exige PowerShell instalado

### Uso rápido (executável)

1. Gere ou baixe `dist/ConsoleMode.exe` (veja [Build](#build))
2. Execute o arquivo — na primeira vez cria `ConsoleMode_Data/` ao lado dele
3. Siga o assistente e clique em **Iniciar modo console**
4. Prefira **Steam Big Picture**, a menos que queira testar o Modo Xbox

> **Antivírus:** executáveis gerados com [PS2EXE](https://github.com/MScholtes/PS2EXE) podem gerar falso positivo. O código-fonte está aqui para auditoria.

### Desenvolvimento

```powershell
git clone https://github.com/lippdev/consolemode.git
cd consolemode
powershell -ExecutionPolicy Bypass -File .\build\Get-NirSoftTools.ps1
powershell -ExecutionPolicy Bypass -File .\build\Get-RtssCli.ps1
.\IniciarConsoleMode.bat
```

Config e backups ficam em `ConsoleMode_Data/`.

### Resolução e Hz

No **passo 1 (Monitores)**, cada tela conectada tem um menu **Resolução / Hz**:

1. **(Manter atual)** — não altera aquele monitor
2. Escolha um modo da lista (modos reportados pelo Windows/driver)
3. Ao **Iniciar modo console**, os modos selecionados são aplicados antes de esconder os outros monitores
4. Ao sair ou em **Restaurar agora**, o layout anterior volta pelo backup MMT salvo no início

Exemplo: TV em **3840×2160 @ 60 Hz** enquanto o monitor desktop continua em alta taxa até você sair.

### Restaurar o setup

| Modo | Ao sair |
|------|---------|
| **Steam Big Picture** | Automático (app na bandeja) |
| **Modo Xbox (Alpha)** | Manual — *Restaurar agora*, bandeja ou reabrir a janela |

Também: **ESC** nas cortinas pretas; menu da bandeja a qualquer momento.

### Limitações conhecidas (Alpha)

- Layouts de monitores variam; em alguns setups a restauração pode precisar de nova tentativa
- Modo Xbox não detecta o fechamento do fullscreen — restaure manualmente
- Monitores/áudio dependem das ferramentas [NirSoft](https://www.nirsoft.net/) incluídas no build
- Limite de FPS exige RTSS instalado pelo usuário; o cap é global, não por monitor
- Builds PS2EXE podem ser sinalizados por antivírus

### Licença

Projeto sob licença [MIT](LICENSE). Dependências externas: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
